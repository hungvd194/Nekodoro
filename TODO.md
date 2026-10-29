# TODO — Nekodoro

Danh sách triển khai theo [ARCHITECTURE.md](ARCHITECTURE.md). Thực hiện theo thứ tự các giai đoạn; đánh dấu `[x]` khi công việc đã hoàn thành và được kiểm chứng.

## Đổi nhận diện Nekodoro

- [x] Đổi tên hiển thị, EXE, tray và thông báo sang Nekodoro; giữ đường dữ liệu và định danh cũ.
- [x] Icon mèo đa kích thước, mascot sáu tư thế tĩnh, resource được cache.
- [x] Cửa sổ tập trung riêng với một nút chính theo trạng thái và dấu chân chu kỳ.
- [x] Cài đặt bốn tab, chọn cạnh bằng preview, slider + số chính xác, mã màu + picker.
- [x] Dấu chưa lưu, lỗi tại trường, Lưu giữ cửa sổ và Hủy bỏ đúng bản nháp.
- [x] Giữ tối ưu native geometry/style và nhịp cập nhật nền.
- [ ] Kiểm chứng dài hạn trên nhiều máy/DPI và các tình huống hệ thống trong phần nghiệm thu.

Tiến độ MVP: các chức năng chính đã triển khai; build Release và kiểm tra Core đã đạt. Đã thử Bắt đầu/Tạm dừng qua giao diện Windows. Các mục chưa đánh dấu gồm phần kiểm chứng tích hợp và cấu trúc tách lớp dự kiến; xem README về giới hạn hiện tại.

Rà soát ngày 29/10/2026: cập nhật các mục đã triển khai dựa trên mã nguồn và kết quả kiểm chứng đã ghi trong README; không chạy lại build hoặc kiểm thử trong lần rà soát này. Còn 17 mục chưa đánh dấu, gồm 3 mục sau MVP. Các mục nghiệm thu tổng hợp vẫn để mở nếu chưa kiểm chứng đủ mọi tình huống.

## 1. Khởi tạo dự án

- [x] Chọn phiên bản .NET LTS phù hợp và ghi yêu cầu môi trường phát triển.
- [x] Tạo solution với `PomodoroTaskbar.Core` và ứng dụng WPF `PomodoroTaskbar.Windows`.
- [x] Tạo project kiểm thử cho engine và các thành phần Windows cần kiểm chứng tự động (`tests/PomodoroTaskbar.Core.Tests`, `tests/PomodoroTaskbar.Windows.Tests`).
- [x] Tạo cấu trúc Models, Timer, Abstractions, Views, ViewModels, Services, Persistence, Interop và Assets.
- [x] Thiết lập cấu hình build, `.gitignore` và README hướng dẫn chạy.
- [x] Xác minh solution build và mở được ứng dụng mẫu trên Windows (kết quả build Release và thao tác giao diện đã ghi trong README).

## 2. Bộ đếm Pomodoro

- [x] Tạo loại phiên `Focus`, `ShortBreak`, `LongBreak` và trạng thái `Idle`, `Running`, `Paused`, `AwaitingNext`.
- [x] Tạo mô hình snapshot chứa thời lượng ban đầu, thời gian còn lại, tiến độ và số phiên hoàn tất.
- [x] Triển khai `IClock` với đồng hồ đơn điệu và đồng hồ giả cho kiểm thử (`MonotonicClock` trong `PomodoroEngine.cs`, `FakeClock` trong kiểm thử Core).
- [x] Triển khai bắt đầu, tạm dừng, tiếp tục, bỏ qua và đặt lại.
- [x] Tính thời gian theo thời lượng thực đã trôi qua; giới hạn tiến độ trong `[0, 1]`.
- [x] Thiết lập mặc định 25/5/15 phút và nghỉ dài sau mỗi 4 phiên làm việc hoàn tất.
- [x] Không tăng số phiên hoàn tất khi bỏ qua hoặc đặt lại; đặt lại giữ số phiên trong chu kỳ.
- [x] Thêm tự động chuyển phiên, mặc định tắt; thay đổi thời lượng chỉ áp dụng từ phiên sau.
- [x] Xử lý lệnh tuần tự và phát sự kiện hoàn tất đúng một lần.
- [x] Kiểm thử chu kỳ nghỉ dài, tạm dừng/tiếp tục, bỏ qua, đặt lại và nhịp cập nhật bị trễ.

## 3. Thanh thời gian

- [x] Tạo `OverlayWindow` không viền, không hiện trong taskbar và không lấy focus.
- [ ] Tạo `OverlayViewModel` nhận snapshot từ engine.
- [x] Hiển thị rãnh nền cố định và phần màu thu ngắn theo thời gian còn lại.
- [x] Hỗ trợ bốn cạnh: trên, dưới, trái và phải.
- [x] Cho chỉnh độ dài 10–100%, độ dày 2–20 DIP và khoảng cách mép 0–30 DIP.
- [x] Hỗ trợ căn đầu/giữa/cuối và ba hướng thu ngắn: từ đầu, từ cuối, về giữa.
- [x] Cho chỉnh độ trong suốt và bật/tắt chữ thời gian còn lại.
- [x] Thiết lập mặc định cạnh trên, căn giữa, dài 60%, dày 4 DIP.
- [x] Thêm chế độ luôn trên cửa sổ thông thường và nhấp xuyên qua mặc định.
- [x] Thêm tùy chọn Luôn hiển thị trên cùng (mặc định bật), lưu lựa chọn và khôi phục thứ tự khi taskbar che thanh mà không lấy focus; kiểm thử hồi quy bật/tắt và cửa sổ topmost che thanh (1.0.1).
- [x] Thêm chế độ chỉnh vị trí bằng kéo, căn vào cạnh; khôi phục nhấp xuyên qua khi kết thúc.
- [ ] Xác minh đủ bốn cạnh, ba hướng thu ngắn và các mức độ dài 10%, 60%, 100%.

## 4. Màu thanh thời gian

- [x] Tách cấu hình màu riêng cho làm việc, nghỉ ngắn và nghỉ dài.
- [x] Kiểm tra mã màu hợp lệ, lưu và khôi phục màu đã chọn.
- [x] Chuyển đổi cấu hình v1 sang v2, giữ màu và các tùy chọn đã lưu.
- [ ] Kiểm chứng chọn màu, xem trước và Hủy qua giao diện.

## 5. Cài đặt và lưu dữ liệu

- [x] Tạo mô hình cấu hình, giá trị mặc định và kiểm tra giới hạn đầu vào (`AppSettings`, `PomodoroSettings`, `OverlaySettings`, `ColorSettings` trong `Settings.cs`).
- [x] Xây dựng cửa sổ cài đặt cho Pomodoro, hình học thanh, màu sắc và hành vi ứng dụng.
- [x] Cho xem trước trực tiếp thay đổi vị trí, kích thước và màu sắc bằng bản nháp cấu hình.
- [x] Nút Lưu áp dụng và ghi cấu hình; Hủy hoặc đóng cửa sổ khôi phục phần xem trước.
- [x] Tạo `SettingsService`, lưu `settings.json` trong `%LocalAppData%\PomodoroTaskbar\`.
- [x] Thêm `schemaVersion`, chuyển đổi v1 sang v2 và ghi file qua file tạm.
- [x] Khi cấu hình hỏng, giữ bản lỗi và tải mặc định.
- [x] Tạo `SessionRepository`, lưu `session.json` sau lệnh, chuyển trạng thái, ngủ/thoát và định kỳ khoảng 10 giây.
- [x] Khôi phục phiên ở trạng thái tạm dừng sau khi mở lại ứng dụng.
- [x] Đưa đọc/ghi file ra khỏi luồng giao diện; thêm log có giới hạn dung lượng.
- [ ] Kiểm chứng lưu/đọc, file hỏng, migration, khôi phục phiên và thao tác Hủy.

## 6. Khay hệ thống và thông báo

- [x] Tạo `ApplicationController` kết nối engine, giao diện và dịch vụ.
- [x] Tạo biểu tượng khay và menu Bắt đầu, Tạm dừng, Tiếp tục, Bỏ qua, Đặt lại, Cài đặt, Thoát.
- [x] Cập nhật trạng thái bật/tắt của lệnh theo trạng thái engine.
- [x] Đóng cửa sổ cài đặt đưa ứng dụng về khay.
- [x] Thêm thông báo và âm báo khi phiên hoàn tất; cho bật/tắt riêng.
- [x] Không phát âm báo hoàn tất khi bỏ qua; lỗi thông báo không làm dừng bộ đếm.
- [x] Ngăn mở trùng tiến trình ứng dụng.
- [x] Khi thoát, lưu phiên, xóa biểu tượng khay và giải phóng đăng ký sự kiện.

## 7. Tích hợp Windows và nhiều màn hình

- [x] Tạo `DisplayService`, liệt kê màn hình và cho chọn màn hình hiển thị thanh.
- [x] Hỗ trợ DPI theo từng màn hình và tọa độ màn hình âm.
- [x] Tính lại vị trí khi thay đổi độ phân giải, DPI, hướng xoay hoặc bố trí màn hình.
- [x] Khi tháo màn hình đã chọn, dùng màn hình chính và giữ lựa chọn ưu tiên để khôi phục.
- [x] Thêm tùy chọn ẩn thanh khi ứng dụng khác toàn màn hình trên cùng màn hình.
- [x] Tạm dừng phiên khi máy ngủ; sau khi thức chờ người dùng tiếp tục.
- [x] Thêm bật/tắt khởi động cùng Windows, mặc định tắt.
- [ ] Kiểm chứng vị trí thanh khi trùng taskbar và khả năng chỉnh khoảng cách mép.

## 8. Nghiệm thu và phát hành MVP

- [x] Hồi quy lỗi Lưu/tray: Lưu giữ cửa sổ mở, đăng ký icon với Windows, đóng/mở lại cài đặt và giữ giá trị đã lưu (12 kiểm tra Windows).
- [x] Chạy các kiểm thử engine và lưu trữ; build bản Release thành công.
- [ ] Chạy thử trọn chu kỳ làm việc → nghỉ ngắn → nghỉ dài bằng thời lượng thử ngắn.
- [ ] Kiểm tra nhấp xuyên qua, focus, kéo chỉnh vị trí và tất cả tùy chọn thanh.
- [ ] Kiểm tra màu từng loại phiên ở bốn cạnh và thao tác xem trước/Hủy.
- [ ] Kiểm tra nhiều màn hình, DPI khác nhau, tháo/cắm màn hình và toàn màn hình.
- [ ] Kiểm tra ngủ/thức, thoát/mở lại và tình huống tiến trình bị kết thúc đột ngột.
- [x] Giảm nhịp cập nhật, cache hình học/style và đo CPU/bộ nhớ ngắn hạn trước/sau (PERFORMANCE.md).
- [ ] Đánh giá CPU và rò rỉ tài nguyên dài hạn, gồm trường hợp toàn màn hình và nhiều DPI.
- [ ] Kiểm tra không còn biểu tượng khay hoặc tài nguyên tồn đọng khi thoát.
- [x] Đóng gói bản portable self-contained win-x64 và ZIP; kiểm tra tiến trình khởi động.
- [x] Tạo bản Setup x64 kèm runtime, shortcut Start Menu, tùy chọn shortcut desktop và trình gỡ cài đặt; thêm script tạo lại bản cài. Đã thử cài/gỡ im lặng trong thư mục kiểm tra và đối chiếu hash DLL thành công ngày 29/10/2026.
- [ ] Xác minh bản portable trên máy khác không có SDK phát triển.
- [x] Bổ sung README: chạy ứng dụng, cấu hình, màu sắc, vị trí dữ liệu và giới hạn đã biết.

## 9. Sau MVP

- [ ] Lịch sử phiên và thống kê thời gian tập trung.
- [ ] Danh sách công việc gắn với phiên Pomodoro.
- [ ] Đồng bộ cấu hình nếu có nhu cầu.


