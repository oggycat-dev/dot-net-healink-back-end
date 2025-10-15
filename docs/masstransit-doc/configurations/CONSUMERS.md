# Tổng quan về Cấu hình Consumer trong MassTransit

Tài liệu này đi sâu vào các cách cấu hình **Consumer** trên các receive endpoint, dựa trên documentation chính thức.

---

## 1. Nguyên tắc cơ bản

Việc cấu hình một consumer bao gồm hai bước chính:
1.  **Đăng ký Consumer** với Dependency Injection (DI) container (ví dụ: `x.AddConsumer<MyConsumer>()`).
2.  **Kết nối Consumer** đó vào một (hoặc nhiều) **Receive Endpoint** để nó bắt đầu nhận message.

MassTransit cung cấp cả hai phương pháp tự động và thủ công để thực hiện bước thứ hai.

---

## 2. Cấu hình tự động với `ConfigureEndpoints` 🪄

Đây là phương pháp đơn giản và được khuyến nghị nhất. Khi bạn gọi `cfg.ConfigureEndpoints(context)`, MassTransit sẽ tự động thực hiện các công việc sau cho mỗi consumer đã được đăng ký:

* **Tạo một Receive Endpoint (Queue):** Tên của endpoint sẽ được tự động suy ra từ tên của class consumer theo quy tắc **kebab-case**.
    * Ví dụ: `SubmitOrderConsumer` ➡️ `submit-order`.
* **Áp dụng Consumer Definition:** Nếu có một `ConsumerDefinition` tương ứng, các cấu hình trong đó (như `EndpointName`, `ConcurrentMessageLimit`) sẽ được áp dụng.
* **Kết nối Consumer:** Tự động kết nối consumer vào endpoint vừa được tạo.

```csharp
// Trong cấu hình bus
x.UsingRabbitMq((context, cfg) =>
{
    // Chỉ cần dòng này, MassTransit sẽ lo phần còn lại
    cfg.ConfigureEndpoints(context);
});
```

### Tùy chỉnh tên Endpoint với Definition
Bạn có thể dễ dàng ghi đè tên endpoint mặc định bằng cách sử dụng `ConsumerDefinition`.

```csharp
public class SubmitOrderConsumerDefinition :
    ConsumerDefinition<SubmitOrderConsumer>
{
    public SubmitOrderConsumerDefinition()
    {
        // Ghi đè tên endpoint mặc định
        EndpointName = "order-processing-service";
    }
}
```
Bây giờ, `ConfigureEndpoints` sẽ tạo ra một queue tên là `order-processing-service` cho `SubmitOrderConsumer`.

---

## 3. Cấu hình thủ công trên Receive Endpoint

Nếu bạn cần kiểm soát nhiều hơn, ví dụ như cho nhiều consumer cùng lắng nghe trên một queue, bạn có thể cấu hình thủ công.

```csharp
cfg.ReceiveEndpoint("shared-order-queue", e =>
{
    // e là IReceiveEndpointConfigurator

    // Kết nối SubmitOrderConsumer vào endpoint này
    e.ConfigureConsumer<SubmitOrderConsumer>(context);

    // Kết nối luôn cả CancelOrderConsumer vào CÙNG endpoint này
    e.ConfigureConsumer<CancelOrderConsumer>(context);
});
```
* **Lưu ý:** Khi bạn cấu hình một consumer thủ công trên một endpoint, `ConfigureEndpoints` sẽ **bỏ qua** consumer đó để tránh cấu hình trùng lặp.

---

## 4. Cấu hình riêng cho từng Consumer trên Endpoint

Bạn có thể thêm các middleware hoặc cấu hình chỉ áp dụng cho một consumer cụ thể trên một endpoint.

```csharp
cfg.ReceiveEndpoint("order-queue", e =>
{
    // Cấu hình chung cho cả endpoint
    e.UseMessageRetry(r => r.Interval(3, 1000));

    // Cấu hình riêng cho SubmitOrderConsumer
    e.ConfigureConsumer<SubmitOrderConsumer>(context, consumerCfg => 
    {
        // Thêm một policy retry khác, chỉ áp dụng cho consumer này
        consumerCfg.UseMessageRetry(r => r.Exponential(5, ...));
    });
});
```

---

## 5. Tổng kết về Consumer Definition

`ConsumerDefinition` là nơi tốt nhất để định nghĩa các thuộc tính cố định của một consumer.

* **`ConcurrentMessageLimit`:** Giới hạn số lượng message mà một instance consumer có thể xử lý đồng thời.
    ```csharp
    public class MyConsumerDefinition : ConsumerDefinition<MyConsumer>
    {
        public MyConsumerDefinition()
        {
            // Chỉ cho phép consumer này xử lý 10 message cùng lúc
            ConcurrentMessageLimit = 10;
        }
    }
    ```
* **`Endpoint` configuration:** Áp dụng các cấu hình mặc định cho endpoint của consumer (như retry policy, outbox...).
    ```csharp
    protected override void ConfigureConsumer(..., IRegistrationContext context)
    {
        // Bất kỳ endpoint nào host consumer này cũng sẽ có outbox
        endpointConfigurator.UseInMemoryOutbox(context);
    }
    ```

Sử dụng `Definition` giúp giữ cho cấu hình bus chính (`AddMassTransit`) được gọn gàng và tập trung vào việc kết nối transport, trong khi các chi tiết về nghiệp vụ của consumer được đóng gói tại nơi định nghĩa của nó.