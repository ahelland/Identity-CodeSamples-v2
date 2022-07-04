using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        ConfigurationBinder.Bind(builder.Configuration.GetSection("AzureAd"), options);                
        options.Events ??= new OpenIdConnectEvents();
        options.Events.OnRedirectToIdentityProvider += OnRedirectToIdentityProviderFunc;
    });

async Task OnRedirectToIdentityProviderFunc(RedirectContext arg)
{
    //https://docs.microsoft.com/en-us/azure/active-directory-b2c/enable-authentication-web-application-options

    //Prepopulate the sign-in name
    string loginHint = arg.HttpContext.Request.Query["loginHint"];
    if (loginHint != null)
    { 
        arg.ProtocolMessage.LoginHint = loginHint; 
    }

    //Preselect an identity provider
    string domainHint = arg.HttpContext.Request.Query["domainHint"];
    if(domainHint != null)
    {
        arg.ProtocolMessage.DomainHint = domainHint;
    }

    //Specify the UI language
    string locale = arg.HttpContext.Request.Query["locale"];
    if (!string.IsNullOrEmpty(locale))
    {
        arg.ProtocolMessage.UiLocales = locale;
    }

    //Pass a custom query string parameter
    string customParamKey = arg.HttpContext.Request.Query["customParamKey"];
    string customParamValue = arg.HttpContext.Request.Query["customParamValue"];
    if (!string.IsNullOrEmpty(customParamKey))
    {
        arg.ProtocolMessage.SetParameter(customParamKey, customParamValue);
    }

    //Use a different policy
    string policy = arg.HttpContext.Request.Query["policy"];
    if(!string.IsNullOrEmpty(policy))
    {
        arg.ProtocolMessage.Parameters["p"] = policy;
    }

    //Pass an ID token hint
    string idTokenHint = arg.HttpContext.Request.Query["tokenHint"];
    if(!string.IsNullOrEmpty(idTokenHint))
    {
        arg.ProtocolMessage.IdTokenHint = idTokenHint;
    }    

    await Task.CompletedTask.ConfigureAwait(false);
}

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
