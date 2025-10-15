# Tổng quan về Cấu hình Saga Tùy chỉnh

Tài liệu này đi sâu vào cách **cấu hình thủ công** một Saga trên một receive endpoint, dựa trên documentation chính thức. Phương pháp này cho phép bạn kiểm soát chi tiết hơn so với việc dùng `ConfigureEndpoints` tự động.

---

## 1. Khi nào cần Cấu hình Tùy chỉnh?

Thông thường, `ConfigureEndpoints` là đủ cho hầu hết các trường hợp. Tuy nhiên, bạn sẽ cần cấu hình thủ công khi:
* Bạn muốn nhiều Saga và/hoặc Consumer cùng lắng nghe trên **một queue duy nhất**.
* Bạn cần áp dụng các **middleware (filter)** đặc biệt chỉ cho một Saga trên một endpoint cụ thể.
* Bạn muốn ghi đè các cấu hình mặc định từ `SagaDefinition` cho một trường hợp đặc biệt.

---

## 2. Phương thức `ConfigureSaga`

`ConfigureSaga<TState>` là phương thức mở rộng chính được sử dụng bên trong một `ReceiveEndpoint` để kết nối một Saga đã được đăng ký vào endpoint đó.

### Cú pháp cơ bản
Phương thức này kết nối Saga và repository của nó vào pipeline của endpoint.

```csharp
cfg.ReceiveEndpoint("order-processing-queue", e =>
{
    // Kết nối OrderState saga vào endpoint này.
    // MassTransit sẽ tự động tìm repository đã được đăng ký cho OrderState.
    e.ConfigureSaga<OrderState>(context);
});
```

### Thêm Middleware vào Pipeline của Saga
Bạn có thể truyền vào một action để cấu hình sâu hơn vào pipeline xử lý message của riêng Saga đó.

```csharp
cfg.ReceiveEndpoint("order-processing-queue", e =>
{
    e.ConfigureSaga<OrderState>(context, sagaConfigurator =>
    {
        // Thêm một filter (middleware) vào pipeline của Saga.
        // Filter này sẽ chỉ chạy cho các message được xử lý bởi OrderState Saga
        // trên endpoint "order-processing-queue" này.
        sagaConfigurator.UseFilter(new MySagaLoggingFilter<OrderState>());
    });
});
```
* **`sagaConfigurator`** (`ISagaConfigurator<TState>`): Cho phép bạn truy cập vào pipeline của Saga để thêm các hành vi tùy chỉnh.

---

## 3. Saga Message Pipeline 🧩

Mỗi khi một message đến và được định tuyến đến một Saga, nó sẽ đi qua một pipeline các bước xử lý. Bằng cách cấu hình tùy chỉnh, bạn có thể "chèn" các filter của riêng mình vào pipeline này.



**Ví dụ về một Filter tùy chỉnh:**
Một filter là một class implement `IFilter` cho phép bạn thực thi code **trước và sau khi** message được xử lý bởi Saga.

```csharp
public class MySagaLoggingFilter<T> : IFilter<SagaConsumeContext<T>>
    where T : class, ISaga
{
    public async Task Send(SagaConsumeContext<T> context, IPipe<SagaConsumeContext<T>> next)
    {
        _logger.LogInformation($"SAGA: Processing message {context.Message.GetType().Name} for {context.Saga.CorrelationId}");

        // Chuyển tiếp đến bước tiếp theo trong pipeline (chính là Saga)
        await next.Send(context);

        _logger.LogInformation($"SAGA: Finished processing message for {context.Saga.CorrelationId}");
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("my-saga-logging-filter");
    }
}
```

---

## 4. Ví dụ: Dùng chung Queue

Đây là trường hợp sử dụng phổ biến nhất cho việc cấu hình thủ công.

```csharp
cfg.ReceiveEndpoint("shared-queue", e =>
{
    // Endpoint này sẽ xử lý các message cho cả hai Saga
    e.ConfigureSaga<OrderState>(context);
    e.ConfigureSaga<ShipmentState>(context);
    
    // Và nó cũng có thể xử lý message cho một Consumer
    e.ConfigureConsumer<ProcessPaymentConsumer>(context);
});
```
Trong ví dụ này, một queue duy nhất tên là `shared-queue` sẽ nhận và xử lý các message cho `OrderState`, `ShipmentState`, và `ProcessPaymentConsumer`.