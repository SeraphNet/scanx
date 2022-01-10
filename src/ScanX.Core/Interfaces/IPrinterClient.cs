using ScanX.Core.Models;

namespace ScanX.Core
{
    public interface IPrinterClient
    {
        string GetDefaultPrinter();

        void Print(byte[] imageToPrint, PrintSettings settings);
    }
}
