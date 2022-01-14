using Microsoft.Extensions.Logging;
using ScanX.Core.Args;
using ScanX.Core.Exceptions;
using ScanX.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WIA;

namespace ScanX.Core
{
    //for more info https://ourcodeworld.com/articles/read/382/creating-a-scanning-application-in-winforms-with-csharp
    public class DeviceClient : IDisposable
    {
        public const uint WIA_ERROR_PAPER_EMPTY = 0x80210003;
        public const uint WIA_ERROR_COVER_OPEN = 0x80210016;
        public const uint WIA_ERROR_DEVICE_COMMUNICATION = 0x8021000A;
        public const uint WIA_ERROR_DEVICE_LOCKED = 0x8021000D;
        public const uint WIA_ERROR_SCANNER_JAM = 0x80210002;

        public object WIA_IPS_BRIGHTNESS { get; private set; }

        public event EventHandler OnImageScanned;

        private readonly ILogger _logger;

        private readonly DeviceManager _deviceManager;

        public DeviceClient()
        {
            _deviceManager = new DeviceManager();
        }

        public DeviceClient(ILogger logger) : this()
        {
            _logger = logger;
        }

        public List<string> GetAllPrinters()
        {
            List<string> result = new List<string>();

            var printers = PrinterSettings.InstalledPrinters;

            foreach (string item in printers)
            {
                result.Add(item);
            }

            return result;

        }

        public List<ScannerDevice> GetAllScanners()
        {
            var result = new List<ScannerDevice>();

            var deviceInfos = _deviceManager.DeviceInfos;

            for (int i = 0; i < deviceInfos.Count; i++)
            {
                var info = deviceInfos[i + 1];

                if (info.Type == WiaDeviceType.ScannerDeviceType)
                {
                    result.Add(new ScannerDevice()
                    {
                        DeviceId = info.DeviceID,
                        Name = info.Properties["Name"].get_Value().ToString(),
                        Description = info.Properties["Description"]?.get_Value()?.ToString(),
                        Port = info.Properties["Port"]?.get_Value()?.ToString()
                    });
                }
            }

            return result;
        }

        public void Scan(string deviceID, ScanSetting setting = null, bool scanAllPages = false)
        {
            if (setting == null)
                setting = new ScanSetting();

            int page = 1;

            IDeviceInfo device = GetDeviceById(deviceID);

            Device connectedDevice;

            try
            {
                connectedDevice = device.Connect();

                SetDeviceSettings(connectedDevice, setting);

                do
                {
                    page = ScanImage(connectedDevice, page, setting);
                }
                while (scanAllPages);
            }
            catch (COMException ex)
            {
                var excpetion = GetException(ex);

                _logger?.LogError(ex.ToString());

                if (page != 1 && excpetion.Code != ScanXExceptionCodes.NoPaper)
                    throw excpetion;

            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.ToString());

                throw new ScanXException($"Error: {ex.ToString()}", ex, ScanXExceptionCodes.UnkownError);
            }
        }

        private Bitmap CropImage(ImageFile image)
        {
            var imageBytes = (byte[])image.FileData.get_BinaryData();
            var ms = new MemoryStream(imageBytes);
            var img = Image.FromStream(ms);
            int Height = img.Height;
            int Width = img.Width;

            Bitmap bm = new Bitmap(img);



            Func<int, bool> IsAllWhiteRow = row =>
            {
                for (int i = 0; i < Width; i += 2)
                {
                    if (bm.GetPixel(i, row).R != 255)
                    {
                        return false;
                    }
                }
                return true;
            };

            int leftMost = 0;

            int rightMost = Width - 1;

            int topMost = 0;            

            int bottomMost = Height - 1; 
            for (int row = bottomMost; row > 0; row -= 1)
            {
                if (IsAllWhiteRow(row)) bottomMost = row - 1;
                else break;
            }

            if (rightMost == 0 && bottomMost == 0 && leftMost == Width && topMost == Height)
            {
                return bm;
            }

            int croppedWidth = rightMost - leftMost + 1;
            int croppedHeight = bottomMost - topMost + 1;
            try
            {
                Bitmap target = new Bitmap(croppedWidth, croppedHeight);
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(bm,
                            new RectangleF(0, 0, croppedWidth, croppedHeight),
                            new RectangleF(leftMost, topMost, croppedWidth, croppedHeight),
                            GraphicsUnit.Pixel);
                }
                return target;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Values are top={0} bottom={1} left={2} right={3}", topMost, bottomMost, leftMost, rightMost), ex);
            }

        }
        public static byte[] ImageToByte(Image img)
        {
            using (var stream = new MemoryStream())
            {
                img.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
        private int ScanImage(Device connectedDevice, int page, ScanSetting setting)
        {
            var img = (ImageFile)connectedDevice.Items[1].Transfer(FormatID.wiaFormatPNG);

            Bitmap cropped = CropImage(img);

            byte[] dataConverted = ImageToByte(cropped);

            var args = new DeviceImageScannedEventArgs(dataConverted, "png", page)
            {
                Height = cropped.Height,
                Width = cropped.Width,
                Settings = setting
            };

            OnImageScanned?.Invoke(this, args);

            page++;
            return page;
        }


        public void ScanWithUI(int deviceID)
        {
            CommonDialogClass dlg = new CommonDialogClass();


        }

        public string GetDefualtPrinter()
        {
            var defualtPrinter = new PrinterSettings();

            return defualtPrinter.PrinterName;
        }

        public void Print(string imageLocation, PrintSettings setting = null)
        {
            Print(imageLocation, GetDefualtPrinter(), setting);
        }

        public void Print(string imageLocation, string deviceID, PrintSettings setting = null)
        {
            if (setting == null)
                setting = new PrintSettings();

            try
            {
                PrintDocument pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = deviceID;
                pd.DocumentName = imageLocation;
                pd.PrintPage += PrintPage;
                pd.Print();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.ToString());

                throw new ScanXException($"Error: {ex.ToString()}", ex, ScanXExceptionCodes.UnkownError);
            }
        }

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            string imageLocation = ((PrintDocument)sender).DocumentName; // Current file name 
            System.Drawing.Image img = System.Drawing.Image.FromFile(imageLocation);
            Point loc = new Point(100, 100);
            e.Graphics.DrawImage(img, loc);
        }

        public List<DeviceProperty> GetDeviceProperties(string id)
        {
            List<DeviceProperty> result = new List<DeviceProperty>();

            IDeviceInfo device = GetDeviceById(id);

            foreach (IProperty item in device.Properties)
            {
                result.Add(new DeviceProperty()
                {
                    Id = item.PropertyID,
                    Name = item.Name,
                    Value = item.get_Value()
                });
            }

            return result;
        }

        public List<DeviceProperty> GetDeviceConnectProperties(string id)
        {
            List<DeviceProperty> result = new List<DeviceProperty>();

            IDeviceInfo device = GetDeviceById(id);

            var connectedDevice = device.Connect();

            foreach (IProperty item in connectedDevice.Properties)
            {
                result.Add(new DeviceProperty()
                {
                    Id = item.PropertyID,
                    Name = item.Name,
                    Value = item.get_Value()
                });
            }

            result = result.OrderBy(a => a.Name).ToList();

            return result;
        }

        public List<DeviceProperty> GetItemDeviceConnectProperties(string id)
        {
            List<DeviceProperty> result = new List<DeviceProperty>();

            IDeviceInfo device = GetDeviceById(id);

            var connectedDevice = device.Connect();

            foreach (IProperty item in connectedDevice.Items[1].Properties)
            {
                result.Add(new DeviceProperty()
                {
                    Id = item.PropertyID,
                    Name = item.Name,
                    Value = item.get_Value()
                });
            }

            result = result.OrderBy(a => a.Name).ToList();

            return result;
        }

        private IDeviceInfo GetDeviceById(string deviceID)
        {
            if (string.IsNullOrWhiteSpace(deviceID))
                throw new ScanXException("Please select a scanner device", ScanXExceptionCodes.NoDevice);

            var count = _deviceManager.DeviceInfos.Count;

            for (int i = 0; i < count; i++)
            {
                IDeviceInfo device = _deviceManager.DeviceInfos[i + 1];

                if (device.DeviceID == deviceID)
                {
                    return device;
                }
            }

            throw new ScanXException($"No scanner device named: {deviceID} found", ScanXExceptionCodes.NoDevice);
        }

        private void SetDeviceSettings(Device connectedDevice, ScanSetting setting)
        {


            var properties = connectedDevice.Items[1].Properties;

            SetWIAProperty(properties, ScanSetting.WIA_ITEM_SIZE, 0);

            SetWIAProperty(properties, ScanSetting.WIA_PAGE_SIZE, 100);

            SetWIAProperty(properties, ScanSetting.WIA_COLOR_MODE, (int)setting.Color);

            SetWIAProperty(properties, ScanSetting.WIA_HORIZONTAL_SCAN_RESOLUTION_DPI, (int)setting.Dpi);

            SetWIAProperty(properties, ScanSetting.WIA_VERTICAL_SCAN_RESOLUTION_DPI, (int)setting.Dpi);

            SetWIAProperty(properties, ScanSetting.WIA_VERTICAL_EXTENT, 4200); // Legal size at 300 dpi

        }

        private void SetWIAProperty(IProperties properties, int propertyId, object value)
        {
            try
            {
                for (int i = 0; i < properties.Count; i++)
                {
                    var index = i + 1;

                    if (properties[index].PropertyID.Equals(propertyId))
                    {
                        properties[index].set_Value(value);
                    }

                    Debug.WriteLine($"{properties[index].Name}: {properties[index].PropertyID}");
                }
            }
            catch (Exception ex)
            {
                var msg = $"unable to set properties: {ex}";

                _logger?.LogWarning(msg);

                Debug.WriteLine(msg);
            }

        }

        private ScanXException GetException(COMException ex)
        {
            uint errorCode = (uint)ex.HResult;

            switch (errorCode)
            {
                case 0x80210006:

                    return new ScanXException("The device is busy. Close any apps that are using this device or wait for it to finish and then try again.", ScanXExceptionCodes.DeviceBusy);

                case 0x80210016:

                    return new ScanXException("One or more of the device’s cover is open", ScanXExceptionCodes.CoverOpen);

                case 0x8021000A:

                    return new ScanXException("Communication with the WIA device failed. Make sure that the device is powered on and connected to the PC. If the problem persists, disconnect and reconnect the device.", ScanXExceptionCodes.CommunicationWithDeviceFailed);

                case 0x8021000D:

                    return new ScanXException("The device is locked. Close any apps that are using this device or wait for it to finish and then try again.", ScanXExceptionCodes.DeviceLocked);

                case 0x8021000E:

                    return new ScanXException("The device driver threw an exception.", ScanXExceptionCodes.DeviceDriverError);

                case 0x80210001:

                    return new ScanXException("An unknown error has occurred with the WIA device.", ScanXExceptionCodes.UnkownError);

                case 0x8021000C:

                    return new ScanXException("There is an incorrect setting on the WIA device.", ScanXExceptionCodes.IconrrectSetting);

                case 0x8021000B:

                    return new ScanXException("The device doesn't support this command.", ScanXExceptionCodes.NotSupportedCommand);

                case 0x8021000F:

                    return new ScanXException("The response from the driver is invalid.", ScanXExceptionCodes.DeviceDriverInvlid);

                case 0x80210009:

                    return new ScanXException("The WIA device was deleted. It's no longer available.", ScanXExceptionCodes.ItemDeleted);

                case 0x80210017:

                    return new ScanXException("The scanner's lamp is off.", ScanXExceptionCodes.ScannerLampIsOff);

                case 0x80210021:

                    return new ScanXException("A scan job was interrupted because an Imprinter/Endorser item reached the maximum valid value for WIA_IPS_PRINTER_ENDORSER_COUNTER, and was reset to 0.", ScanXExceptionCodes.ScannerInterupted);

                case 0x80210020:

                    return new ScanXException("A scan error occurred because of a multiple page feed condition.", ScanXExceptionCodes.MultipageFeedCondition);

                case 0x80210005:

                    return new ScanXException("The device is offline. Make sure the device is powered on and connected to the PC.", ScanXExceptionCodes.DeviceOffline);

                case 0x80210003:

                    return new ScanXException("There are no documents in the document feeder.", ScanXExceptionCodes.NoPaper);

                case 0x80210002:

                    return new ScanXException("Paper is jammed in the scanner's document feeder.", ScanXExceptionCodes.PaperJammed);

                case 0x80210004:

                    return new ScanXException("An unspecified problem occurred with the scanner's document feeder.", ScanXExceptionCodes.DocumentFeeder);

                case 0x80210007:

                    return new ScanXException("The device is warming up.", ScanXExceptionCodes.DeviceIsWarmpingUp);

                case 0x80210008:

                    return new ScanXException("There is a problem with the WIA device. Make sure that the device is turned on, online, and any cables are properly connected.", ScanXExceptionCodes.DeviceOffline);

                case 0x80210015:

                    return new ScanXException("No scanner device was found. Make sure the device is online, connected to the PC, and has the correct driver installed on the PC.", ScanXExceptionCodes.NoDevice);

                default:
                    return new ScanXException("Unkown Error", ex, ScanXExceptionCodes.UnkownError);
            }
        }

        public void Dispose()
        {
            Marshal.ReleaseComObject(_deviceManager);
        }
    }
}
