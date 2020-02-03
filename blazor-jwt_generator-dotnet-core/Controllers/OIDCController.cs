using System;
using System.Security.Cryptography.X509Certificates;
using blazor_jwt_generator_dotnet_core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Newtonsoft.Json;

namespace blazor_jwt_generator_dotnet_core.Controllers
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

        [Route(".well-known/openid-configuration", Name = "OIDCMetadata")]
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
            string host = configuration.GetSection("JWTSettings")["HostEnvironment"];

            SigningCertThumbprint = configuration.GetSection("JWTSettings")["SigningCertThumbprint"];

            //One way to handle Windows-based certs
            if (host.ToLower() == "windows")
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

                    throw new Exception("Certificate not found");
                });
            }

            //And another way to handle Linux certs
            if (host.ToLower() == "linux")
            {
                var bytes = System.IO.File.ReadAllBytes($"/var/ssl/private/{SigningCertThumbprint}.p12");
                var cert = new X509Certificate2(bytes);

                SigningCredentials = new Lazy<X509SigningCredentials>(() =>
                {
                    if (cert != null)
                    {
                        return new X509SigningCredentials(cert);
                    }

                    throw new Exception("Certificate not found");
                });
            }

            return Content(JsonConvert.SerializeObject(new JWKSModel
            {
                Keys = new[] { JwksKeyModel.FromSigningCredentials(SigningCredentials.Value) }
            }), "application/json");
        }        
    }
}