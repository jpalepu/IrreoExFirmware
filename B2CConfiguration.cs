using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IrreoExFirmware
{
    internal class B2CConfiguration
    {
        public string ClientId { get; set; }
        public string Authority { get; set; }
        public string RedirectUri { get; set; }
        public IEnumerable<string> Scopes { get; set; }
    }
}
