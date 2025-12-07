using System;
using Newtonsoft.Json;

namespace DigiEden.Framework
{
    [Serializable]
    internal struct IapProductDefinition
    {
        [JsonProperty("id")]
        public string Id;

        [JsonProperty("type")]
        public int Type;
    }
}