using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using blazor_jwt_generator_dotnet10.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace blazor_jwt_generator_dotnet10.Pages
{
    public class IndexBase : ComponentBase
    {
        [Inject]
        protected IConfiguration configuration { get; set; } = default!;

        [Inject]
        protected IJSRuntime JS { get; set; } = default!;

        private static Lazy<X509SigningCredentials> SigningCredentials = default!;
        protected string SigningCertThumbprint = string.Empty;

        public static GenericToken jwt { get; set; } = default!;
        public string output = "";
        public bool useLocalTime { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            string iss = configuration.GetSection("JWTSettings")["Issuer"] ?? string.Empty;
            string aud = configuration.GetSection("JWTSettings")["Audience"] ?? string.Empty;
            string sub = configuration.GetSection("JWTSettings")["DefaultSubject"] ?? string.Empty;

            SigningCertThumbprint = configuration.GetSection("JWTSettings")["SigningCertThumbprint"] ?? string.Empty;

            jwt = new GenericToken
            {
                Audience = aud,
                Issuer = iss,
                Subject = sub,
            };

            await ApplyTimeSettings();

            //If we run on Windows the certificate needs to be in cert store
            if (OperatingSystem.IsWindows())
            {
                SigningCredentials = new Lazy<X509SigningCredentials>(() =>
                {
                    X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                    certStore.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection certCollection = certStore.Certificates.Find(
                                                X509FindType.FindByThumbprint,
                                                SigningCertThumbprint,
                                                false);
                    // Get the first cert with the thumbprint
                    if (certCollection.Count > 0)
                    {
                        return new X509SigningCredentials(certCollection[0]);
                    }

                    return null!;
                });
            }

            //If Linux
            if (OperatingSystem.IsLinux())
            {
                {
                    X509Certificate2? cert = null;
                    try
                    {
                        byte[]? bytes = null;

                        //Check if we're running in Azure Container Apps and if so enable Key Vault integration
                        if (configuration.GetSection("JWTSettings")["HostEnvironment"] == "ACA")
                        {
                            var vaultName = configuration.GetSection("AzureSettings")["KeyVaultName"];
                            var certificateName = configuration.GetSection("AzureSettings")["CertificateName"];

                            var client = new SecretClient(new Uri($"https://{vaultName}.vault.azure.net/"), new ManagedIdentityCredential());
                            var certy = client.GetSecret(certificateName);
                            var rawCert = certy.Value;
                            bytes = Convert.FromBase64String(rawCert.Value);
                        }
                        //Fallback to file system if local Linux
                        else
                        {
                            bytes = System.IO.File.ReadAllBytes($"/var/ssl/private/{SigningCertThumbprint}.p12");
                        }

                        if (bytes != null) cert = X509CertificateLoader.LoadPkcs12(bytes, null, X509KeyStorageFlags.PersistKeySet, null);
                    }
                    catch
                    {
                        cert = null;
                    }

                    SigningCredentials = new Lazy<X509SigningCredentials>(() =>
                    {
                        if (cert != null)
                        {
                            return new X509SigningCredentials(cert);
                        }

                        return new X509SigningCredentials(null);
                    });
                }
            }
        }

        protected void HandleValidSubmit()
        {

        }

        protected async Task ApplyTimeSettings()
        {
            DateTimeOffset now = useLocalTime ? await GetLocalTimeAsync() : DateTimeOffset.UtcNow;
            DateTimeOffset expiration = now.AddMinutes(60);
            string format = useLocalTime ? "yyyy-MM-dd HH:mm:sszzz" : "yyyy-MM-dd HH:mm:ssZ";

            jwt.IssuedAt = now.ToString(format);
            jwt.iat = now.ToUnixTimeSeconds().ToString();
            jwt.Expiration = expiration.ToString(format);
            jwt.exp = expiration.ToUnixTimeSeconds().ToString();

            await InvokeAsync(StateHasChanged);
        }

        protected async Task OnUseLocalTimeChanged(ChangeEventArgs e)
        {
            useLocalTime = e.Value is bool b && b;
            await ApplyTimeSettings();
        }

        private async Task<DateTimeOffset> GetLocalTimeAsync()
        {
            if (JS == null)
            {
                return DateTimeOffset.Now;
            }

            try
            {
                BrowserTime browserTime = await JS.InvokeAsync<BrowserTime>("getBrowserTime");
                DateTimeOffset absolute = DateTimeOffset.FromUnixTimeMilliseconds(browserTime.Now);
                TimeSpan offset = TimeSpan.FromMinutes(-browserTime.Offset);
                return new DateTimeOffset(absolute.DateTime + offset, offset);
            }
            catch
            {
                return DateTimeOffset.Now;
            }
        }

        protected void GenerateJWT()
        {
            output = BuildIdToken(jwt.Subject);
        }

        public long ToUnixTime(string value)
        {
            return DateTime.TryParse(value, out DateTime parsed)
                ? ((DateTimeOffset)parsed).ToUnixTimeSeconds()
                : 0;
        }

        public static string BuildIdToken(string Email)
        {
            string issuer = jwt.Issuer;
            string audience = jwt.Audience;

            IList<System.Security.Claims.Claim> claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("sub", Email, System.Security.Claims.ClaimValueTypes.String, issuer)
            };

            // Create the token
            JwtSecurityToken token = new JwtSecurityToken(
                    issuer,
                    audience,
                    claims,
                    DateTime.Parse(jwt.IssuedAt),
                    DateTime.Parse(jwt.Expiration),
                    SigningCredentials.Value);

            // Get the representation of the signed token
            JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();

            return jwtHandler.WriteToken(token);
        }
    }
}
