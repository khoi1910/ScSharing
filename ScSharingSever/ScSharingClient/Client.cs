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

        Button shareScreenButton;
        Thread imgReceiveThread;
        private Form screenShareForm; // Tham chiếu đến cửa sổ "Screen Share"

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

            // Nút Chia sẻ màn hình
            shareScreenButton = new Button
            {
                Text = "Chia sẻ màn hình",
                Location = new Point(230, 10),
                Size = new Size(150, 30),
                Enabled = false
            };
            shareScreenButton.Click += ShareScreenButton_Click;
            this.Controls.Add(shareScreenButton);

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

        private void ShareScreenButton_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                imgSendThread = new Thread(SendImages);
                imgSendThread.IsBackground = true;
                imgSendThread.Start();
                shareScreenButton.Enabled = false; // Vô hiệu hóa nút khi đang chia sẻ
            }
        }

        void Connect()
        {
            IP = new IPEndPoint(IPAddress.Parse("192.168.0.102"), 9999);
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
                // Bắt đầu nhận hình ảnh từ server
                imgReceiveThread = new Thread(ReceiveImages);
                imgReceiveThread.IsBackground = true;
                imgReceiveThread.Start();
                statusLabel.Text = "Status: Connected";
                shareScreenButton.Enabled = true; // Kích hoạt nút chia sẻ màn hình
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

        private void ReceiveImages()
        {
            while (isConnected)
            {
                try
                {
                    byte[] data = new byte[1024 * 5000];
                    int byteRead = client.Receive(data);

                    if (byteRead > 0)
                    {
                        using (MemoryStream ms = new MemoryStream(data, 0, byteRead))
                        {
                            Image img = Image.FromStream(ms);
                            // Hiển thị hình ảnh nhận được
                            this.Invoke((MethodInvoker)(() =>
                            {
                                // Kiểm tra xem cửa sổ "Screen Share" đã mở chưa
                                if (screenShareForm == null || screenShareForm.IsDisposed)
                                {
                                    // Tạo PictureBox mới để hiển thị hình ảnh
                                    PictureBox pictureBox = new PictureBox
                                    {
                                        SizeMode = PictureBoxSizeMode.Zoom,
                                        Dock = DockStyle.Fill,
                                        Image = img
                                    };
                                    screenShareForm = new Form
                                    {
                                        Text = "Screen Share",
                                        Size = new Size(800, 600)
                                    };
                                    screenShareForm.Controls.Add(pictureBox);
                                    screenShareForm.Show();
                                }
                                else
                                {
                                    // Nếu cửa sổ đã mở, chỉ cần cập nhật hình ảnh
                                    PictureBox pictureBox = (PictureBox)screenShareForm.Controls[0];
                                    pictureBox.Image = img;
                                }
                            }));
                        }
                    }
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