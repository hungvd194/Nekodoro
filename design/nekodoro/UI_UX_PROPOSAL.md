# Nekodoro — đề xuất đổi mới UI/UX

## Trạng thái

Thiết kế đã được phê duyệt và triển khai vào Nekodoro: cửa sổ tập trung riêng, cài đặt bốn tab, icon mèo và mascot sáu tư thế. Concept tham chiếu: [concept-v1.png](concept-v1.png). Các chi tiết hình vẽ, cỡ chữ và dữ liệu mẫu trong hình có thể khác bản triển khai native thực tế. Xem README và TODO về kiểm chứng còn lại.

## 1. Ý tưởng sản phẩm

**Nekodoro** kết hợp neko (mèo) và Pomodoro: một người bạn nhỏ giúp tập trung và nghỉ đúng lúc. Câu mô tả đề xuất: **“Một nhịp nhỏ, một ngày nhẹ hơn.”**

Phong cách tham chiếu: minh họa Nhật thân thiện theo hướng Irasutoya — hình phẳng, dáng hơi vụng về tự nhiên, màu dịu, nét không quá đều, biểu cảm đơn giản và chút chất liệu giấy. Nguồn tham chiếu: https://www.irasutoya.com/ . Mascot được thiết kế mới cho Nekodoro; không lấy hình minh họa sẵn làm logo.

Cảm giác đáng yêu đến từ nhân vật và lời nói, còn giao diện vẫn rõ, gọn và dễ thao tác. Không biến mọi nút thành một hình mèo hoặc thêm trang trí vào mọi hàng cài đặt.

## 2. Nhận diện và ngôn ngữ hình ảnh

| Thành phần | Đề xuất |
|---|---|
| Tên hiển thị | Nekodoro trên cửa sổ, tray, thông báo, tên file EXE và gói phát hành |
| Mascot | Mèo kem có mảng xám nâu, mắt chấm nhỏ, phụ kiện cà chua; cùng một nhân vật ở mọi trạng thái |
| App/tray icon | Đầu mèo với dấu cà chua, silhouette rõ ở 16–32 px; bản nhỏ giảm chi tiết |
| Nền | Giấy ngà `#F7F5EE`; bảng điều khiển trắng hoặc trắng ấm |
| Chữ | Nâu than `#403D37`; chữ phụ dùng nâu xám đủ tương phản |
| Nút chính | Đỏ cà chua trầm `#BF6559`; tinh chỉnh độ đậm nếu cần để chữ đạt tương phản |
| Nghỉ ngắn / nghỉ dài | Xanh sage `#7F9276` / xanh nhạt `#97B4BE` làm màu tiến độ, không dùng làm chữ nhỏ trên nền trắng |
| Font | Segoe UI cho phần điều khiển tiếng Việt, số tabular cho đồng hồ; chỉ dùng font tròn khác nếu đóng gói được và hiển thị dấu đầy đủ |
| Chất liệu | Grain rất nhẹ trong tranh; nền nút và vùng nhập giữ phẳng, sắc nét |
| Hình học | Cửa sổ theo khung native, panel bo 16–20 px, control bo 8–12 px; không glow/glass |

Ba màu thanh vẫn tùy chỉnh riêng theo loại phiên. Không đưa lựa chọn theme hoặc hiệu ứng Galaxy trở lại. Người dùng nâng cấp giữ màu hiện có; bảng màu mới chỉ là mặc định đề xuất cho cài mới.

## 3. Kiến trúc trải nghiệm

Tách **thực hiện phiên** khỏi **cấu hình ứng dụng**.

### A. Cửa sổ tập trung

- Kích thước mục tiêu khoảng 400–440 × 500–560 DIP, tự co theo vùng làm việc của màn hình.
- Tên Nekodoro, tranh mèo vừa phải, đồng hồ là thông tin nổi bật nhất.
- Hiện rõ loại phiên và trạng thái bằng chữ; không bắt người dùng suy ra từ màu hoặc biểu cảm.
- Một nút chính đổi theo trạng thái: Bắt đầu → Tạm dừng → Tiếp tục.
- Bỏ qua và Đặt lại là thao tác phụ; Cài đặt có vị trí cố định.
- Bốn dấu chân nhỏ chỉ tiến độ chu kỳ hiện tại, kèm chữ như “Phiên 2/4”; không gọi là thống kê hôm nay khi chưa có dữ liệu lịch sử.
- Sau khi hoàn tất phiên và chưa bật tự chuyển: hiện phiên kế tiếp cùng nút “Bắt đầu nghỉ” hoặc “Bắt đầu tập trung”.
- Trạng thái Idle không tự chạy ngay khi mở ứng dụng; khôi phục phiên cũ ở trạng thái tạm dừng.
- Đóng bằng X đưa ứng dụng về tray; không đặt xác nhận gây phiền ở mỗi lần đóng.

Trong ảnh concept, tranh chiếm diện tích lớn để thể hiện phong cách. Khi triển khai cần cân đối lại để đồng hồ và nút chính luôn hiện trên màn hình nhỏ.

### B. Cửa sổ cài đặt

Bốn mục, mỗi mục là một view riêng, không ghép tất cả vào một trang cuộn dài:

| Mục | Nội dung |
|---|---|
| Nhịp | Làm việc/nghỉ ngắn/nghỉ dài, nghỉ dài sau bao nhiêu phiên, tự chuyển phiên |
| Thanh | Chọn màn hình, cạnh, chiều dài, độ dày, căn vị trí, cách mép, hướng thu ngắn, độ hiển thị và chữ thời gian |
| Màu | Ba mẫu màu có nhãn, mã hex, bảng chọn màu và xem trước các loại phiên |
| Ứng dụng | Âm báo, thông báo, ẩn khi toàn màn hình, khởi động cùng Windows |

- Mục Thanh có một hình màn hình nhỏ: nhấp trực tiếp cạnh để chọn; trạng thái được chọn có viền và nhãn.
- Độ dài, độ dày, độ hiển thị dùng slider **kèm ô số chính xác** và đơn vị. Không chỉ có slider.
- Bản xem trước nội bộ phản ánh cấu hình; thanh ngoài desktop cũng xem trước trực tiếp.
- Màu chỉ có một nơi chỉnh chính thức là mục Màu. Hình concept đặt một hàng màu ở mục Thanh để minh họa tổng thể; khi triển khai thay bằng liên kết “Chỉnh màu thanh”.
- Footer Lưu/Hủy cố định, luôn thấy khi cuộn. Lưu giữ cửa sổ mở và hiện “Đã lưu thay đổi”.
- Sau khi đã Lưu, những sửa đổi tiếp theo là bản nháp mới; Hủy chỉ bỏ phần chưa lưu.
- Lỗi đầu vào nằm cạnh trường, có câu hướng dẫn; khi Lưu lỗi, tự mở đúng mục và focus vào trường đó.
- Có dấu “Chưa lưu” khi bản nháp khác cấu hình đã lưu; không bật dấu này khi chỉ chuyển mục.
- Các thay đổi thời lượng ghi rõ “Áp dụng từ phiên tiếp theo”, tránh gây hiểu nhầm về phiên đang chạy.

### C. Tray và thanh màn hình

- Nhấp trái icon mèo: mở hoặc đưa cửa sổ tập trung lên trước.
- Nhấp phải: Bắt đầu/Tạm dừng/Tiếp tục theo trạng thái, Bỏ qua, Mở Nekodoro, Cài đặt, Thoát.
- Tooltip có tên, loại phiên và thời gian còn lại. Không dùng màu làm thông tin duy nhất.
- Chạy EXE lần nữa mở cửa sổ của tiến trình đang chạy, không tạo bộ đếm thứ hai.
- Giữ kiểm tra đăng ký tray, khôi phục sau Explorer restart và đường lui cửa sổ khi đăng ký thất bại.
- Thanh ngoài màn hình vẫn mảnh và dùng màu đơn sắc. Mặc định không gắn mèo chuyển động trên thanh để tránh cản công việc.
- Nếu thêm đầu mèo ở đầu thanh về sau, đó là lựa chọn tắt mặc định và không được làm sai hình học tiến độ.

## 4. Mascot theo trạng thái

| Trạng thái | Tư thế | Lời ngắn đề xuất |
|---|---|---|
| Sẵn sàng | Ngồi cạnh đồng hồ cà chua | “Mình cùng tập trung nhé.” |
| Đang tập trung | Đọc sách hoặc ghi chép | “Cứ từng chút một thôi.” |
| Tạm dừng | Đặt sách xuống, nhìn lên | “Mình chờ bạn ở đây.” |
| Nghỉ ngắn | Ôm cốc trà | “Duỗi vai một chút nhé.” |
| Nghỉ dài | Cuộn tròn ngủ | “Nghỉ lâu hơn một chút nào.” |
| Hoàn tất | Giơ một chân chào | “Xong một phiên rồi!” |

Lời nhắn xuất hiện trong cửa sổ hoặc thông báo liên quan; không tự bật hội thoại khi đang làm việc. Không có trách móc khi bỏ qua, tạm dừng hoặc đặt lại.

## 5. Thông báo, chuyển động và khả năng tiếp cận

- Thông báo ghi tên Nekodoro, phiên vừa hoàn tất và hành động tiếp theo. Âm báo và thông báo giữ quyền bật/tắt riêng.
- Không giành focus khi hết giờ. Thao tác tiếp theo từ cửa sổ hoặc tray; thêm nút hành động trong thông báo chỉ khi cơ chế Windows đang dùng hỗ trợ ổn định.
- Mèo là bộ ảnh tĩnh đổi theo sự kiện. Có thể thử một chuyển động ngắn khi hoàn tất sau này; không có animation loop khi chạy nền.
- Giữ toàn bộ tối ưu hiện tại: nhịp 500 ms/1 giây, cache geometry/style, tiến độ theo pixel, không vẽ khi ẩn.
- Đảm bảo contrast chữ thường tối thiểu 4.5:1, focus bàn phím rõ ràng và thứ tự Tab nhất quán.
- Tất cả nút chỉ có hình phải có nhãn trợ năng/tooltip; màu trạng thái luôn có chữ đi kèm.
- Tranh minh họa không chặn hit-testing và không làm thay đổi layout khi đổi tư thế.
- Kiểm tra 100%, 125%, 150%, 200% DPI và vùng làm việc nhỏ; đồng hồ, nút chính và footer không bị cắt.

## 6. Phạm vi triển khai đề xuất

1. **Nhận diện:** chốt mascot, hoàn thiện sáu pose và icon đa kích thước; đổi tên hiển thị sang Nekodoro.
2. **Cấu trúc UX:** thêm cửa sổ tập trung riêng, điều hướng từ tray sang đúng cửa sổ, giữ bộ đếm hiện có.
3. **Cài đặt:** bốn mục, preview màn hình tương tác, slider + ô số, trạng thái chưa lưu và lỗi tại trường.
4. **Nhất quán:** lời nhắn, thông báo, palette, font, trạng thái nút và cách đóng cửa sổ.
5. **Kiểm chứng/phát hành:** hồi quy timer/tray/Lưu, nhiều DPI/màn hình, icon nhỏ, đo lại hiệu năng; xuất bản đầy đủ và Lite.

Khi đổi tên cần giữ cấu hình, phiên đang tạm dừng và khả năng tương thích dữ liệu cũ. Không chỉ thay chuỗi tên: phải kiểm tra tên EXE, icon, tooltip, thông báo, khởi động cùng Windows và cơ chế một tiến trình. Những định danh nội bộ phục vụ tương thích không cần lộ ra giao diện.

Không đưa lịch sử ngày, task list, achievement, pet nuôi dưỡng hoặc tài khoản vào đợt đổi UI này; chúng cần dữ liệu và thiết kế sản phẩm riêng.

## 7. Tiêu chí hoàn tất

- Chỉ nhìn icon/mascot và cửa sổ có thể nhận ra Nekodoro.
- Bắt đầu, tạm dừng và tiếp tục chỉ cần một thao tác từ cửa sổ tập trung hoặc menu tray.
- Điều chỉnh thanh không phải đoán giá trị; preview và thanh thật thống nhất.
- Lưu không đóng cửa sổ; Hủy bỏ đúng bản nháp; không mất đường quay lại từ tray.
- Các số liệu và dấu chân chu kỳ lấy từ engine, không thêm số minh họa vào bản chạy.
- Không mất màu/cấu hình/phiên khi nâng cấp từ Pomodoro Taskbar.
- Build và các kiểm tra hồi quy hiện có vẫn đạt; benchmark không phát sinh hoạt ảnh nền hoặc tăng liên tục handle/bộ nhớ.

## 8. Ghi chú concept

Hình được tạo bằng công cụ imagegen tích hợp, dùng để trình bày hướng thiết kế. Prompt lưu ở [CONCEPT_PROMPT.md](CONCEPT_PROMPT.md). Ảnh chưa phải sprite sheet hoặc asset đã cắt để tích hợp. Khi triển khai cần tạo riêng các pose và icon, tránh dùng ảnh chụp concept làm giao diện.
