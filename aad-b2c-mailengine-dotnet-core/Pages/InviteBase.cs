using aad_b2c_mailengine_dotnet_core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace aad_b2c_mailengine_dotnet_core.Pages
{
    public class InviteBase : ComponentBase
    {
        [Inject]
        protected IConfiguration configuration { get; set; }
        private static Lazy<X509SigningCredentials> SigningCredentials;
        protected string SigningCertThumbprint = string.Empty;

        public static SignUpLinkModel mailer { get; set; }

        protected override void OnInitialized()
        {
            string host = configuration.GetSection("MailerSettings")["HostEnvironment"];
            SigningCertThumbprint = configuration.GetSection("MailerSettings")["SigningCertThumbprint"];

            var toEmail = "john.doe@contoso.com";
            var toName = "John Doe";

            var fromEmail = configuration.GetSection("MailerSettings")["MailFromEmail"];
            var fromName = configuration.GetSection("MailerSettings")["MailFromName"];

            mailer = new SignUpLinkModel
            {
                to = new EmailAddress(toEmail, toName),
                from = new EmailAddress(fromEmail, fromName),
                subject = configuration.GetSection("MailerSettings")["InviteLinkMailSubject"],
                displayName = "John Doe",
                givenName = "John",
                surname = "Doe",
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
        }

        protected void HandleValidSubmit()
        {

        }

        protected async Task SendInviteLinkAsync()
        {
            string email = mailer.to.Email;
            string token = BuildIdToken(email);
            string link = BuildUrl(token);
            string Body = string.Empty;
            string htmlTemplate = System.IO.File.ReadAllText("SignUpTemplate.html");

            var apiKey = configuration.GetSection("MailerSettings")["SendGridApiKey"];
            var client = new SendGridClient(apiKey);
            var plainTextContent = "You should be seeing a SignUp link below.";
            var htmlContent = string.Format(htmlTemplate, email, link);
            var msg = MailHelper.CreateSingleEmail(mailer.from, mailer.to, mailer.subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
        }
        private string BuildIdToken(string Email)
        {
            string B2CClientId = configuration.GetSection("MailerSettings")["B2CClientId"];
            double LinkExpiresAfterMinutes;
            double.TryParse(configuration.GetSection("MailerSettings")["LinkExpiresAfterMinutes"], out LinkExpiresAfterMinutes);
            
            string issuer = configuration.GetSection("MailerSettings")["issuer"];

            // All parameters send to Azure AD B2C needs to be sent as claims
            IList<System.Security.Claims.Claim> claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("email", Email, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("displayName", mailer.displayName, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("givenName", mailer.givenName, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("surname", mailer.surname, System.Security.Claims.ClaimValueTypes.String, issuer),
                new System.Security.Claims.Claim("family_name", mailer.surname, System.Security.Claims.ClaimValueTypes.String, issuer)
            };

            // Create the token
            JwtSecurityToken token = new JwtSecurityToken(
                    issuer,
                    B2CClientId,
                    claims,
                    DateTime.Now,
                    DateTime.Now.AddMinutes(LinkExpiresAfterMinutes),
                    SigningCredentials.Value);

            // Get the representation of the signed token
            JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();

            return jwtHandler.WriteToken(token);
        }

        private string BuildUrl(string token)
        {
            string B2CSignUpUrl = configuration.GetSection("MailerSettings")["B2CSignUpUrl"];

            return $"{B2CSignUpUrl}?id_token_hint={token}";
        }
    }
}
