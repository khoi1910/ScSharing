using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace ScSharingSever
{
    public partial class Sever : Form
    {
        private PictureBox pictureBox1;
        private Button connectButton;
        private Button disconnectButton;
        private Label statusLabel;

        private IPEndPoint IP;
        private Socket server;
        private Socket client;
        private bool isConnected = false;
        private const string CORRECT_PASSWORD = "123456"; // Mật khẩu được định nghĩa cố định
        private List<Socket> clients = new List<Socket>(); // Danh sách các client kết nối

        public Sever()
        {
            InitializeComponent();
            InitializeCustomComponents();
            this.WindowState = FormWindowState.Maximized;  // Phóng to cửa sổ khi mở
            this.Resize += Sever_Resize;  // Đăng ký sự kiện Resize
            this.Text = "Server";  // Đặt tên cửa sổ là "Server"
        }

        private void InitializeCustomComponents()
        {
            // Tạo PictureBox
            pictureBox1 = new PictureBox();
            pictureBox1.Size = new Size(800, 450);  // Đặt kích thước ban đầu cho PictureBox
            pictureBox1.Location = new Point(0, 50); // Đặt PictureBox cách đầu form 50px
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom; // Hình ảnh tự động điều chỉnh theo kích thước PictureBox
            this.Controls.Add(pictureBox1);

            // Tạo nút Kết nối
            connectButton = new Button();
            connectButton.Text = "Kết nối";
            connectButton.Size = new Size(100, 40);
            connectButton.Location = new Point(10, 10);
            connectButton.Click += ConnectButton_Click;
            this.Controls.Add(connectButton);

            // Tạo nút Ngắt kết nối
            disconnectButton = new Button();
            disconnectButton.Text = "Ngắt kết nối";
            disconnectButton.Size = new Size(100, 40);
            disconnectButton.Location = new Point(120, 10);
            disconnectButton.Click += DisconnectButton_Click;
            this.Controls.Add(disconnectButton);

            // Tạo Label để hiển thị trạng thái kết nối
            statusLabel = new Label();
            statusLabel.Text = "Trạng thái: Chưa kết nối";
            statusLabel.Size = new Size(200, 40);
            statusLabel.Location = new Point(230, 10);
            this.Controls.Add(statusLabel);
        }

        private void Sever_Resize(object sender, EventArgs e)
        {
            // Cập nhật kích thước của PictureBox khi cửa sổ thay đổi kích thước
            pictureBox1.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 50);
        }

        private void ConnectButton_Click(object sender, EventArgs e)
        {
            if (!isConnected)
            {
                Connect();
                statusLabel.Text = "Trạng thái: Đang kết nối...";
                connectButton.Enabled = false;
                disconnectButton.Enabled = true;
            }
        }

        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                Disconnect();
                statusLabel.Text = "Trạng thái: Đã ngắt kết nối";
                connectButton.Enabled = true;
                disconnectButton.Enabled = false;
            }
        }

        private void Connect()
        {
            IP = new IPEndPoint(IPAddress.Any, 9999);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(IP);

            Thread listenThread = new Thread(ListenFromClient);
            listenThread.IsBackground = true;
            listenThread.Start();
        }

        private void Disconnect()
        {
            if (client != null && client.Connected)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }

            if (server != null)
            {
                server.Close();
            }

            isConnected = false;
        }

        private void ListenFromClient()
        {
            try
            {
                server.Listen(10); // Cho phép nhiều client kết nối
                while (true)
                {
                    Socket clientSocket = server.Accept();
                    clients.Add(clientSocket); // Thêm client vào danh sách

                    isConnected = true;
                    this.Invoke((MethodInvoker)(() => statusLabel.Text = "Trạng thái: Đang xác thực..."));

                    Thread receiveThread = new Thread(ReceiveDataFromClient);
                    receiveThread.IsBackground = true;
                    receiveThread.Start(clientSocket);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in listening: " + ex.Message);
            }
        }

        private void ReceiveDataFromClient(object obj)
        {
            Socket clientSocket = obj as Socket;

            try
            {
                // Đầu tiên, nhận mật khẩu từ client
                byte[] passwordBuffer = new byte[1024];
                int passwordBytes = clientSocket.Receive(passwordBuffer);
                string receivedPassword = System.Text.Encoding.UTF8.GetString(passwordBuffer, 0, passwordBytes);

                if (receivedPassword != CORRECT_PASSWORD)
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        statusLabel.Text = "Trạng thái: Mật khẩu không đúng";
                        MessageBox.Show("Mật khẩu không đúng!");
                    }));
                    clientSocket.Close();
                    isConnected = false;
                    return;
                }

                this.Invoke((MethodInvoker)(() => statusLabel.Text = "Trạng thái: Xác thực thành công"));

                // Tiếp tục nhận dữ liệu màn hình
                while (isConnected)
                {
                    try
                    {
                        byte[] data = new byte[1024 * 5000];
                        int byteRead = clientSocket.Receive(data);

                        if (byteRead > 0)
                        {
                            // Gửi lại dữ liệu cho tất cả các client khác
                            foreach (var c in clients)
                            {
                                if (c != clientSocket) // Không gửi lại cho client đang chia sẻ
                                {
                                    c.Send(data, byteRead, SocketFlags.None);
                                }
                            }

                            using (MemoryStream ms = new MemoryStream(data, 0, byteRead))
                            {
                                Image img = Image.FromStream(ms);
                                pictureBox1.Invoke((MethodInvoker)(() => pictureBox1.Image = img));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        isConnected = false;
                        // Xử lý lỗi như trước
                        this.Invoke((MethodInvoker)(() =>
                        {
                            statusLabel.Text = "Trạng thái: Lỗi kết nối";
                            MessageBox.Show("Error receiving data: " + ex.Message);
                        }));
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                isConnected = false;
                this.Invoke((MethodInvoker)(() =>
                {
                    statusLabel.Text = "Trạng thái: Lỗi xác thực";
                    MessageBox.Show("Error in authentication: " + ex.Message);
                }));
            }
        }
    }
}