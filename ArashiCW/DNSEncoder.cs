using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ARSoft.Tools.Net.Dns;

namespace ArashiCW
{
    class DNSEncoder
    {
        private static MethodInfo info;

        public static void Init()
        {
            foreach (var mInfo in new DnsMessage().GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
                if (mInfo.ToString() == "Int32 Encode(Boolean, Byte[] ByRef)")
                    info = mInfo;
        }

        public static byte[] Encode(DnsMessage dnsQMsg)
        {
            var args = new object[] { false, null };
            if (info != null) info.Invoke(dnsQMsg, args);
            var dnsBytes = args[1] as byte[];
            if (dnsBytes != null && dnsBytes[2] == 0) dnsBytes[2] = 1;
            return dnsBytes;
        }
    }
}
