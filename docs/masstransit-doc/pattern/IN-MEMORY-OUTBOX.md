# Tổng quan về In-Memory Outbox trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **In-Memory Outbox**, một middleware trong MassTransit giúp tăng độ tin cậy khi gửi message từ bên trong một consumer, dựa trên documentation chính thức.

---

## 1. In-Memory Outbox là gì? 📬

**In-Memory Outbox** là một middleware hoạt động như một "hộp thư đi" tạm thời được lưu trữ **trong bộ nhớ**.

Nó giải quyết vấn đề sau: Điều gì sẽ xảy ra nếu consumer của bạn gửi đi một message rồi sau đó lại gặp lỗi và ném ra exception? Message đã được gửi đi, nhưng việc xử lý message gốc lại thất bại, dẫn đến trạng thái không nhất quán.

In-Memory Outbox đảm bảo rằng tất cả các message được gửi hoặc publish từ bên trong một consumer sẽ chỉ được chuyển đến broker **KHI VÀ CHỈ KHI** phương thức `Consume` của consumer đó hoàn thành thành công (không ném ra exception).



---

## 2. Luồng hoạt động

1.  Khi một consumer có `UseInMemoryOutbox` được kích hoạt, outbox sẽ bắt đầu "ghi nhận".
2.  Bên trong phương thức `Consume`, mỗi khi bạn gọi `context.Publish(...)` hoặc `context.Send(...)`, message sẽ **không được gửi đi ngay**. Thay vào đó, nó được thêm vào một danh sách chờ trong bộ nhớ.
3.  Phương thức `Consume` tiếp tục thực thi.
4.  **Nếu `Consume` hoàn tất thành công:** Outbox sẽ "giải phóng" và gửi tất cả các message trong danh sách chờ đến broker.
5.  **Nếu `Consume` ném ra một exception:** Tất cả các message trong danh sách chờ sẽ bị **hủy bỏ** và không bao giờ được gửi đi. Message gốc sẽ được đưa vào cơ chế retry hoặc chuyển đến queue lỗi.

---

## 3. So sánh In-Memory Outbox và Transactional Outbox

| Tiêu chí | In-Memory Outbox (`UseInMemoryOutbox`) | Transactional Outbox (`UseEntityFrameworkOutbox`) |
| :--- | :--- | :--- |
| **Nơi lưu trữ** | Trong bộ nhớ (RAM) của ứng dụng | Trong database (dưới dạng các bảng) |
| **Vấn đề giải quyết** | Đảm bảo consumer hoàn thành mới gửi message | Đảm bảo cả consumer hoàn thành **VÀ** database commit thành công mới gửi message (giải quyết "dual-write") |
| **Độ bền vững** | Thấp hơn. Message có thể mất nếu ứng dụng sập đúng thời điểm. | **Cao nhất.** Message được lưu an toàn trong database. |
| **Hiệu năng** | Rất cao | Thấp hơn một chút do phải ghi vào database |

**Kết luận:** **Transactional Outbox** an toàn và đáng tin cậy hơn, là lựa chọn được khuyến nghị cho các nghiệp vụ quan trọng. **In-Memory Outbox** là một giải pháp nhẹ nhàng hơn, hữu ích khi bạn không dùng database hoặc khi nghiệp vụ không yêu cầu độ bền vững tuyệt đối.

---

## 4. Cách cấu hình

`UseInMemoryOutbox` là một middleware và được cấu hình trên receive endpoint.

```csharp
cfg.ReceiveEndpoint("my-queue", e =>
{
    // Thêm middleware In-Memory Outbox vào pipeline
    // Nó nên được đặt trước các middleware khác như Retry
    e.UseInMemoryOutbox();

    e.ConfigureConsumer<MyConsumer>(context);
});
```
* **Lưu ý:** `UseInMemoryOutbox` cần được truyền `context` (`IRegistrationContext`) nếu nó được dùng cùng với các middleware khác cần đến DI scope, ví dụ như Scoped Filters.

---

## 5. Khi nào nên dùng?

* **Khi dùng Saga với Pessimistic Locking:** Đây là trường hợp sử dụng rất phổ biến. Nó đảm bảo các message do saga gửi ra chỉ được publish sau khi saga đã cập nhật xong trạng thái và giải phóng lock trên database, tránh được các vấn đề về deadlock.
* **Tăng độ tin cậy cho consumer không dùng database:** Khi consumer của bạn thực hiện nhiều bước và có thể thất bại ở giữa chừng, in-memory outbox đảm bảo không có message nào bị gửi đi một cách vô ích.
* **Các kịch bản không yêu cầu độ bền vững tuyệt đối:** Khi việc mất một vài message trong trường hợp ứng dụng bị sập là có thể chấp nhận được.