using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MobileUISupport;

namespace MobileUISupport.Config
{
    public sealed class LookupAnythingConfig
    {
        /// <summary>Enable integrasi dengan Lookup Anything.</summary>
        public bool EnableLookupAnythingIntegration { get; set; } = true;

        /// <summary>Gunakan custom mobile search menu (lebih nyaman untuk touch).</summary>
        public bool UseMobileSearchMenu { get; set; } = true;
    }
}
