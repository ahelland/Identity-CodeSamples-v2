namespace blazor_jwt_generator_dotnet10.Models
{
    public class BrowserTime
    {
        public long Now { get; set; }
        public int Offset { get; set; }
    }

    public class GenericToken
    {
        public string Issuer { get; set; } = string.Empty;
        public string IssuedAt { get; set; } = string.Empty;
        public string iat { get; set; } = string.Empty;
        public string Expiration { get; set; } = string.Empty;
        public string exp { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
    }

    public class B2EToken
    {
        public string Version { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string sub { get; set; } = string.Empty;
        public string NotBefore { get; set; } = string.Empty;
        public string nbf { get; set; } = string.Empty;
        public string IssuedAt { get; set; } = string.Empty;
        public string iat { get; set; } = string.Empty;
        public string Expiration { get; set; } = string.Empty;
        public string exp { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string preferred_username { get; set; } = string.Empty;
        public string oid { get; set; } = string.Empty;
        public string tid { get; set; } = string.Empty;
        public string nonce { get; set; } = string.Empty;
        public string aio { get; set; } = string.Empty;
    }

    public class B2CToken
    {
        public string Version { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string sub { get; set; } = string.Empty;
        public string NotBefore { get; set; } = string.Empty;
        public string nbf { get; set; } = string.Empty;
        public string IssuedAt { get; set; } = string.Empty;
        public string iat { get; set; } = string.Empty;
        public string Expiration { get; set; } = string.Empty;
        public string exp { get; set; } = string.Empty;
        public string at { get; set; } = string.Empty;
        public string auth_time { get; set; } = string.Empty;
        public string nonce { get; set; } = string.Empty;
        public string idp { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string given_name { get; set; } = string.Empty;
        public string family_name { get; set; } = string.Empty;
        public string acr { get; set; } = string.Empty;
    }
}
