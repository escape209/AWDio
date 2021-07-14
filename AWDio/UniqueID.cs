using System;

namespace AWDio
{
    public class UniqueID
    {
        public int pUuid;
        public int pName;
        public uint flags;

        public string Name { get; set; } = string.Empty;
        public Guid Uuid { get; set; } = Guid.Empty;

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
