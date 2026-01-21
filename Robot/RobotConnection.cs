using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SystemLogin.Robotics
{
    public sealed class Robot
    {
        private TcpClient? _clientDashboard;
        private TcpClient? _clientUrscript;
        private Stream? _streamDashboard;
        private StreamReader? _streamReaderDashboard;
        private Stream? _streamUrscript;

        public Robot(string ipAddress = "172.20.254.206", int dashboardPort = 29999, int urscriptPort = 30002)
        {
            IpAddress = ipAddress;
            DashboardPort = dashboardPort;
            UrscriptPort = urscriptPort;
        }

        public string IpAddress { get; set; }
        public int DashboardPort { get; set; }
        public int UrscriptPort { get; set; }

        public bool ProgramRunning
        {
            get
            {
                if (_clientDashboard?.Connected == true)
                {
                    SendDashboard("running\n");
                    return ReadLineDashboard() == "Program running: true";
                }

                return false;
            }
        }

        public bool Connected => _clientDashboard?.Connected == true && _clientUrscript?.Connected == true;

        public string RobotMode
        {
            get
            {
                SendDashboard("robotmode\n");
                return ReadLineDashboard();
            }
        }

        public void Connect()
        {
            Disconnect();

            _clientDashboard = new TcpClient();
            _clientDashboard.Connect(IpAddress, DashboardPort);
            _streamDashboard = _clientDashboard.GetStream();
            _streamReaderDashboard = new StreamReader(_streamDashboard, Encoding.ASCII);

            // Consume Dashboard welcome message
            ReadLineDashboard();

            _clientUrscript = new TcpClient();
            _clientUrscript.Connect(IpAddress, UrscriptPort);
            _streamUrscript = _clientUrscript.GetStream();
        }

        public void Disconnect()
        {
            _streamReaderDashboard?.Dispose();
            _streamDashboard?.Dispose();
            _streamUrscript?.Dispose();

            _clientDashboard?.Close();
            _clientUrscript?.Close();

            _streamReaderDashboard = null;
            _streamDashboard = null;
            _streamUrscript = null;
            _clientDashboard = null;
            _clientUrscript = null;
        }


        public void SendDashboard(string command)
        {
            if (_streamDashboard == null)
                throw new InvalidOperationException("Robot is not connected.");

            _streamDashboard.Write(Encoding.ASCII.GetBytes(command));
        }

        public void SendUrscript(string program)
        {
            if (_streamUrscript == null)
                throw new InvalidOperationException("Robot is not connected.");

            _streamUrscript.Write(Encoding.ASCII.GetBytes(program));
        }

        public void SendUrscriptFile(string path)
        {
            var program = File.ReadAllText(path) + Environment.NewLine;
            SendUrscript(program);
        }

        public string ReadLineDashboard()
        {
            if (_streamReaderDashboard == null)
                throw new InvalidOperationException("Robot is not connected.");

            return _streamReaderDashboard.ReadLine();
        }
    }
}

namespace SystemLogin.RobotScripts
{
    public static class RobotScriptCatalog
    {
        public static string SorterColorPath =>
            Path.Combine(AppContext.BaseDirectory, "Robot", "Programs", "Sorter.cs");

        public static string LoadSorterColor() =>
            File.ReadAllText(SorterColorPath) + Environment.NewLine;
    }
}
