using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace CognitiveServicesDemo.Models
{
    [JsonObject]
    public class UserMediaCosmosJSON : UserMediaJSON
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
