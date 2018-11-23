using NLog;
using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace NbIotBackend
{
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public long dwServiceType;
        public ServiceState dwCurrentState;
        public long dwControlsAccepted;
        public long dwWin32ExitCode;
        public long dwServiceSpecificExitCode;
        public long dwCheckPoint;
        public long dwWaitHint;
    };

    public partial class NbIotBackend : ServiceBase
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private NbIotListener nbiotListener = new NbIotListener();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private Task nbiotBackendTask;

        public NbIotBackend()
        {
            InitializeComponent();
        }
                
        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            _logger.Info("NB-IoT backend service is started.");

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            try
            {
                nbiotBackendTask = Task.Run(() => nbiotListener.RunAsync(cts.Token));
            }
            catch(Exception ex)
            {
                _logger.Fatal(ex, "UDP listener failed.");
            }
        }

        protected override void OnStop()
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            _logger.Info("NB-IoT backend service is stopping.");

            cts.Cancel();

            try
            {
                nbiotBackendTask.Wait();
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Failed to stop the UDP listener.");
            }

            _logger.Info("NB-IoT backend service is finished.");

            // Update the service state to Stopped.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
