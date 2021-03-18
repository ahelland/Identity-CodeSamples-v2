using blazor_jwt_generator_dotnet_core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;

namespace blazor_jwt_generator_dotnet_core.Pages
{
    public class B2CTokenGeneratorBase : ComponentBase
    {
        [Inject]
        protected IConfiguration configuration { get; set; }

        private static Lazy<X509SigningCredentials> SigningCredentials;
        protected string SigningCertThumbprint = string.Empty;
        protected static string SigningCertHash = "placeholder";

        public static B2CToken jwt { get; set; }
        public string output = "";

        protected override void OnInitialized()
        {
            string ver = "1.0";
            string iss = "https://contoso.b2clogin.com/9122040d-6c67-4c5b-b112-36a304b66dad/v2.0";
            string aud = "6cb04018-a3f5-46a7-b995-940c78f5aef3";            
            string sub = "6aff436f-5e5b-4549-889f-69303f7d4381";
            string name = "John Doe";
            string given_name = "John";
            string family_name = "Doe";
            string acr = "B2C_1A_CustomPolicy";            
            string nonce = "defaultNonce";            
            string idp = "Contoso B2C";

            string host = configuration.GetSection("JWTSettings")["HostEnvironment"];

            SigningCertThumbprint = configuration.GetSection("JWTSettings")["SigningCertThumbprint"];

            jwt = new B2CToken
            {
                Version = ver,
                Audience = aud,
                IssuedAt = DateTime.UtcNow.ToString(),
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                NotBefore = DateTime.UtcNow.ToString(),
                nbf = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                Expiration = DateTime.UtcNow.AddMinutes(60).ToString(),
                exp = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds().ToString(),
                at = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                auth_time = DateTime.UtcNow.ToString(),
                Issuer = iss,
                sub = sub,
                name = name,       
                nonce = nonce,
                idp = idp,
                given_name = given_name,
                family_name = family_name,                
                acr = acr,
            };

            //One way to handle Windows-based certs
            if (Environment.OSVersion.Platform.ToString() == "Win32NT")
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
            if (Environment.OSVersion.Platform.ToString() == "Unix")
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

            IList<System.Security.Claims.Claim> claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("ver", jwt.Version, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("sub", Subject, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("iat", jwt.iat, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("name", jwt.name, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("given_name", jwt.given_name, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("family_name", jwt.family_name, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("idp", jwt.idp, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("auth_time", jwt.auth_time, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("nonce", jwt.nonce, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("acr", jwt.acr, System.Security.Claims.ClaimValueTypes.String, issuer)
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

