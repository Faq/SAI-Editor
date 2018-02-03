using Newtonsoft.Json;

namespace SAI_Editor.Classes.Serialization
{
    [JsonObject]
    public class ScriptContainer
    {
        [JsonProperty]
        public string TypeName { get; set; }

        [JsonProperty]
        public object Value { get; set; }

    }
}
