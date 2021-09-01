﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

using Azure.Search.Documents.Indexes;

namespace CognitiveServicesDemo.Models
{
    public class SearchIndexUserMediaCosmos
    {
        [JsonPropertyName("id")]
        [SearchableField(IsKey = true, IsFacetable = true, IsFilterable = true, IsSortable = true)]
        public string id { get; set; }

        [JsonPropertyName("userid")]
        [SearchableField(IsFacetable = true, IsFilterable = true, IsSortable = true)]
        public string userid { get; set; }

        [JsonPropertyName("mediafilename")]
        [SearchableField(IsFacetable = true, IsFilterable = true, IsSortable = true)]
        public string mediafilename { get; set; }

        [JsonPropertyName("mediafiletype")]
        [SearchableField(IsFacetable = true, IsFilterable = true, IsSortable = true)]
        public string mediafiletype { get; set; }

        [JsonPropertyName("mediaurl")]
        [SearchableField(IsFacetable = true, IsFilterable = true, IsSortable = true)]
        public string mediaurl { get; set; }

        [JsonPropertyName("tags")]
        public List<SearchIndexUserMediaCosmosTag> Tags { get; set; }

        [JsonPropertyName("datetimeuploaded")]
        [SimpleField(IsFacetable = true, IsFilterable = true, IsSortable = true)]
        public string datetimeuploaded { get; set; }

        [JsonPropertyName("search_score")]
        public string search_score { get; set; }
    }

    public class SearchIndexUserMediaCosmosTag {

        [JsonPropertyName("name")]
        [SearchableField(IsFacetable = true, IsFilterable = true, IsSortable = false)]
        public string name { get; set; }

        [JsonPropertyName("confidence")]
        [SearchableField(IsFacetable = true, IsFilterable = true, IsSortable = false)]
        public double confidence { get; set; }

        [JsonPropertyName("type")]
        [SearchableField(IsFacetable = true, IsFilterable = true, IsSortable = false)]
        public int type { get; set; }
    }
}