using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using blazor_jwt_generator_dotnet_core.Models;

namespace blazor_jwt_generator_dotnet_core.Pages
{
    public class B2ETokenGeneratorBase : ComponentBase
    {
        [Inject]
        protected IConfiguration configuration { get; set; }

        private static Lazy<X509SigningCredentials> SigningCredentials;
        protected string SigningCertThumbprint = string.Empty;
        protected static string SigningCertHash = "placeholder";

        public static B2EToken jwt { get; set; }
        public string output = "";

        protected override void OnInitialized()
        {
            string iss = "https://login.microsoftonline.com/common/v2.0";
            string aud = "6cb04018-a3f5-46a7-b995-940c78f5aef3";
            string preferred_username = configuration.GetSection("JWTSettings")["DefaultSubject"];
            string sub = "AAAAAAAAAAAAAAAAAAAAAIkzqFVrSaSaFHy782bbtaQ";
            string name = "John Doe";
            string ver = "2.0";
            string nonce = "12345";
            string oid = "00000000-0000-0000-66f3-3332eca7ea81";
            string tid = "9122040d-6c67-4c5b-b112-36a304b66dad";
            string aio = "Df2UVXL1ix!lMCWMSOJBcFatzcGfvFGhjKv8q5g0x732dR5MB5BisvGQO7YWByjd8iQDLq!eGbIDakyp5mnOrcdqHeYSnltepQmRp6AIZ8jY";

            string host = configuration.GetSection("JWTSettings")["HostEnvironment"];

            SigningCertThumbprint = configuration.GetSection("JWTSettings")["SigningCertThumbprint"];

            jwt = new B2EToken
            {
                Version = ver,
                Audience = aud,
                IssuedAt = DateTime.UtcNow.ToString(),
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                NotBefore = DateTime.UtcNow.ToString(),
                nbf = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                Expiration = DateTime.UtcNow.AddMinutes(60).ToString(),
                exp = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds().ToString(),                
                Issuer = iss,
                sub = sub,
                name = name,
                preferred_username = preferred_username,
                oid = oid,
                tid = tid,
                nonce = nonce,
                aio = aio,
            };

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
                        SigningCertHash = Base64UrlEncoder.Encode(certCollection[0].GetCertHash());
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

                SigningCertHash = Base64UrlEncoder.Encode(cert.GetCertHash());

                SigningCredentials = new Lazy<X509SigningCredentials>(() =>
                {
                    if (cert != null)
                    {
                        return new X509SigningCredentials(cert);
                    }

                    throw new Exception("Certificate not found");
                });
            }
        }

        protected void HandleValidSubmit()
        {

        }

        protected void GenerateJWT()
        {
            output = BuildIdToken(jwt.sub);
        }
        public static string BuildIdToken(string Subject)
        {
            string issuer = jwt.Issuer;
            string audience = jwt.Audience;

            IList<System.Security.Claims.Claim> claims = new List<System.Security.Claims.Claim>();
            claims.Add(new System.Security.Claims.Claim("ver", jwt.Version, System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("sub", Subject, System.Security.Claims.ClaimValueTypes.String, issuer));            
            claims.Add(new System.Security.Claims.Claim("iat", jwt.iat, System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("name", jwt.name, System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("preferred_username", jwt.preferred_username, System.Security.Claims.ClaimValueTypes.String, issuer));            
            claims.Add(new System.Security.Claims.Claim("oid", jwt.oid, System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("tid", jwt.tid, System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("nonce", jwt.nonce, System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("aio", jwt.aio, System.Security.Claims.ClaimValueTypes.String, issuer));

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
