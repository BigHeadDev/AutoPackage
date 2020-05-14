using System;
using System.Collections.Generic;
using System.Text;

namespace AutoPackgeCore.Model {
    internal class USBDevice {
        public string DevId { get; set; }
        public DeviceStatus Status { get; set; }
    }
    internal enum DeviceStatus {
        Connected,
        Shared,
        Busy
    }
}
