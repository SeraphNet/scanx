﻿using Microsoft.AspNetCore.Mvc;
using ScanX.Core;
using ScanX.Core.Models;
using System.Collections.Generic;

namespace ScanX.Protocol.Controllers
{
    public class ScannerController : ApiBaseController
    {
        public IActionResult Get()
        {
            List<ScannerDevice> result = new List<ScannerDevice>();

            using (DeviceClient client = new DeviceClient())
            {
                result = client.GetAllScanners();
            }
            return Ok(result);
        }
    }
}