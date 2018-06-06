using System.IdentityModel.Tokens;

namespace OneRoster_MVC_Hybrid_Client.Models
{
    public class ShowTokensAndClaimsModel
    {
        public JwtSecurityToken IdToken { get; set; }
        public string IdTokenRaw { get; set; }
        public JwtSecurityToken AccessToken { get; set; }
        public string AccessTokenRaw { get; set; }
    }

    public class StudentsModel
    {
        public string Students { get; set; }
    }
}