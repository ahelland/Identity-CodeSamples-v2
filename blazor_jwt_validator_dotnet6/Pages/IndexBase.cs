using blazor_jwt_validator_dotnet6.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace blazor_jwt_validator_dotnet6.Pages
{
    public class IndexBase : ComponentBase
    {
        [Inject]
        protected IConfiguration Configuration { get; set; }
        //public static EncodedToken Jwt { get; set; }
        public static EncodedToken Jwt = new();
        public string FormatStatus = "N/A";
        public string MetadataStatus = "N/A";
        public string output = "N/A";
        public string jwtPayload = "N/A";
        public string jwtHeader = "N/A";
        public string jwtSignature = "N/A";
        public JwtSecurityToken token = new();

        protected override void OnInitialized() => Jwt = new EncodedToken
        {
            MetadataAddress = String.Empty,
            Base64Token = String.Empty,
        };

        protected void HandleValidSubmit()
        {

        }

        protected void ValidateJWTAsync()
        {
            output = string.Empty;

            JwtSecurityTokenHandler handler = new();

            //Check if readable token (string is in a JWT format)
            var readableToken = handler.CanReadToken(Jwt.Base64Token);
            if (readableToken != true)
            {
                FormatStatus = "The token doesn't seem to be in a proper JWT format.";
                return;
            }
            if (readableToken == true)
            {
                FormatStatus = "The token seems to be in a proper JWT format.";
            }

            //Load Metadata if available
            IConfigurationManager<OpenIdConnectConfiguration> configurationManager;
            OpenIdConnectConfiguration? openIdConfig = null;
            bool metadataAvailable;
            try
            {
                configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(Jwt.MetadataAddress, new OpenIdConnectConfigurationRetriever());
                openIdConfig = configurationManager.GetConfigurationAsync(CancellationToken.None).Result;
                metadataAvailable = true;
                MetadataStatus = $"Successfully loaded metadata.";
            }
            catch (Exception e)
            {
                MetadataStatus = $"Failed to load metadata (skipping signature validation):  {e.Message}";
                metadataAvailable = false;
            }

            TokenValidationParameters validationParameters = new();

            //If we cannot load metadata we fall back
            if (!metadataAvailable)
            {
                validationParameters =
                    new TokenValidationParameters
                    {
                        ValidIssuer = Jwt.Issuer,
                        ValidAudience = Jwt.Audience,
                        ValidateLifetime = true,
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        //Needed to force disabling signature validation                        
                        SignatureValidator = delegate (string token, TokenValidationParameters parameters)
                        {
                            var jwt = new JwtSecurityToken(token);
                            return jwt;
                        },
                        ValidateIssuerSigningKey = false,
                    };
            }

            //If we succcessfully loaded metadata we do signature validation as well
            if (metadataAvailable)
            {
                if (openIdConfig != null)
                {
                    validationParameters =
                        new TokenValidationParameters
                        {
                            ValidIssuer = openIdConfig.Issuer,
                            ValidAudience = Jwt.Audience,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidateAudience = true,
                            ValidateIssuer = true,
                            IssuerSigningKeys = openIdConfig.SigningKeys
                        };

                    Jwt.Issuer = openIdConfig.Issuer;
                }
            }

            token = handler.ReadJwtToken(Jwt.Base64Token);
            try
            {
                var identity = handler.ValidateToken(Jwt.Base64Token, validationParameters, out SecurityToken validatedToken);
                if (metadataAvailable)
                {
                    output += "Token is valid according to metadata!";
                }
                else
                {
                    output += "Token is valid according to a self-evaluation!";
                }

                jwtSignature = token.RawSignature;

            }
            catch (Exception e)
            {
                //Due to a bug in the AAD IdentityModel extension we need custom handling to print out the attributes in the error message.
                var customMessage = string.Empty;

                if (e.Message.Contains("IDX10223"))
                {
                    customMessage = $"Time values not valid. Current time: {DateTime.UtcNow.ToShortDateString()} {DateTime.UtcNow.ToLongTimeString()}. Token valid from: '{token.ValidFrom.ToShortDateString()} {token.ValidFrom.ToLongTimeString()}' Token valid to: '{token.ValidTo.ToShortDateString()} {token.ValidTo.ToLongTimeString()}'";
                }
                if (e.Message.Contains("IDX10214"))
                {
                    customMessage = $"Incorrect Audience. Accepted audience '{Jwt.Audience}'. Audience in token '{token.Audiences.FirstOrDefault()}'.";
                }
                if (e.Message.Contains("IDX10205"))
                {
                    customMessage = $"Incorrect Issuer. Accepted issuer '{Jwt.Issuer}'. Issuer in token '{token.Issuer}.'";
                }
                //Unable to obtain metadata
                if (e.Message.Contains("IDX20803"))
                {
                    customMessage = $"Unable to obtain configuration.";
                }
                //Signature fail
                if (e.Message.Contains("IDX10501"))
                {
                    customMessage = $"Signature validation failed. Unable to match key: {token.Header.Kid}";
                }
                //Signature fail
                if (e.Message.Contains("IDX10508"))
                {
                    customMessage = "Signature validation failed. Signature is improperly formatted.";
                }
                else if (customMessage == string.Empty)
                {
                    customMessage = e.Message;
                }
                output += $"Token failed to validate. {Environment.NewLine}{customMessage}";
            }
        }
    }
}
