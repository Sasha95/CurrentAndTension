using System;
using System.Threading;
using System.IO.Ports;
using System.Windows.Forms;
namespace PWM
{
    public class Port : IDisposable
    {
        const int speed = 19200;
        const Parity parity = Parity.None;
        const int dataBits = 8;
        const StopBits stopBits = StopBits.Two;
        SerialPort port;
        static public string[] GetPortNames()
        {
            return SerialPort.GetPortNames();
        }
        
        public Port(string name)
        {
            port = new SerialPort(name, speed, parity, dataBits, stopBits);
            port.Open();
        }
        public void Dispose()
        {
            port.Close();
        }
        public int GetData()
        {
            var buf = new byte[2];
            for (int i = 0; i < 2; i++)
            {
                port.Read(buf, i, 1);
            }
            var C = buf[0] + (buf[1] << 8);
            //double T = C & 1023;
            //var C = buf[0]+ (buf[1] << 8) + (buf[2] << 16);
            return C;
        }

        public void SetData(byte[] i)
        {
            for (int j = 0; j < 3; j++)
            {
                port.Write(i, j, 1);
                //Thread.Sleep(5);
            }
                      
        }
    }
}
