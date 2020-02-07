namespace blazor_jwt_generator_dotnet_core.Models
{
    public class GenericToken
    {
        public string Issuer { get; set; }
        public string IssuedAt { get; set; }
        public string iat { get; set; }
        public string Expiration { get; set; }
        public string exp { get; set; }
        public string Audience { get; set; }
        public string Subject { get; set; }

    }

    public class B2EToken
    {
        public string Version { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string sub { get; set; }
        public string NotBefore { get; set; }
        public string nbf { get; set; }
        public string IssuedAt { get; set; }
        public string iat { get; set; }
        public string Expiration { get; set; }
        public string exp { get; set; }
        public string name { get; set; }
        public string preferred_username { get; set; }
        public string oid { get; set; }
        public string tid { get; set; }
        public string nonce { get; set; }
        public string aio { get; set; }

    }
}
