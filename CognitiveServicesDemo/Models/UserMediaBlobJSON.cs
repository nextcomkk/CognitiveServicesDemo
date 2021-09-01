using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace CognitiveServicesDemo.Models
{
    [JsonObject]
    public class UserMediaBlobJSON : UserMediaJSON
    {
        [JsonProperty("key")]
        public string Key { get; set; }
    }
}
