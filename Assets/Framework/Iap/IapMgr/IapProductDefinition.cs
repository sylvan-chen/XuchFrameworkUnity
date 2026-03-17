using System;
using Newtonsoft.Json;

namespace Framework.Core
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