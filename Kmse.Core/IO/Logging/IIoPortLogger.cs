namespace Kmse.Core.IO.Logging
{
    public interface IIoPortLogger
    {
        void Debug(string port, string message);
        void Information(string port, string message);
        void Error(string port, string message);
        
        void PortRead(ushort address, byte data);
        void PortWrite(ushort address, byte newData);

        void SetMaskableInterruptStatus(bool status);
        void SetNonMaskableInterruptStatus(bool status);
    }
}
