
Ứng dụng Chia sẻ Màn hình
==========================
ScSharing là một ứng dụng chia sẻ màn hình đơn giản, hoạt động trong cùng mạng nội bộ. Ứng dụng cho phép server phát màn hình đến các client kết nối và hỗ trợ client chia sẻ màn hình ngược lại đến server.

Tính năng
=========
Client
------
- **Kết nối đến Server**: Thiết lập kết nối với server thông qua mật khẩu xác thực.
- **Chia sẻ Màn hình**: Gửi dữ liệu màn hình đến server theo thời gian thực.
- **Nhận Màn hình**: Xem màn hình được server phát sóng.

Server
------
- **Chấp nhận Kết nối**: Xử lý nhiều kết nối từ các client cùng lúc.
- **Xác thực Client**: Kiểm tra mật khẩu của client trước khi cấp quyền truy cập.
- **Phát sóng Màn hình**: Chia sẻ màn hình của server cho tất cả các client.
- **Hiển thị Màn hình Client**: Xem màn hình được chia sẻ bởi các client.

Yêu cầu
=======
- **Môi trường phát triển**:
  - .NET Framework
  - Visual Studio
- **Hệ điều hành**: Windows
- **Kết nối mạng**: Server và client phải ở cùng một mạng nội bộ.

Cài đặt
=======
1. Clone hoặc tải dự án về máy:
   ```bash
   git clone https://github.com/your-repo-url/ScSharing.git
   ```
2. Mở dự án trong Visual Studio.
3. Build giải pháp để khôi phục các gói NuGet và phụ thuộc.

Hướng dẫn sử dụng
=================
Server
------
1. Chạy ứng dụng **ScSharingSever**.
2. Nhấn nút **Kết nối** để bắt đầu lắng nghe kết nối từ các client.
3. Sử dụng giao diện để xem màn hình được chia sẻ.

Client
------
1. Chạy ứng dụng **ScSharingClient**.
2. Nhấn nút **Connect** và nhập địa chỉ IP cùng mật khẩu của server.
3. Chia sẻ màn hình qua nút **Chia sẻ màn hình** hoặc xem màn hình từ server.

Cấu hình
========
- **Địa chỉ IP của Server**: Chỉnh sửa địa chỉ IP của server trong file `Client.cs`:
  ```csharp
  IP = new IPEndPoint(IPAddress.Parse("ĐỊA_CHỈ_IP_SERVER"), 9999);
  ```
- **Mật khẩu**: Thay đổi mật khẩu mặc định trong file `Sever.cs`:
  ```csharp
  private const string CORRECT_PASSWORD = "mật_khẩu_của_bạn";
  ```

Hạn chế
=======
- Không hỗ trợ kết nối khác mạng (cần VPN hoặc port forwarding để sử dụng từ xa).
- Chưa hỗ trợ chia sẻ âm thanh.
- Chỉ chia sẻ màn hình, không hỗ trợ truyền file hoặc điều khiển từ xa.

Dự định phát triển
==================
- Thêm tính năng truyền âm thanh.
- Hỗ trợ kết nối qua mạng khác.
- Tối ưu hiệu suất để giảm độ trễ và nâng cao chất lượng.

Giấy phép
=========
Dự án này được cấp phép theo giấy phép MIT. Xem chi tiết trong file `LICENSE`.
