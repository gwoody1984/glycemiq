using Fitbit.Api.Portable;
using Fitbit.Api.Portable.OAuth2;
using Fitbit.Models;
using Glycemiq.WebApi.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.SessionState;

namespace Glycemiq.WebApi.Controllers
{
    [RoutePrefix("fitbit")]
    public class FitbitController : ApiController
    {
        private HttpSessionState session = HttpContext.Current.Session;

        private static string clientId = ConfigurationManager.AppSettings["FitbitClientId"];
        private static string clientSecret = ConfigurationManager.AppSettings["FitbitClientSecret"];
        private static string verificationCode = ConfigurationManager.AppSettings["FitbitSubscriptionVerificationCode"];

        private const string AppCredentials = "AppCredentials";
        private const string FitbitClient = "FitbitClient";

        //
        // GET: /Fitbit/Authorize/
        // Setup - prepare the user redirect to Fitbit.com to prompt them to authorize this app.
        [HttpGet]
        [Route(nameof(Authorize))]
        public IHttpActionResult Authorize()
        {
            var appCredentials = new FitbitAppCredentials()
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            session[AppCredentials] = appCredentials;

            // provide the App Credentials. You get those by registering your app at dev.fitbit.com
            // configure Fitbit authenticaiton request to perform a callback to this controller's result method
            var authenticator = new OAuth2Helper(appCredentials, $"{Request.RequestUri.GetLeftPart(UriPartial.Authority)}/fitbit/{nameof(RegistrationResult).ToLowerInvariant()}");
            string[] scopes = Enum.GetValues(typeof(FitbitAuthScope)).Cast<FitbitAuthScope>().Select(x => x.ToString().ToLowerInvariant()).ToArray();

            string authUrl = authenticator.GenerateAuthUrl(scopes, null);

            return Redirect(authUrl);
        }

        //Final step. Take this authorization information and use it in the app
        [HttpGet]
        [Route(nameof(RegistrationResult))]
        public async Task<IHttpActionResult> RegistrationResult(string code)
        {
            FitbitAppCredentials appCredentials = (FitbitAppCredentials)session[AppCredentials];

            var authenticator = new OAuth2Helper(appCredentials, $"{Request.RequestUri.GetLeftPart(UriPartial.Authority)}/fitbit/{nameof(RegistrationResult).ToLowerInvariant()}");

            OAuth2AccessToken accessToken = await authenticator.ExchangeAuthCodeForAccessTokenAsync(code);

            // Store credentials in FitbitClient. The client in its default implementation manages the Refresh process
            var fitbitClient = GetFitbitClient(accessToken);

            // register a subscription for the authorized user
            // TODO: Remove hardcoded subscriber id -- should be the equivalent of our user id
            await fitbitClient.AddSubscriptionAsync(APICollectionType.activities, "123456789");

            return Ok();
        }

        [HttpGet]
        [Route(nameof(SubscriberEndpoint))]
        public IHttpActionResult SubscriberEndpoint([FromBody] JArray updates, [FromUri] string verify)
        {
            if (verify == verificationCode)
                return StatusCode(HttpStatusCode.NoContent);
            return NotFound();

            // TODO: implement logic to get updates
        }


        /// <summary>
        /// HttpClient and hence FitbitClient are designed to be long-lived for the duration of the session. This method ensures only one client is created for the duration of the session.
        /// More info at: http://stackoverflow.com/questions/22560971/what-is-the-overhead-of-creating-a-new-httpclient-per-call-in-a-webapi-client
        /// </summary>
        /// <returns></returns>
        private FitbitClient GetFitbitClient(OAuth2AccessToken accessToken = null)
        {
            if (session[FitbitClient] == null)
            {
                if (accessToken != null)
                {
                    var appCredentials = (FitbitAppCredentials)session[AppCredentials];
                    FitbitClient client = new FitbitClient(appCredentials, accessToken);
                    session[FitbitClient] = client;
                    return client;
                }
                else
                {
                    throw new Exception("First time requesting a FitbitClient from the session you must pass the AccessToken.");
                }

            }
            else
            {
                return (FitbitClient)session[FitbitClient];
            }
        }

    }
}