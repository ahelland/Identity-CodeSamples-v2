using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using blazor_jwt_generator_dotnet6.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace blazor_jwt_generator_dotnet6.Controllers 
{

    [ApiController]
    public class OIDCController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private static Lazy<X509SigningCredentials> SigningCredentials;
        private string SigningCertThumbprint = string.Empty;

        public OIDCController(IConfiguration config)
        {
            configuration = config;
        }

        [Route(".well-known/openid-configuration")]
        public ActionResult Metadata()
        {
            return Content(JsonConvert.SerializeObject(new OIDCModel
            {
                Issuer = configuration.GetSection("JWTSettings")["Issuer"],

                JwksUri = Url.Link("JWKS", null),

                IdTokenSigningAlgValuesSupported = new[] { configuration.GetSection("JWTSettings")["SigningCertAlgorithm"], },
            }), "application/json"); ;
        }

        [Route(".well-known/keys", Name = "JWKS")]
        public ActionResult JwksDocument()
        {
            var SigningCertThumbprint = configuration.GetSection("JWTSettings")["SigningCertThumbprint"];
            Lazy<X509SigningCredentials>? SigningCredentials = null;

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

            if (SigningCredentials == null)
                return null;

            return Content(JsonConvert.SerializeObject(new JWKSModel
            {
                Keys = new[] { JwksKeyModel.FromSigningCredentials(SigningCredentials.Value) }
            }), "application/json");
        }
    }
}