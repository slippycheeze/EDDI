﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EliteDangerousEDDNResponder
{
    class EDDNOutfittingMessage : EDDNMessage
    {
        public List<string> modules;

        public EDDNOutfittingMessage()
        {
            modules = new List<string>();
        }
    }
}
