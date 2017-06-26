using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glycemiq.WebApi.Models
{
    public class FitbitNotification
    {
        public string CollectionType { get; set; }
        public string Date { get; set; }
        public string OwnerId { get; set; }
        public string OwnerType { get; set; }
        public string SubscriptionId { get; set; }
    }
}