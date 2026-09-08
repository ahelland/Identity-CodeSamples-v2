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
    public class B2ETokenGeneratorBase : ComponentBase
    {
        [Inject]
        protected IConfiguration configuration { get; set; } = default!;

        [Inject]
        protected IJSRuntime JS { get; set; } = default!;

        private static Lazy<X509SigningCredentials> SigningCredentials = default!;
        protected string SigningCertThumbprint = string.Empty;
        protected static string SigningCertHash = "placeholder";

        public static B2EToken jwt { get; set; } = default!;
        public string output = "";
        public bool useLocalTime { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            string iss = "https://login.microsoftonline.com/common/v2.0";
            string aud = "6cb04018-a3f5-46a7-b995-940c78f5aef3";
            string preferred_username = configuration.GetSection("JWTSettings")["DefaultSubject"] ?? string.Empty;
            string sub = "AAAAAAAAAAAAAAAAAAAAAIkzqFVrSaSaFHy782bbtaQ";
            string name = "John Doe";
            string ver = "2.0";
            string nonce = "12345";
            string oid = "00000000-0000-0000-66f3-3332eca7ea81";
            string tid = "9122040d-6c67-4c5b-b112-36a304b66dad";
            string aio = "Df2UVXL1ix!lMCWMSOJBcFatzcGfvFGhjKv8q5g0x732dR5MB5BisvGQO7YWByjd8iQDLq!eGbIDakyp5mnOrcdqHeYSnltepQmRp6AIZ8jY";

            string host = configuration.GetSection("JWTSettings")["HostEnvironment"] ?? string.Empty;

            SigningCertThumbprint = configuration.GetSection("JWTSettings")["SigningCertThumbprint"] ?? string.Empty;

            jwt = new B2EToken
            {
                Version = ver,
                Audience = aud,
                Issuer = iss,
                sub = sub,
                name = name,
                preferred_username = preferred_username,
                oid = oid,
                tid = tid,
                nonce = nonce,
                aio = aio,
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
            jwt.NotBefore = now.ToString(format);
            jwt.nbf = now.ToUnixTimeSeconds().ToString();
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
            output = BuildIdToken(jwt.sub);
        }

        public long ToUnixTime(string value)
        {
            return DateTime.TryParse(value, out DateTime parsed)
                ? ((DateTimeOffset)parsed).ToUnixTimeSeconds()
                : 0;
        }

        public static string BuildIdToken(string Subject)
        {
            string issuer = jwt.Issuer;
            string audience = jwt.Audience;

            IList<System.Security.Claims.Claim> claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("ver", jwt.Version, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("sub", Subject, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("iat", jwt.iat, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("name", jwt.name, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("preferred_username", jwt.preferred_username, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("oid", jwt.oid, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("tid", jwt.tid, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("nonce", jwt.nonce, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("aio", jwt.aio, System.Security.Claims.ClaimValueTypes.String, issuer)
            };

            // Create the token
            JwtSecurityToken token = new JwtSecurityToken(
                    issuer,
                    audience,
                    claims,
                    DateTime.Parse(jwt.IssuedAt),
                    DateTime.Parse(jwt.Expiration),
                    SigningCredentials.Value);


            //AAD v2 tokens doesn't use the x5t claim
            token.Header.Remove("x5t");

            //AAD doesn't use kid/thumbprint, but the hash so a "hack" is required
            token.Header.Remove("kid");
            token.Header.Add("kid", SigningCertHash);

            // Get the representation of the signed token
            JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();

            return jwtHandler.WriteToken(token);
        }
    }
}
