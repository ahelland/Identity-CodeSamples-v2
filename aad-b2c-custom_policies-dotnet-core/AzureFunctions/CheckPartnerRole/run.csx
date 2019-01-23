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

    //Uncomment to validate email claim is present
    //if (requestContentAsJObject.emailAddress == null)
    //{
    //    log.Info($"Bad request");
    //    return request.CreateResponse(HttpStatusCode.BadRequest);
    //}

    //Hardcoded for illustration/testing purposes
    var email = "bob@contoso.com";

    //var email = ((string) requestContentAsJObject.emailAddress).ToLower();
    var role = ((string)requestContentAsJObject.role).ToLower();

    log.Info($"email: {email}, role: {role}");

    char splitter = '@';
    string[] splitEmail = email.Split(splitter);
    var emailSuffix = splitEmail[1];

    //Dummy values :)
    if ((role == "partner" && email != "chuck@norris.com") || (role == "partner" && emailSuffix != "northwind.com"))
    {
        return request.CreateResponse<ResponseContent>(
            HttpStatusCode.Conflict,
            new ResponseContent
            {
                version = "1.0.0",
                status = (int)HttpStatusCode.Conflict,
                userMessage = $"Your account does not seem to be a partner account."
            },
            new JsonMediaTypeFormatter(),
            "application/json");
    }

    return request.CreateResponse(HttpStatusCode.OK);
}

public class ResponseContent
{
    public string version { get; set; }
    public int status { get; set; }
    public string userMessage { get; set; }
}