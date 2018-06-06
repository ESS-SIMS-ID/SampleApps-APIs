using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using OneRoster_MVC_Hybrid_Client;
using Owin;

[assembly: OwinStartup(typeof(Startup))]
namespace OneRoster_MVC_Hybrid_Client
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {

            var stsServer = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["sts-server"]) ? ConfigurationManager.AppSettings["sts-server"] : "";
            var redirectUri = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["redirect-uri"]) ? ConfigurationManager.AppSettings["redirect-uri"] : "";
            var clientId = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["client-id"]) ? ConfigurationManager.AppSettings["client-id"] : "";
            var clientSecret = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["client-secret"]) ? ConfigurationManager.AppSettings["client-secret"] : "";
            var clientScope = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["client-scope"]) ? ConfigurationManager.AppSettings["client-scope"] : "";

            JwtSecurityTokenHandler.InboundClaimTypeMap = new Dictionary<string, string>();

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "Cookies"
            });

            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                ClientId = clientId,
                Authority = stsServer,
                RedirectUri = redirectUri,
                PostLogoutRedirectUri = redirectUri,
                ResponseType = "code id_token",
                Scope = clientScope,

                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role"
                },

                SignInAsAuthenticationType = "Cookies",

                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthorizationCodeReceived = async n =>
                    {
                        // use the code to get the access and refresh token
                        var tokenClient = new TokenClient(
                            $"{stsServer}/connect/token/",
                            clientId,
                            clientSecret);

                        var tokenResponse = await tokenClient.RequestAuthorizationCodeAsync(
                            n.Code, n.RedirectUri);

                        if (tokenResponse.IsError)
                        {
                            throw new Exception(tokenResponse.Error);
                        }

                        // use the access token to retrieve claims from userinfo
                        var userInfoClient = new UserInfoClient(
                        new Uri($"{stsServer}/connect/userinfo/").ToString());

                        var userInfoResponse = await userInfoClient.GetAsync(tokenResponse.AccessToken);

                        // create new identity
                        var id = new ClaimsIdentity(n.AuthenticationTicket.Identity.AuthenticationType);
                        id.AddClaims(userInfoResponse.Claims);

                        id.AddClaim(new Claim("access_token", tokenResponse.AccessToken));
                        id.AddClaim(new Claim("expires_at", DateTime.Now.AddSeconds(tokenResponse.ExpiresIn).ToLocalTime().ToString(CultureInfo.CurrentCulture)));
                        id.AddClaim(new Claim("id_token", n.ProtocolMessage.IdToken));
                        id.AddClaim(new Claim("sid", n.AuthenticationTicket.Identity.FindFirst("sid").Value));

                        n.AuthenticationTicket = new AuthenticationTicket(
                            new ClaimsIdentity(id.Claims, n.AuthenticationTicket.Identity.AuthenticationType, "name", "role"),
                            n.AuthenticationTicket.Properties);
                    },

                    RedirectToIdentityProvider = n =>
                    {
                        // if signing out, add the id_token_hint
                        if (n.ProtocolMessage.RequestType == OpenIdConnectRequestType.LogoutRequest)
                        {
                            var idTokenHint = n.OwinContext.Authentication.User.FindFirst("id_token");

                            if (idTokenHint != null)
                            {
                                n.ProtocolMessage.IdTokenHint = idTokenHint.Value;
                            }
                        }

                        return Task.FromResult(0);

                    }
                }
            });
        }
    }
}