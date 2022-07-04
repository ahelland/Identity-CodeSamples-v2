using aad_b2c_custom_policies_dotnet6.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace aad_b2c_custom_policies_dotnet6.Pages
{
    public class IndexBase : ComponentBase
    {       
        public static ProtocolParams parameters = new();

        public string url = "MicrosoftIdentity/Account/SignIn";

        protected override void OnInitialized()
        {
            parameters = new ProtocolParams
            {
                loginHint = String.Empty,
                domainHint = String.Empty,
                locale = String.Empty,
                customParamKey = String.Empty,
                customParamValue = String.Empty,
                policy = String.Empty,
                tokenHint = String.Empty
            };
        }

        protected void HandleValidSubmit()
        {

        }               

        protected Task GenerateUrl()
        {
            var queryParams = new Dictionary<string, string?>();

            if (!string.IsNullOrEmpty(parameters.loginHint))
            {                
                queryParams.Add("loginHint", parameters.loginHint);
            }
            if (!string.IsNullOrEmpty(parameters.domainHint))
            {
                queryParams.Add("idp", parameters.domainHint);
            }
            if (!string.IsNullOrEmpty(parameters.locale))
            {
                queryParams.Add("locale", parameters.locale);
            }
            if (!string.IsNullOrEmpty(parameters.customParamKey))
            {
                if (!string.IsNullOrEmpty(parameters.customParamValue))
                    queryParams.Add(parameters.customParamKey, parameters.customParamValue);
            }
            if (!string.IsNullOrEmpty(parameters.policy))
            {
                queryParams.Add("policy", parameters.policy);
            }
            if (!string.IsNullOrEmpty(parameters.tokenHint))
            {
                queryParams.Add("tokenHint", parameters.tokenHint);
            }

            url = QueryHelpers.AddQueryString("MicrosoftIdentity/Account/SignIn", queryParams);
            return Task.CompletedTask;
        }
    }
}
