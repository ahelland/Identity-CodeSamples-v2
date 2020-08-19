using blazor_jwt_validator_dotnet_core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;

namespace blazor_jwt_validator_dotnet_core.Pages
{
    public class IndexBase : ComponentBase
    {
        [Inject]
        protected IConfiguration configuration { get; set; }        
        public static EncodedToken jwt { get; set; }
        public string FormatStatus = "N/A";
        public string MetadataStatus = "N/A";
        public string output = "N/A";
        public string jwtPayload = "N/A";
        public string jwtHeader = "N/A";
        public string jwtSignature = "N/A";
        public JwtSecurityToken token = null;

        protected override void OnInitialized()
        {
            jwt = new EncodedToken
            {
                MetadataAddress = String.Empty,
                Base64Token = String.Empty,
            };       
        }

        protected void HandleValidSubmit()
        {

        }

        protected void ValidateJWTAsync()
        {
            output = string.Empty;

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

            //Check if readable token (string is in a JWT format)
            var readableToken = handler.CanReadToken(jwt.Base64Token);
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
            OpenIdConnectConfiguration openIdConfig = null;
            bool metadataAvailable;
            try
            {
                configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(jwt.MetadataAddress, new OpenIdConnectConfigurationRetriever());
                openIdConfig = configurationManager.GetConfigurationAsync(CancellationToken.None).Result;
                metadataAvailable = true;
                MetadataStatus = $"Successfully loaded metadata.";
            }
            catch (Exception e)
            {
                MetadataStatus = $"Failed to load metadata (skipping signature validation):  {e.Message}";
                metadataAvailable = false;
            }

            TokenValidationParameters validationParameters = null;

            //If we cannot load metadata we fall back
            if (!metadataAvailable)
            {
                validationParameters =
                    new TokenValidationParameters
                    {
                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,
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
                validationParameters =
                    new TokenValidationParameters
                    {
                        ValidIssuer = openIdConfig.Issuer,
                        ValidAudience = jwt.Audience,
                        ValidateLifetime = true,                        
                        ValidateIssuerSigningKey = true,   
                        ValidateAudience = true,
                        ValidateIssuer = true,                        
                        IssuerSigningKeys = openIdConfig.SigningKeys
                    };
            }

            token = handler.ReadJwtToken(jwt.Base64Token);
            try
            {
                SecurityToken validatedToken;                
                var identity = handler.ValidateToken(jwt.Base64Token, validationParameters, out validatedToken);
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
                    customMessage = $"Incorrect Audience. Accepted audience '{validationParameters.ValidateAudience}'. Audience in token '{token.Audiences.FirstOrDefault()}'.";
                }
                if (e.Message.Contains("IDX10205"))
                {
                    customMessage = $"Incorrect Issuer. Accepted issuer '{validationParameters.ValidateIssuer}'. Issuer in token '{token.Issuer}.'";
                }
                //Sginature fail
                if (e.Message.Contains("IDX10501"))
                {
                    customMessage = e.Message;
                }
                //Signature fail
                if (e.Message.Contains("IDX10508"))
                {
                    customMessage = e.Message;
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
