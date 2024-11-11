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
            this.WindowState = FormWindowState.Maximized;
            this.Text = "Client";
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
            if (isConnected)
            {
                connectButton.Enabled = false;
                disconnectButton.Enabled = true;
                statusLabel.Text = "Status: Connected";
            }
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
            IP = new IPEndPoint(IPAddress.Parse("192.168.1.34"), 9999);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                client.Connect(IP);

                // Hiện form nhập mật khẩu
                string password = ShowPasswordDialog();
                if (string.IsNullOrEmpty(password))
                {
                    Disconnect();
                    statusLabel.Text = "Status: Connection Cancelled";
                    return;
                }

                // Gửi mật khẩu đến server
                byte[] passwordData = System.Text.Encoding.UTF8.GetBytes(password);
                client.Send(passwordData);

                // Đợi 1 chút để server xử lý mật khẩu
                Thread.Sleep(500);

                // Kiểm tra xem kết nối còn tồn tại không
                if (!client.Connected)
                {
                    statusLabel.Text = "Status: Wrong Password";
                    return;
                }

                isConnected = true;
                imgSendThread = new Thread(SendImages);
                imgSendThread.IsBackground = true;
                imgSendThread.Start();
                statusLabel.Text = "Status: Connected";
            }
            catch
            {
                statusLabel.Text = "Status: Failed to Connect";
                isConnected = false;
            }
        }

        private string ShowPasswordDialog()
        {
            Form prompt = new Form()
            {
                Width = 300,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Enter Password",
                StartPosition = FormStartPosition.CenterScreen
            };

            TextBox textBox = new TextBox()
            {
                Left = 50,
                Top = 20,
                Width = 200,
                PasswordChar = '*'
            };

            Button confirmation = new Button()
            {
                Text = "OK",
                Left = 50,
                Width = 100,
                Top = 50,
                DialogResult = DialogResult.OK
            };

            Button cancel = new Button()
            {
                Text = "Cancel",
                Left = 150,
                Width = 100,
                Top = 50,
                DialogResult = DialogResult.Cancel
            };

            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);

            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
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