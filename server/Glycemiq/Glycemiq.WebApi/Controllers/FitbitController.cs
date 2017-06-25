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
        HttpSessionState Session = HttpContext.Current.Session;

        //
        // GET: /Fitbit/Authorize/
        // Setup - prepare the user redirect to Fitbit.com to prompt them to authorize this app.
        [HttpGet]
        [Route(nameof(Authorize))]
        public IHttpActionResult Authorize()
        {
            var appCredentials = new FitbitAppCredentials()
            {
                ClientId = ConfigurationManager.AppSettings["FitbitClientId"],
                ClientSecret = ConfigurationManager.AppSettings["FitbitClientSecret"]
            };
            //make sure you've set these up in Web.Config under <appSettings>:

            Session["AppCredentials"] = appCredentials;

            //Provide the App Credentials. You get those by registering your app at dev.fitbit.com
            //Configure Fitbit authenticaiton request to perform a callback to this constructor's Callback method
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
            FitbitAppCredentials appCredentials = (FitbitAppCredentials)Session["AppCredentials"];

            var authenticator = new OAuth2Helper(appCredentials, $"{Request.RequestUri.GetLeftPart(UriPartial.Authority)}/fitbit/{nameof(RegistrationResult).ToLowerInvariant()}");

            OAuth2AccessToken accessToken = await authenticator.ExchangeAuthCodeForAccessTokenAsync(code);

            // Store credentials in FitbitClient. The client in its default implementation manages the Refresh process
            var fitbitClient = GetFitbitClient(accessToken);

            // register a subscription for the authorized user
            // TODO: This is failing because we don't have a publicly reachable Subscriber Endpoint
            // TODO: Remove hardcoded subscriber id -- should be the equivalent of our user id
            await fitbitClient.AddSubscriptionAsync(APICollectionType.user, "123456789");  

            return Ok();
        }

        [HttpPost]
        [Route(nameof(SubscriberEndpoint))]
        public IHttpActionResult SubscriberEndpoint([FromBody] JArray updates)
        {
            // TODO: Do something here to verify that the endpoint is reachable
            return StatusCode(HttpStatusCode.NoContent);
        }
        

        /// <summary>
        /// HttpClient and hence FitbitClient are designed to be long-lived for the duration of the session. This method ensures only one client is created for the duration of the session.
        /// More info at: http://stackoverflow.com/questions/22560971/what-is-the-overhead-of-creating-a-new-httpclient-per-call-in-a-webapi-client
        /// </summary>
        /// <returns></returns>
        private FitbitClient GetFitbitClient(OAuth2AccessToken accessToken = null)
        {
            if (Session["FitbitClient"] == null)
            {
                if (accessToken != null)
                {
                    var appCredentials = (FitbitAppCredentials)Session["AppCredentials"];
                    FitbitClient client = new FitbitClient(appCredentials, accessToken);
                    Session["FitbitClient"] = client;
                    return client;
                }
                else
                {
                    throw new Exception("First time requesting a FitbitClient from the session you must pass the AccessToken.");
                }

            }
            else
            {
                return (FitbitClient)Session["FitbitClient"];
            }
        }

    }
}