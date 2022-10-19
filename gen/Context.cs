
using System.Collections.Generic;
using org.unirail.Agent;
using org.unirail.collections;

namespace org.unirail
{
    public class Context
    {

        public Dictionary < uint, ulong >.Enumerator   Dictionary_uint_ulong__Enumerator;
        public Dictionary < ushort, uint >.Enumerator   Dictionary_ushort_uint__Enumerator;
        public Dictionary < ushort, Project.Host.Langs >.Enumerator   Dictionary_ushort_Project_Host_Langs__Enumerator;

        public interface Provider
        {
            Context context {get;}
        }
    }
}
