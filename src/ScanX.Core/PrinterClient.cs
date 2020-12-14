﻿using Microsoft.Extensions.Logging;
using ScanX.Core.Exceptions;
using ScanX.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text;

namespace ScanX.Core
{
    public class PrinterClient : IDisposable , IPrinterClient
    {
        private readonly PrinterSettings _defaultPrinterSettings;
        private readonly ILogger<PrinterClient> _logger;
        private readonly PrintDocument _printDocument;
        private MemoryStream _ms;


        public PrinterClient(ILogger<PrinterClient> logger)
        {
            _logger = logger;

            _defaultPrinterSettings = new PrinterSettings();
            
            _printDocument = new PrintDocument();
            _printDocument.PrintPage += OnPrintingPage;
        }

        public string GetDefaultPrinter()
        {
            return _defaultPrinterSettings.PrinterName;
        }

        public void Print(byte[] imageToPrint,PrintSettings settings)
        {
            var printerSettings = new PrinterSettings()
            {
                PrinterName = settings.PrinterName,
            };

            _ms = new MemoryStream();

            _ms.Write(imageToPrint, 0, imageToPrint.Length);

            _printDocument.PrinterSettings = printerSettings;
            _printDocument.DefaultPageSettings.Margins = new Margins(10, 20, 300, 0);
            _printDocument.Print();
        }

        private void OnPrintingPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                var leftMargin = e.MarginBounds.Left;
                var rightMargin = e.MarginBounds.Right;
                var topMargin = e.MarginBounds.Top;
                var bottomMargin = e.MarginBounds.Bottom;

                var img = Image.FromStream(_ms);

                e.Graphics.DrawImage(img,leftMargin,topMargin,200,300);
            }
            catch (Exception)
            {

            }
            finally
            {
                _ms.Flush();
            }
            
        }

        public void Dispose()
        {

        }

        private void ValidateSettings(PrintSettings settings)
        {
            var printerName = settings.PrinterName;

            if (string.IsNullOrWhiteSpace(printerName))
                throw new PrintException("Printer name cannot be null");

            var printers = PrinterSettings.InstalledPrinters;

            var allPrinters = new List<string>();

            foreach (string item in printers)
            {
                allPrinters.Add(item);
            }

            if (!allPrinters.Any(a => a == printerName))
                throw new PrintException($"Cannot find printer with name \"{printerName}\"");

        }
    }
}
