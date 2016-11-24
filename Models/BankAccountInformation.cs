using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bank_Bot.Models
{
    public class BankAccountInformation
    {

        [JsonProperty(PropertyName = "Id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "money")]
        public double Money { get; set; }

        [JsonProperty(PropertyName = "accountNumber")]
        public string accountNumber { get; set; }
        
        [JsonProperty(PropertyName = "bestFriend")]
        public string bestFriend { get; set; }

        [JsonProperty(PropertyName = "password")]
        public string password { get; set; }
    }
}