namespace blazor_easyauth_default_dotnet6.Models
{
    public class UserClaim
    {
        public string typ { get; set; }
        public string val { get; set; }
    }

    public class Tokens
    {
        public string access_token { get; set; }
        public DateTime expires_on { get; set; }
        public string id_token { get; set; }
        public string provider_name { get; set; }
        public string refresh_token { get; set; }
        public List<UserClaim> user_claims { get; set; }
        public string user_id { get; set; }
    }
}
