using System;

namespace AwdIO.Rwa
{
    public class RwaUniqueID
    {
        public string Name { get; set; } = string.Empty;
        public Guid Uuid { get; set; } = Guid.Empty;

        public int pUuid;
        public int pName;
        public uint flags;

        public RwaUniqueID() : this(-1, -1, 0) { }
        public RwaUniqueID(int pUuid, int pName, uint flags)
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
