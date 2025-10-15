# Tổng quan về Consumers trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **Consumer** trong MassTransit, dựa trên documentation chính thức. Consumer là thành phần trung tâm, chịu trách nhiệm **tiếp nhận và xử lý message** từ một hàng đợi (queue).

---

## 1. Consumer là gì?

Consumer là một class C# thực thi interface `IConsumer<TMessage>`, với `TMessage` là loại message mà nó được thiết kế để xử lý. Mỗi consumer có một phương thức duy nhất là `Consume`, được MassTransit tự động gọi khi có message phù hợp được nhận.

```csharp
// Ví dụ về một Consumer
public class SubmitOrderConsumer : IConsumer<SubmitOrder>
{
    public async Task Consume(ConsumeContext<Submit-Order> context)
    {
        // Logic xử lý message được đặt ở đây
        var orderId = context.Message.OrderId;
        await Console.Out.WriteLineAsync($"Đã nhận lệnh đặt hàng: {orderId}");
    }
}
```

---

## 2. ConsumeContext: "Hộp dụng cụ" khi xử lý Message

Tham số `ConsumeContext<TMessage>` không chỉ chứa message (`context.Message`) mà còn cung cấp nhiều công cụ mạnh mẽ để tương tác với message bus:

* **Truy cập message:** `context.Message`
* **Truy cập headers:** `context.Headers`
* **Gửi hoặc publish message khác:** `context.Send(...)`, `context.Publish(...)`
* **Tương tác với bus:** `context.RespondAsync(...)` (cho kịch bản request/response)
* **Truy cập thông tin vận chuyển:** `context.ReceiveContext.TransportHeaders`

---

## 3. Vòng đời của Consumer (Lifecycle)

MassTransit được thiết kế để tích hợp liền mạch với các **Dependency Injection (DI) container**.

* **Vòng đời khuyến nghị:** **Transient** hoặc **Scoped**.
* **Ý nghĩa:** Một instance consumer **mới** sẽ được tạo ra cho **mỗi message** được xử lý.
* **Lợi ích:**
    * **An toàn về luồng (Thread-safe):** Loại bỏ rủi ro khi nhiều luồng cùng truy cập một instance.
    * **Quản lý tài nguyên hiệu quả:** Các tài nguyên như `DbContext` được tạo và giải phóng cùng với consumer, tránh rò rỉ bộ nhớ.

---

## 4. Cấu hình Consumer

Để MassTransit biết đến consumer của bạn, bạn cần đăng ký và cấu hình nó khi khởi tạo bus.

```csharp
services.AddMassTransit(x =>
{
    // Tự động quét và đăng ký tất cả consumer trong một Assembly
    x.AddConsumers(typeof(Program).Assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        // Tự động cấu hình các endpoint và kết nối consumer tương ứng
        cfg.ConfigureEndpoints(context);
    });
});
```

Phương thức `ConfigureEndpoints(context)` cực kỳ mạnh mẽ. Nó sẽ tự động tạo queue, exchange, và binding cần thiết cho mỗi consumer đã được đăng ký.

---

## 5. Các loại Consumer đặc biệt

Ngoài `IConsumer<T>`, MassTransit còn hỗ trợ các loại consumer chuyên dụng khác:

### Consumer xử lý lỗi (`IConsumer<Fault<TMessage>>`)
* Khi một consumer xử lý `TMessage` thất bại (sau khi đã retry), MassTransit sẽ tự động publish một message lỗi có kiểu là `Fault<TMessage>`.
* Bạn có thể tạo một consumer riêng để lắng nghe các sự kiện lỗi này, phục vụ cho việc giám sát, cảnh báo hoặc ghi log.

### Batch Consumer (`IConsumer<Batch<TMessage>>`)
* Cho phép xử lý **một lô (batch)** các message cùng một lúc thay vì từng cái một.
* Rất hiệu quả cho các tác vụ cần thông lượng cao, ví dụ như ghi đồng loạt nhiều bản ghi vào database.

```csharp
public class UpdateCustomerConsumer : IConsumer<Batch<UpdateCustomerAddress>>
{
    public async Task Consume(ConsumeContext<Batch<Update-CustomerAddress>> context)
    {
        // context.Message là một mảng chứa thông tin của các message trong lô
        for(int i=0; i < context.Message.Length; i++)
        {
            var customerAddress = context.Message[i].Message;
            // ... Logic xử lý
        }
    }
}
```
---

## 6. Consumer Definition

**Consumer Definition** là một class tùy chọn giúp bạn **tập trung hóa cấu hình** cho một loại consumer cụ thể. Thay vì phải lặp lại các thiết lập (như retry policy, concurrency limit) ở nhiều nơi, bạn có thể định nghĩa chúng một lần.

```csharp
public class SubmitOrderConsumerDefinition : ConsumerDefinition<Submit-OrderConsumer>
{
    public SubmitOrderConsumerDefinition()
    {
        // Đặt tên endpoint mặc định cho consumer này
        EndpointName = "order-submissions";

        // Cấu hình retry policy mặc định cho consumer này
        Endpoint(e => e.UseMessageRetry(r => r.Interval(5, 1000)));
    }
}
```
Khi bạn gọi `ConfigureEndpoints`, MassTransit sẽ tự động tìm và áp dụng các `Definition` này, giúp code cấu hình của bạn gọn gàng và nhất quán.