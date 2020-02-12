using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace aad_b2c_custom_policies_dotnet_core
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //"Regular" AAD B2C policies
            services.AddAuthentication(AzureADB2CDefaults.AuthenticationScheme)
                .AddAzureADB2C(options => Configuration.Bind("AzureADB2C", options));

            //Magic link auth
            string magic_link_policy = Configuration.GetSection("AzureAdB2C")["MagicLinkPolicyId"];
            services.AddAuthentication(options => options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme).AddOpenIdConnect(magic_link_policy, GetOpenIdSignUpOptions(magic_link_policy)).AddCookie();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Partner", policy => policy.RequireClaim("Role", "Partner"));
                options.AddPolicy("Customer", policy => policy.RequireClaim("Role", "Customer"));
                options.AddPolicy("Employee", policy => policy.RequireClaim("Role", "Employee"));                
            });

            services.AddRazorPages();          
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //Make sure scheme is https
            app.Use((context, next) =>
            {
                context.Request.Scheme = "https";
                return next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private Action<OpenIdConnectOptions> GetOpenIdSignUpOptions(string policy)
            => options =>
            {
                string clientId = Configuration.GetSection("AzureAdB2C")["ClientId"];
                string B2CDomain = Configuration.GetSection("AzureAdB2C")["B2CDomain"];
                string Domain = Configuration.GetSection("AzureAdB2C")["Domain"];


                options.MetadataAddress = $"https://{B2CDomain}/{Domain}/{policy}/v2.0/.well-known/openid-configuration";
                options.ClientId = clientId;
                options.ResponseType = OpenIdConnectResponseType.IdToken;
                options.SignedOutCallbackPath = "/signout/" + policy;
                options.CallbackPath = "/signin-oidc-link";
                options.SignedOutRedirectUri = "/";
                options.SignInScheme = "AzureADB2C";
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        if (context.Properties.Items.ContainsKey("id_token_hint"))
                            context.ProtocolMessage.SetParameter("id_token_hint", context.Properties.Items["id_token_hint"]);                        

                        return Task.FromResult(0);
                    }
                };
            };
    }
}
