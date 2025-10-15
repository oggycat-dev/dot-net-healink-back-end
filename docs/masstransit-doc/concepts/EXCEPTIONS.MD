# Tổng quan về Xử lý lỗi (Exceptions) trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về cách MassTransit xử lý lỗi khi có một exception xảy ra bên trong **Consumer**, dựa trên documentation chính thức.

---

## 1. Khi Exception xảy ra

Khi một exception không được bắt (unhandled) bị ném ra từ phương thức `Consume` của bạn, MassTransit sẽ tự động bắt nó và kích hoạt một chuỗi các cơ chế xử lý lỗi được thiết kế để đảm bảo hệ thống phục hồi một cách an toàn.

Luồng xử lý lỗi mặc định gồm 2 bước chính: **Retry** và **Move to Error Queue**.

---

## 2. Retry: Cố gắng thử lại 🔁

Retry là tuyến phòng thủ đầu tiên. MassTransit sẽ tự động thử lại việc xử lý message với hy vọng rằng lỗi chỉ là tạm thời (transient), ví dụ như mất kết nối database tạm thời hoặc một service khác bị quá tải.

### Các chính sách Retry tích hợp sẵn:
* **Immediate:** Thử lại ngay lập tức.
* **Interval:** Thử lại sau một khoảng thời gian cố định.
* **Exponential:** Thử lại với khoảng thời gian tăng theo cấp số nhân (ví dụ: 2, 4, 8, 16 giây). Đây là chính sách rất hiệu quả.
* **Incremental:** Thử lại với khoảng thời gian tăng dần theo một hằng số (ví dụ: 2, 4, 6, 8 giây).

### Cách cấu hình Retry:
Bạn có thể cấu hình retry policy trên một endpoint hoặc trên một Consumer Definition.

```csharp
// Cấu hình retry policy trên một endpoint
cfg.ReceiveEndpoint("my-queue", e =>
{
    // Thử lại 5 lần, với khoảng thời gian tăng dần: 1s, 2s, 4s...
    e.UseMessageRetry(r => r.Exponential(5, 
        TimeSpan.FromSeconds(1), 
        TimeSpan.FromMinutes(2), 
        TimeSpan.FromSeconds(2))
    );
    
    e.ConfigureConsumer<MyConsumer>(context);
});
```
Bạn cũng có thể lọc để chỉ retry một số loại exception cụ thể.

---

## 3. Move to Error Queue: Di chuyển đến Hàng đợi lỗi ☣️

Nếu message vẫn thất bại sau khi đã thử lại hết số lần cho phép, MassTransit sẽ coi đó là một lỗi không thể phục hồi (poison message).

* **Hành động:** Message gốc sẽ được **di chuyển** đến một hàng đợi lỗi đặc biệt. Tên của hàng đợi này sẽ là tên của queue gốc cộng với hậu tố `_error`. Ví dụ: `my-queue_error`.
* **Mục đích:**
    * Ngăn chặn message lỗi làm tắc nghẽn queue chính.
    * Tách biệt các message lỗi để các nhà phát triển hoặc quản trị viên có thể kiểm tra và xử lý thủ công sau này (ví dụ: sửa dữ liệu rồi gửi lại).

---

## 4. `Fault<T>` Message: Sự kiện lỗi được Publish 📢

Đồng thời với việc di chuyển message đến queue lỗi, MassTransit sẽ **publish** một message sự kiện đặc biệt có kiểu là `Fault<TMessage>`, với `TMessage` là kiểu của message gốc gây ra lỗi.

* **Nội dung:** Message `Fault<T>` chứa thông tin chi tiết về lỗi, bao gồm:
    * Message gốc.
    * Thông tin Exception (stack trace, message).
    * Tên máy chủ, thời gian xảy ra lỗi.
* **Mục đích:** Cho phép các service khác (như service giám sát, dashboard) có thể **subscribe** vào các sự kiện lỗi này để thực hiện các hành động tự động như gửi cảnh báo cho DevOps, cập nhật trạng thái của một quy trình nghiệp vụ thành "Thất bại", v.v.

```csharp
// Một consumer có thể lắng nghe sự kiện lỗi của một message SubmitOrder
public class SubmitOrderFaultConsumer : IConsumer<Fault<SubmitOrder>>
{
    public Task Consume(ConsumeContext<Fault<SubmitOrder>> context)
    {
        // Gửi email hoặc tin nhắn Slack cho đội phát triển
        Console.WriteLine($"Lỗi khi xử lý đơn hàng: {context.Message.Message.OrderId}, lỗi: {context.Message.Exceptions.FirstOrDefault()?.Message}");
        return Task.CompletedTask;
    }
}
```

---

## 5. Redelivery và Idempotency (Giao lại và Tính bất biến)

Do cơ chế retry và các lỗi mạng, một message có thể được giao đến consumer **nhiều hơn một lần**. Do đó, consumer của bạn phải được thiết kế để có **tính bất biến (idempotent)**, nghĩa là việc xử lý cùng một message nhiều lần phải cho ra cùng một kết quả và không gây ra tác dụng phụ tiêu cực.

MassTransit cung cấp các header như `MessageId`, `ConversationId`, `CorrelationId` để giúp bạn xác định và xử lý các message trùng lặp.

---

## 6. The Outbox Pattern: Đảm bảo tính nhất quán

Để giải quyết vấn đề "dual-write" (ví dụ: lưu vào database thành công nhưng gửi message thất bại), MassTransit cung cấp **Outbox Pattern**.

* **Cách hoạt động:** Các message bạn muốn gửi/publish từ bên trong một consumer sẽ không được gửi đi ngay. Thay vào đó, chúng được lưu tạm vào một "hộp thư đi" (outbox) trong cùng một transaction với database của bạn.
* **Lợi ích:** Đảm bảo rằng các message chỉ được gửi đi **KHI VÀ CHỈ KHI** transaction của consumer (ví dụ: `_dbContext.SaveChangesAsync()`) thành công. Điều này đảm bảo tính nhất quán tuyệt đối giữa trạng thái database và các message được gửi ra ngoài.
* **Cách dùng:** Kích hoạt thông qua `UseInMemoryOutbox()` hoặc `UseEntityFrameworkOutbox()`.