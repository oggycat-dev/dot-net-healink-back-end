# Tổng quan về Saga Persistence trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **Saga Persistence**, tức là cách MassTransit lưu trữ và truy xuất trạng thái của Saga, dựa trên documentation chính thức.

---

## 1. Tại sao Persistence lại quan trọng?

Saga là các quy trình nghiệp vụ có thể kéo dài (long-running). Một service có thể bị khởi động lại hoặc gặp sự cố bất cứ lúc nào. **Persistence** đảm bảo rằng trạng thái hiện tại của tất cả các saga đang hoạt động được lưu trữ an toàn vào một nơi bền vững (như database), cho phép chúng tiếp tục hoạt động bình thường sau khi service khởi động lại.

Nếu không có persistence, tất cả các saga đang chạy sẽ bị mất khi ứng dụng tắt.

---

## 2. Saga Repository: Cầu nối đến Database

MassTransit sử dụng một interface trừu tượng là `ISagaRepository<TState>` để tương tác với lớp lưu trữ. Điều này cho phép MassTransit hỗ trợ nhiều loại database khác nhau một cách linh hoạt.

Nhiệm vụ của repository là:
* **Tìm** một saga instance dựa trên correlation.
* **Thêm** một instance mới.
* **Cập nhật** một instance đã có.
* **Xóa** một instance đã hoàn thành.

---

## 3. Các loại Repository được hỗ trợ

MassTransit cung cấp nhiều gói NuGet để hỗ trợ các loại database phổ biến.

### A. In-Memory
* **Mục đích:** Dùng cho **testing** và phát triển.
* **Đặc điểm:** Dữ liệu được lưu trong RAM và sẽ mất khi ứng dụng tắt. Rất nhanh nhưng không bền vững.
* **Cấu hình:** `.InMemoryRepository()`

### B. Entity Framework Core 💾
* **Mục đích:** **Lựa chọn được khuyến nghị** cho các cơ sở dữ liệu quan hệ như **SQL Server**, **PostgreSQL**.
* **Đặc điểm:** Mạnh mẽ, linh hoạt và tích hợp tốt với `DbContext` của ứng dụng. Hỗ trợ đầy đủ các cơ chế xử lý đồng thời (concurrency).
* **Cấu hình:** `.EntityFrameworkRepository(...)`

### C. MongoDB
* **Mục đích:** Dùng cho cơ sở dữ liệu tài liệu (document database) MongoDB.
* **Đặc điểm:** Phù hợp tự nhiên với việc lưu trữ trạng thái của saga dưới dạng một document.
* **Cấu hình:** `.MongoDbRepository(...)`

### D. Redis
* **Mục đích:** Dùng Redis làm nơi lưu trữ.
* **Đặc điểm:** Tốc độ truy xuất cực nhanh do Redis là in-memory database.