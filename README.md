# Nekodoro

**Một nhịp nhỏ, một ngày nhẹ hơn.** Ứng dụng Pomodoro cho Windows với mèo minh họa, thanh thời gian ở cạnh màn hình và điều khiển qua khay hệ thống.

![Mascot Nekodoro](src/PomodoroTaskbar.Windows/Assets/App.png)

## Chạy ứng dụng

- `artifacts/Nekodoro-Setup-1.0.1-win-x64.exe`: bản cài kèm runtime, cài cho tài khoản hiện tại, tạo shortcut Start Menu và hỗ trợ gỡ trong Windows Settings. Shortcut desktop là tùy chọn. Cấu hình và phiên cũ được giữ khi gỡ cài đặt.
- `artifacts/Nekodoro-win-x64.zip`: bản đầy đủ, kèm runtime. Giải nén toàn bộ rồi chạy **Nekodoro.exe**.
- `artifacts/Nekodoro-win-x64-lite.zip`: bản nhẹ, cần **.NET 10 Desktop Runtime x64**.

Không tách EXE khỏi các file đi kèm. Nếu bản Pomodoro Taskbar cũ đang chạy, chọn Thoát từ tray trước khi chạy Nekodoro.

## Sử dụng

- Cửa sổ tập trung có đồng hồ lớn và nút Bắt đầu/Tạm dừng/Tiếp tục. Mèo đổi tư thế theo phiên; dấu chân thể hiện chu kỳ đã hoàn tất.
- Cài đặt có bốn mục: **Nhịp**, **Thanh**, **Màu**, **Ứng dụng**. Chọn cạnh trực tiếp trên hình màn hình, kéo slider hoặc nhập số chính xác.
- Mỗi loại phiên dùng một màu tùy chỉnh. Không có theme động.
- Trong tab **Thanh**, **Luôn hiển thị trên cùng** mặc định bật; thanh tự khôi phục thứ tự hiển thị khi bị taskbar hoặc cửa sổ trên cùng che, ở nhịp cập nhật 0,5–1 giây, không lấy focus. Tắt tùy chọn để thanh theo thứ tự cửa sổ thông thường. Tùy chọn ẩn khi toàn màn hình vẫn được ưu tiên.
- Lưu giữ cửa sổ mở và xóa dấu “Chưa lưu”. Hủy hoặc đóng cửa sổ chỉ bỏ thay đổi chưa lưu sau lần Lưu gần nhất.
- Đóng cửa sổ bằng X để về tray. Nhấp icon mèo để mở cửa sổ tập trung; nhấp phải để điều khiển phiên, mở cài đặt hoặc thoát.
- Mở EXE lần nữa gọi cửa sổ của tiến trình hiện có. Nếu Windows gộp icon, tìm trong mũi tên `^` cạnh đồng hồ.
- Nếu Windows từ chối đăng ký tray, ứng dụng giữ cửa sổ mở để người dùng không mất đường điều khiển.

## Nâng cấp và dữ liệu

Nekodoro tiếp tục dùng `%LocalAppData%\PomodoroTaskbar\` để giữ cấu hình, màu và phiên cũ. Đây là đường dẫn tương thích, không phải lỗi đổi tên. Cấu hình hiện tại là schema v2; v1 tự chuyển ba màu đã lưu. Dữ liệu sai được sao lưu rồi tải mặc định.

Phiên đang chạy được khôi phục ở trạng thái tạm dừng. Thay đổi thời lượng áp dụng khi bắt đầu hoặc đặt lại phiên, không thay đổi phiên đang chạy. Màu mặc định giấy ngà/cà chua/sage chỉ áp dụng khi chưa có màu đã lưu.

Định danh một tiến trình, tray và khóa khởi động Windows cũ được giữ để tránh chạy song song hoặc nhân đôi mục khởi động. Khi đã bật khởi động cùng Windows, ứng dụng cập nhật đường dẫn EXE mới nếu cần.

## Phát triển và kiểm tra

Yêu cầu Windows, .NET 10 SDK và Windows Desktop targeting pack. Thư mục/namespace mã nguồn giữ tên cũ để tránh đổi đường dẫn không cần thiết; assembly phát hành là Nekodoro.

Thư mục gốc dự án là `Nekodoro`. Chạy các lệnh bên dưới từ thư mục này; đường dẫn dự án con vẫn dùng tên `PomodoroTaskbar` để tương thích mã nguồn.

```powershell
dotnet build PomodoroTaskbar.slnx -c Release -m:1
dotnet run --project tests/PomodoroTaskbar.Core.Tests -c Release
dotnet run --project tests/PomodoroTaskbar.Windows.Tests -c Release
dotnet run --project src/PomodoroTaskbar.Windows -- --data-dir .test-data/manual
```

Kiểm tra Windows cần desktop có Explorer để đăng ký tray.

Bản đổi UI đã vượt qua **26 kiểm tra Core + 30 kiểm tra Windows**, build Release không lỗi/cảnh báo. Đã quan sát giao diện native, đổi tư thế mèo khi chạy/tạm dừng và đóng cửa sổ về tray/mở lại EXE. Bản đầy đủ khoảng 97 MB ZIP; Lite khoảng 2.4 MB ZIP (chủ yếu tăng do bộ hình mèo).

Đo hiệu năng:

```powershell
dotnet run --project tests/PomodoroTaskbar.Windows.Tests -c Release -- --benchmark
```

Đóng gói:

```powershell
dotnet publish src/PomodoroTaskbar.Windows -c Release -r win-x64 --self-contained true -o artifacts/nekodoro-portable
dotnet publish src/PomodoroTaskbar.Windows -c Release -r win-x64 --self-contained false -o artifacts/nekodoro-lite
```

Tạo bản cài bằng [Inno Setup](https://jrsoftware.org/isdl.php):

```powershell
./scripts/Build-Installer.ps1 -Version 1.0.1
# Nếu compiler không ở vị trí mặc định, thêm -IsccPath 'C:\path\ISCC.exe'.
# Đóng gói lại bản self-contained có sẵn mà không publish: thêm -SkipPublish.
```

Bản cài chưa ký số. Thoát Nekodoro từ tray trước khi cập nhật hoặc gỡ cài đặt. Bản cài không tự bật khởi động cùng Windows; tùy chọn này nằm trong cài đặt ứng dụng.

Bản Setup 1.0.0 ngày 29/10/2026 đóng gói từ bản portable đã có cùng ngày. Lần tạo Setup này không build lại mã nguồn do kết nối NuGet không hoàn tất.

Bản 1.0.1 bổ sung tùy chọn luôn trên cùng và sửa thứ tự hiển thị khi taskbar che thanh. Đã build Release từ mã nguồn, vượt qua 28 kiểm tra Core và 34 kiểm tra Windows, gồm mô phỏng cửa sổ topmost che thanh, kiểm tra giữ focus và bật/tắt tùy chọn.

Đã thử cài im lặng vào thư mục kiểm tra, đối chiếu hash DLL với bản portable và gỡ cài đặt thành công (exit code 0); file ứng dụng đã được xóa sau khi gỡ. Chưa kiểm chứng bản cài trên máy khác.

## Hình ảnh và hiệu năng

Mascot sáu tư thế được tạo bằng imagegen tích hợp từ concept đã duyệt; nạp/cắt/cache một lần trong `MascotArt`, không chạy animation loop nền. Icon mèo đa kích thước được dựng bằng `scripts/Generate-Icon.ps1`. Xem [nguồn asset và prompt](design/nekodoro/ASSETS.md).

Giữ nhịp cập nhật 500 ms khi chạy và thanh hiện, 1 giây ở trạng thái khác; cache vị trí/style và tiến độ theo pixel. Xem [PERFORMANCE.md](PERFORMANCE.md) về đo thực tế và giới hạn. Chưa kiểm chứng đầy đủ máy khác, mọi cấu hình nhiều DPI/màn hình, Explorer restart và đăng xuất/ngủ dài.

## Cấu trúc dự án

```text
Nekodoro/
├── PomodoroTaskbar.slnx
├── src/
│   ├── PomodoroTaskbar.Core/           # Engine, cấu hình, lưu dữ liệu
│   └── PomodoroTaskbar.Windows/        # WPF, tray, icon và mascot
├── tests/                             # Kiểm tra Core và Windows
├── scripts/                           # Công cụ tạo icon
├── design/nekodoro/                    # Concept, đặc tả và prompt hình ảnh
├── artifacts/                         # Gói phát hành (không đưa vào Git)
├── .gitignore
└── README.md
```

`.gitignore` loại trừ build output, gói phát hành, dữ liệu kiểm thử, cache SDK/IDE và cấu hình bí mật cục bộ. Mã nguồn, ảnh mascot, icon và tài liệu thiết kế được giữ trong repository.

[Kiến trúc](ARCHITECTURE.md) · [TODO](TODO.md) · [Thiết kế đã duyệt](design/nekodoro/UI_UX_PROPOSAL.md)
