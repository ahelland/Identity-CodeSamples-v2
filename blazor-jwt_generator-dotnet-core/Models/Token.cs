namespace blazor_jwt_generator_dotnet_core.Models
{
    public class Token
    {
        public string Issuer { get; set; }
        public string IssuedAt { get; set; }
        public string iat { get; set; }
        public string Expiration { get; set; }
        public string exp { get; set; }
        public string Audience { get; set; }
        public string Subject { get; set; }

    }
}
