﻿using Microsoft.AspNetCore.Mvc;
using ScanX.Core;
using ScanX.Protocol.Helpers;
using ScanX.Protocol.ViewModels;
using System.Collections.Generic;

namespace ScanX.Protocol.Controllers
{
    public class PrinterController : ApiBaseController
    {
        private readonly IPrinterClient _client;

        public PrinterController(IPrinterClient printerClient)
        {
            _client = printerClient;
        }

        public IActionResult Get()
        {
            List<string> result = new List<string>();

            using (DeviceClient client = new DeviceClient())
            {
                result = client.GetAllPrinters();
            }
            return Ok(result);
        }

        [Route("default")]
        public IActionResult GetDefaultPrinter()
        {
            var printerName = _client.GetDefaultPrinter();

            return Ok(printerName);
        }

        [Route("print")]
        [HttpPost]
        public IActionResult Print([FromBody] PrintViewModel model)
        {

            var settings = model.ToModel();

            var imageBytes = ImageHelper.FromBase64(model.ImageData, out string type);


            _client.Print(imageBytes, settings);


            return Ok("doc printeds");
        }
    }
}