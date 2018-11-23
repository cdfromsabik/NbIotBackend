using NLog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NbIotBackend
{
    public class NbIotListener
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private const int _listenPort = 1234;
        private static IPEndPoint _ipEndpoint = new IPEndPoint(IPAddress.Any, _listenPort);
        private static UdpClient _udpClient = new UdpClient(_ipEndpoint);

        public NbIotListener()
        {
            IPEndPoint originator = new IPEndPoint(IPAddress.Any, 0);
        }

        public async Task RunAsync(CancellationToken cts)
        {
            // TODO: Replace the following with your own logic.
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    await ReceivePacketsAsync();
                }
                catch(Exception ex)
                {
                    _logger.Fatal(ex, "Failed to receive UDP datagrams.");
                }

                await Task.Delay(1000);
            }
        }

        private static async Task<string> ReceivePacketsAsync()
        {
            var receiveTask = _udpClient.ReceiveAsync();

            var receive = await receiveTask;

            _logger.Info($"From {receive.RemoteEndPoint.Address}:{receive.RemoteEndPoint.Port}");
            string request = Encoding.ASCII.GetString(receive.Buffer, 0, receive.Buffer.Length);
            _logger.Info($"{request}");

            var data = Encoding.ASCII.GetBytes($"Received {receive.Buffer.Length} bytes.");
            await _udpClient.SendAsync(data, data.Length, receive.RemoteEndPoint);

            return request;
        }
    }
}
