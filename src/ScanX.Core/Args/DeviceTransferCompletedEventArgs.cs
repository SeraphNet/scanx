﻿using System;

namespace ScanX.Core.Args
{
    public class DeviceTransferCompletedEventArgs : EventArgs
    {
        public int TotalPages { get; set; }

        public DeviceTransferCompletedEventArgs(int totalPages)
        {
            this.TotalPages = totalPages;
        }
    }
}
