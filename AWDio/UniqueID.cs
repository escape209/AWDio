namespace AWDio
{
    public class UniqueID
    {
        public int pUuid;
        public string uniqueName;
        public string copyName;
        public uint flags;

        public UniqueID(int pUuid, string uniqueName, string copyName, uint flags)
        {
            this.pUuid = pUuid;
            this.uniqueName = uniqueName;
            this.copyName = copyName;
            this.flags = flags;
        }

        public override string ToString()
        {
            string ret = uniqueName;
            if (!string.IsNullOrEmpty(copyName))
            {
                ret += string.Format(" ({0})", copyName);
            }
            return ret;
        }
    }
}
