using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using blazor_jwt_generator_dotnet6.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;

namespace blazor_jwt_generator_dotnet6.Pages
{
    public class IndexBase : ComponentBase
    {
        [Inject]
        protected IConfiguration configuration { get; set; }

        private static Lazy<X509SigningCredentials> SigningCredentials;
        protected string SigningCertThumbprint = string.Empty;

        public static GenericToken jwt { get; set; }
        public string output = "";

        protected override void OnInitialized()
        {
            string iss = configuration.GetSection("JWTSettings")["Issuer"];
            string aud = configuration.GetSection("JWTSettings")["Audience"];
            string sub = configuration.GetSection("JWTSettings")["DefaultSubject"];

            SigningCertThumbprint = configuration.GetSection("JWTSettings")["SigningCertThumbprint"];

            jwt = new GenericToken
            {
                Audience = aud,
                IssuedAt = DateTime.UtcNow.ToString(),
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                Expiration = DateTime.UtcNow.AddMinutes(60).ToString(),
                exp = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds().ToString(),
                Issuer = iss,
                Subject = sub,
            };

            //If we run on Windows the certificate needs to be in cert store
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
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

                    return null;
                });
            }

            //If Linux
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
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

                    var cert = new X509Certificate2(bytes);

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

        protected void GenerateJWT()
        {
            output = BuildIdToken(jwt.Subject);
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
