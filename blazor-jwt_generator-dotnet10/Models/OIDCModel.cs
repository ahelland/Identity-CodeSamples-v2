using Newtonsoft.Json;

namespace blazor_jwt_generator_dotnet10.Models
{
    public class OIDCModel
    {
        [JsonProperty("issuer")]
        public string Issuer { get; set; } = string.Empty;

        [JsonProperty("jwks_uri")]
        public string JwksUri { get; set; } = string.Empty;

        [JsonProperty("id_token_signing_alg_values_supported")]
        public ICollection<string> IdTokenSigningAlgValuesSupported { get; set; } = new List<string>();
    }
}
