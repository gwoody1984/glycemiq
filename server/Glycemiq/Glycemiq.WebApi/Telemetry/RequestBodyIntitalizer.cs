using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace Glycemiq.WebApi.Telemetry
{
    public class RequestBodyInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            var verb = HttpContext.Current.Request.HttpMethod;
            var requestTelemetry = telemetry as RequestTelemetry;
            if (requestTelemetry != null && (verb == HttpMethod.Post.ToString() || verb == HttpMethod.Put.ToString()))
            {
                using (var reader = new StreamReader(HttpContext.Current.Request.InputStream))
                {
                    string requestBody = reader.ReadToEnd();
                    requestTelemetry.Properties.Add("body", requestBody);
                }
            }
        }
    }
}