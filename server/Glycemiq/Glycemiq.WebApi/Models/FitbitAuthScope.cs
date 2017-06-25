using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glycemiq.WebApi.Models
{
    [Flags]
    public enum FitbitAuthScope
    {
        Activity,
        Heartrate,
        Location,
        Nutrition,
        Sleep,
        Weight
    }
}