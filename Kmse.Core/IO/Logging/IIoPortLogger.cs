namespace Kmse.Core.IO.Logging
{
    public interface IIoPortLogger
    {
        void Information(string port, string message);
        void Error(string port, string message);
        
        void ReadPort(ushort address, byte data);
        void WritePort(ushort address, byte newData);

        void SetMaskableInterruptStatus(bool status);
        void SetNonMaskableInterruptStatus(bool status);
    }
}
