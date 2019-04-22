#r "Newtonsoft.Json"

using System;
using System.Net;
using System.Net.Http.Formatting;
using Newtonsoft.Json;

public static async Task<object> Run(HttpRequestMessage request, TraceWriter log)
{
    log.Info($"Webhook was triggered!");

    string requestContentAsString = await request.Content.ReadAsStringAsync();
    dynamic requestContentAsJObject = JsonConvert.DeserializeObject(requestContentAsString);

    log.Info($"Request: {requestContentAsString}");

    if (requestContentAsJObject.emailAddress == null)
    {
        log.Info($"Empty request");
        return request.CreateResponse(HttpStatusCode.OK);
    }

    var email = ((string)requestContentAsJObject.emailAddress).ToLower();
    log.Info($"email: {email}");

    char splitter = '@';
    string[] splitEmail = email.Split(splitter);
    var emailSuffix = splitEmail[1];

    //For the "aad" identity provider
    if (email == "bar@foo.com")
    {
        log.Info($"Identity Provider: aad");
        return request.CreateResponse<ResponseContent>(
            HttpStatusCode.OK,
            new ResponseContent
            {
                version = "1.0.0",
                status = (int)HttpStatusCode.OK,
                userMessage = $"Your account is a generic Azure AD account.",
                idp = "aad",
                signInName = email
            },
            new JsonMediaTypeFormatter(),
            "application/json");
    }

    //For B2C local accounts
    if (email == "foo@bar.com")
    {
        log.Info($"Identity Provider: local");
        return request.CreateResponse<ResponseContent>(
            HttpStatusCode.OK,
            new ResponseContent
            {
                version = "1.0.0",
                status = (int)HttpStatusCode.OK,
                userMessage = $"Your account seems to be a local account.",
                idp = "local",
                signInName = email
            },
            new JsonMediaTypeFormatter(),
            "application/json");
    }

    //For Contoso AAD accounts
    if (emailSuffix == "contoso.com")
    {
        log.Info($"Identity Provider: contoso");
        return request.CreateResponse<ResponseContent>(
            HttpStatusCode.OK,
            new ResponseContent
            {
                version = "1.0.0",
                status = (int)HttpStatusCode.OK,
                userMessage = $"Your account belongs to the Contoso Identity Provider",
                idp = "contoso",
                signInName = email
            },
            new JsonMediaTypeFormatter(),
            "application/json");
    }

    else
    {
        log.Info($"Identity Provider: none");
        return request.CreateResponse<BlankContent>(
            HttpStatusCode.OK,
            new BlankContent
            {
                status = (int)HttpStatusCode.OK,
                signInName = email
            },
            new JsonMediaTypeFormatter(),
            "application/json");
    }

}

//Default responses where there is no match
public class BlankContent
{
    public int status { get; set; }
    public string signInName { get; set; }
}

//For responses where there is an IdP matching
public class ResponseContent
{
    public string version { get; set; }
    public int status { get; set; }
    public string userMessage { get; set; }
    public string idp { get; set; }
    public string signInName { get; set; }
}