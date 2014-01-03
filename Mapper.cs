namespace Maklerzentrum.NamedPipeToComMapper
{
    using System;
    using System.Diagnostics;
    using System.IO.Pipes;
    using System.IO.Ports;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Mapper
    {
        private readonly string connectionData;
        private readonly byte[] pipeToComBuffer = new byte[1 << 16];
        private readonly byte[] comToPipeBuffer = new byte[1 << 16];

        private Thread comThread;
        private Thread pipeThread;
        private SerialPort comPort;
        private NamedPipeClientStream pipe;
        private volatile bool stop;

        private string comPortName;
        private string pipeName;

        public Mapper(string connectionData)
        {
            this.connectionData = connectionData;
        }

        public void Start()
        {
            try
            {
                this.DoStart();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                Task.Run((Action)this.Restart);
            }
        }

        private void DoStart()
        {
            this.stop = false;

            this.comThread = new Thread(this.ComProcessor)
            {
                IsBackground = false,
            };

            this.pipeThread = new Thread(this.PipeProcessor)
            {
                IsBackground = false
            };

            var data = this.connectionData.Split('|');
            var comParameters = data[1].Split(',');
            this.comPort = new SerialPort(
                comParameters[0],
                Convert.ToInt32(comParameters[1]),
                (Parity)Enum.Parse(typeof(Parity), comParameters[2]),
                Convert.ToInt32(comParameters[3]),
                (StopBits)Enum.Parse(typeof(StopBits), comParameters[4]))
            {
                DtrEnable = true,
                DiscardNull = true,
                ReadBufferSize = this.comToPipeBuffer.Length,
                WriteBufferSize = this.comToPipeBuffer.Length,
                Handshake = Handshake.RequestToSend,
                ReceivedBytesThreshold = 1,
            };
            this.comPort.Open();
            Debug.WriteLine("{0} opened", comParameters[0]);

            this.pipe = new NamedPipeClientStream(".", data[0], PipeDirection.InOut, PipeOptions.Asynchronous);
            this.pipe.Connect();
            Debug.WriteLine("{0} connected", data[0]);
            this.pipe.ReadMode = PipeTransmissionMode.Byte;

            this.comPortName = comParameters[0];
            this.pipeName = data[0];

            this.comThread.Start();
            this.pipeThread.Start();
        }

        public void Stop()
        {
            this.stop = true;
            try
            {
                this.comThread.Interrupt();
                this.pipeThread.Interrupt();
                this.comPort.Close();
                this.comPort.Dispose();
                this.pipe.Dispose();
            }
            catch
            {
            }
        }

        public void Restart()
        {
            this.Stop();
            Debug.WriteLine("Restarting...");
            if (this.comThread.IsAlive)
            {
                this.comThread.Join();
            }

            if (this.pipeThread.IsAlive)
            {
                this.pipeThread.Join();
            }

            Thread.Sleep(5000);
            this.Start();
        }

        private void PipeProcessor(object obj)
        {
            while (!this.stop)
            {
                try
                {
                    var count = this.pipe.Read(this.pipeToComBuffer, 0, this.pipeToComBuffer.Length);
                    if (count == 0)
                    {
                        Task.Run((Action)this.Restart);
                        return;
                    }

                    Debug.WriteLine(
                        "{2}->{3}: {1} ({0} bytes)",
                        count,
                        Encoding.ASCII.GetString(this.pipeToComBuffer, 0, count).Replace("\r", "\\r").Replace("\n", "\\n"),
                        this.pipeName,
                        this.comPortName);
                    this.comPort.Write(this.pipeToComBuffer, 0, count);
                }
                catch (Exception ex)
                {
                    this.GenericExceptionHandle(ex);
                }
            }
        }

        private void ComProcessor(object obj)
        {
            while (!this.stop)
            {
                try
                {
                    var count = 0;
                    do
                    {
                        count += this.comPort.Read(this.comToPipeBuffer, count, this.comToPipeBuffer.Length - count);
                    }
                    while (this.comPort.BytesToRead > 0);

                    Debug.WriteLine(
                        "{3}->{2}: {1} ({0} bytes ({0})",
                        count,
                        Encoding.ASCII.GetString(this.comToPipeBuffer, 0, count).Replace("\r", "\\r").Replace("\n", "\\n"),
                        this.pipeName,
                        this.comPortName);
                    this.pipe.Write(this.comToPipeBuffer, 0, count);
                }
                catch (Exception ex)
                {
                    this.GenericExceptionHandle(ex);
                }
            }
        }

        private void GenericExceptionHandle(Exception ex)
        {
            if (ex is ThreadInterruptedException || this.stop)
            {
                return;
            }

            Debug.WriteLine(@"Got ""{0}"", restarting", ex.Message);
            Task.Run((Action)this.Restart);
        }
    }
}
