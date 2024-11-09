using System;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;

namespace ScSharingClient
{
    public partial class Client : Form
    {
        Button connectButton;
        Button disconnectButton;
        Label statusLabel;

        IPEndPoint IP;
        Socket client;
        bool isConnected = false;
        Thread imgSendThread;

        public Client()
        {
            InitializeComponent();
            InitializeControls();
            this.WindowState = FormWindowState.Maximized;  // Thêm dòng này để phóng to cửa sổ
            this.Text = "Client";  // Đặt tên cửa sổ là "Client"
        }

        void InitializeControls()
        {
            // Nút Connect
            connectButton = new Button
            {
                Text = "Connect",
                Location = new Point(10, 10),
                Size = new Size(100, 30)
            };
            connectButton.Click += ConnectButton_Click;
            this.Controls.Add(connectButton);

            // Nút Disconnect
            disconnectButton = new Button
            {
                Text = "Disconnect",
                Location = new Point(120, 10),
                Size = new Size(100, 30),
                Enabled = false
            };
            disconnectButton.Click += DisconnectButton_Click;
            this.Controls.Add(disconnectButton);

            // Label trạng thái
            statusLabel = new Label
            {
                Text = "Status: Disconnected",
                Location = new Point(10, 50),
                AutoSize = true
            };
            this.Controls.Add(statusLabel);
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            Connect();
            connectButton.Enabled = false;
            disconnectButton.Enabled = true;
            statusLabel.Text = "Status: Connected";
        }

        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            Disconnect();
            connectButton.Enabled = true;
            disconnectButton.Enabled = false;
            statusLabel.Text = "Status: Disconnected";
        }

        void Connect()
        {
            IP = new IPEndPoint(IPAddress.Parse("192.168.1.241"), 9999);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                client.Connect(IP);
                isConnected = true;
                imgSendThread = new Thread(SendImages);
                imgSendThread.IsBackground = true;
                imgSendThread.Start();
            }
            catch
            {
                statusLabel.Text = "Status: Failed to Connect";
                isConnected = false;
            }
        }

        void Disconnect()
        {
            if (isConnected)
            {
                client.Close();
                isConnected = false;
                if (imgSendThread != null && imgSendThread.IsAlive)
                    imgSendThread.Abort();
            }
        }

        public static byte[] CaptureScreen()
        {
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    return ms.ToArray();
                }
            }
        }

        void SendImages()
        {
            while (isConnected)
            {
                try
                {
                    byte[] screenData = CaptureScreen();
                    client.Send(screenData);
                    Thread.Sleep(100);
                }
                catch
                {
                    isConnected = false;
                    statusLabel.Invoke((MethodInvoker)(() => statusLabel.Text = "Status: Connection Lost"));
                    break;
                }
            }
        }
    }
}
