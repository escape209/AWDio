using System;

using Newtonsoft.Json;

namespace AWDio
{
    public class UniqueID
    {
        [JsonProperty(Order = 0)]
        public string Name { get; set; } = string.Empty;

        [JsonProperty(Order = 1)]
        public Guid Uuid { get; set; } = Guid.Empty;

        [JsonProperty(Order = 2)]
        public int pUuid;
        [JsonProperty(Order = 3)]
        public int pName;
        [JsonProperty(Order = 4)]
        public uint flags;

        public UniqueID() { }
        public UniqueID(int pUuid, int pName, uint flags)
        {
            this.pUuid = pUuid;
            this.pName = pName;
            this.flags = flags;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
