using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StanleyReports
{
    internal class Entry
    {
        internal DateTime dateTime { get; set; }
        internal int KeyholderID { get; set; }
        internal int EntrySourceID { get; set; }
        internal int ExtraSourceID { get; set; }

        internal Entry(DateTime dt, int khid, int esid, int xsid)
        {
            dateTime = dt;
            KeyholderID = khid;
            EntrySourceID = esid;
            ExtraSourceID = xsid;
        }
    }
}
