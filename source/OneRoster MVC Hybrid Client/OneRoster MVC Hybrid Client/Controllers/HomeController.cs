using System;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Mvc;
using OneRoster_MVC_Hybrid_Client.Models;

namespace OneRoster_MVC_Hybrid_Client.Controllers
{
    [Authorize]
    [RemoteRequireHttps]
    public class HomeController : Controller
    {

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ShowTokensAndClaims()
        {
            var model = new ShowTokensAndClaimsModel();
            if (User is ClaimsPrincipal claimsPrincipal)
            {
                var idTokenHandler = new JwtSecurityTokenHandler();
                var idToken = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "id_token");
                if (idToken != null)
                {
                    var idJsonToken = idTokenHandler.ReadToken(idToken.Value) as JwtSecurityToken;
                    model.IdToken = idJsonToken;
                    model.IdTokenRaw = idToken.Value;
                }

                var accessTokenHandler = new JwtSecurityTokenHandler();
                var accessToken = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "access_token");
                if (accessToken != null)
                {
                    var accessJsonToken = accessTokenHandler.ReadToken(accessToken.Value) as JwtSecurityToken;
                    model.AccessToken = accessJsonToken;
                    model.AccessTokenRaw = accessToken.Value;
                }

            }
            return View(model);
        }

        public ActionResult GetStudents()
        {
            var model = new StudentsModel();
            var studentsEndpoint = "/ims/oneroster/v1p1/students";
            var response = string.Empty;
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["api-uri"]))
            {
                var accessToken = string.Empty;
                if (User is ClaimsPrincipal claimsPrincipal)
                {
                    accessToken = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;
                }

                var baseAddress = new Uri(ConfigurationManager.AppSettings["api-uri"]);
                var client = new HttpClient
                {
                    BaseAddress = baseAddress
                };
                client.SetBearerToken(accessToken);
                model.Students = client.GetStringAsync(studentsEndpoint).Result;
            }

            return View(model);
        }
    }
}