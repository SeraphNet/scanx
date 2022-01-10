using ScanX.Core.Models;
using System.Collections.Generic;

namespace ScanX.Protocol.ViewModels
{
    public class DeviceViewModel
    {
        public List<string> Printers { get; set; }

        public List<ScannerDevice> Scanners { get; set; }
    }
}
