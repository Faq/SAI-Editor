using Newtonsoft.Json;

namespace SAI_Editor.Classes.Serialization
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class StateObject
    {
        public object ControlValue { get; set; }

        [JsonProperty]
        public string Key { get; set; }

        [JsonProperty]
        public object Value { get; set; }

        [JsonProperty]
        public bool IsList { get; set; }

    }
}
