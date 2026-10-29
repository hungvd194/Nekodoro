# Đo hiệu năng Pomodoro Taskbar

## Kiểm tra Nekodoro sau đổi UI

Đổi nhận diện thêm bộ ảnh mèo tĩnh được nạp/cache một lần, không thêm animation loop. Harness đã chuyển sang `FocusWindow` mới. Mẫu 6 giây trên cùng máy: khi ở tray, thanh đứng yên có 0 lần đặt lại tọa độ/style, 6 lần kiểm tra; khi chạy có 12 lần kiểm tra và vẫn 0 lần đặt lại tọa độ/style không cần thiết. Working set quan sát khoảng 133–148 MiB. CPU mẫu ngắn lần lượt 12.5% một lõi khi cửa sổ tập trung mở, 0% ở tray-idle, 18.69% tray-running và 2.34% tray-paused; đây vẫn là số liệu có nhiễu, không phải cam kết dài hạn. Dữ liệu thô lần đo lưu cục bộ ở `.test-data/performance-nekodoro.json`.

Các kết quả dưới đây là đợt tối ưu trước khi đổi nhận diện, được giữ làm lịch sử so sánh.

Đo ngày 2026-10-29 bằng harness WPF trong `tests/PomodoroTaskbar.Windows.Tests/PerformanceProbe.cs`, cùng máy Windows, bản Release, dữ liệu tách biệt và GUID tray riêng. Mỗi trạng thái đo khoảng 6 giây sau giai đoạn khởi động. Các số liệu là quan sát ngắn, không phải cam kết trên mọi máy.

## Kết quả trước và sau

| Trạng thái | Render trước → sau | Đặt vị trí trước → sau | Đổi click-through trước → sau | Cấp phát bộ nhớ trước → sau (byte) | CPU trước → sau (% một lõi) |
|---|---:|---:|---:|---:|---:|
| settings-open-idle | 54 → 6 | 54 → 0 | 54 → 0 | 147536 → 7716360 | 19.01 → 28.33 |
| tray-idle | 54 → 6 | 54 → 0 | 54 → 0 | 82000 → 0 | 0.52 → 0.26 |
| tray-running | 55 → 11 | 55 → 0 | 55 → 0 | 409664 → 24592 | 52.47 → 29.73 |
| tray-paused | 55 → 5 | 55 → 0 | 55 → 0 | 82000 → 0 | 33.85 → 11.95 |

Khi ở tray và không chạy, cấp phát trong đoạn đo giảm từ khoảng 82 KB xuống 0 byte. Thanh đứng yên không còn gọi Win32 để đặt lại vị trí hay ghi style. Khi chạy, tiến độ chỉ cập nhật khi đổi pixel vật lý.

CPU và RAM có nhiễu do WPF, JIT, GC, compositor và tải máy; mẫu cửa sổ cài đặt mở còn cao hơn trước, nên chưa thể khẳng định giảm CPU trong mọi trạng thái. Working set đo được: trước 137–149 MiB, sau 133–148 MiB; không có bằng chứng giảm RAM lớn. Cấp phát của mẫu settings mở sau tối ưu có khởi tạo WPF/JIT muộn, không đại diện cho nền khi đóng cài đặt.

## Thay đổi

- Nhịp điều phối từ 100 ms sang 500 ms khi chạy và thanh hiện; 1 giây khi tạm dừng, chờ hoặc thanh ẩn. Kiểm tra toàn màn hình vẫn hoạt động.
- Cache HWND, màn hình và hình học; tính lại khi chỉnh cấu hình, DPI/màn hình thay đổi hoặc thoát chế độ kéo.
- Chỉ thay đổi tọa độ và style khi cần. Cache màu, giây hiển thị, trạng thái cài đặt/tray và độ dài tiến độ theo pixel.
- Bỏ cập nhật tiến độ/màu/chữ khi thanh ẩn hoặc độ hiển thị bằng 0; bỏ cập nhật cửa sổ cài đặt khi thu nhỏ.
- Giữ engine dựa trên đồng hồ đơn điệu và dùng đồng hồ đơn điệu cho lịch lưu phiên. Việc chuyển phiên được phát hiện trong tối đa khoảng một nhịp điều phối khi dispatcher không bận.

## Chạy lại

```powershell
dotnet run --project tests/PomodoroTaskbar.Windows.Tests -c Release -- --benchmark
```

Cần desktop Windows có Explorer hoạt động để kiểm chứng đăng ký tray. Muốn đánh giá CPU dài hạn hoặc rò rỉ tài nguyên cần chạy dài hơn, tránh mở nhiều bản ứng dụng phủ lên cùng cạnh màn hình.
