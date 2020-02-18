using SendGrid.Helpers.Mail;

namespace aad_b2c_mailengine_dotnet_core.Models
{
    public class SignInLinkModel
    {        
        public EmailAddress from { get; set; }
        public string subject { get; set; }
        public EmailAddress to { get; set; }
    }

    public class SignUpLinkModel
    {        
        public EmailAddress from { get; set; }
        public string subject { get; set; }
        public EmailAddress to { get; set; }
        public string displayName { get; set; }
        public string givenName { get; set; }
        public string surname { get; set; }
    }
}
