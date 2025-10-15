# Tổng quan về Cấu hình (Configuration) trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về cách **cấu hình** MassTransit, dựa trên documentation chính thức. MassTransit sử dụng phương pháp cấu hình bằng code (code-based configuration) và tích hợp sâu với hệ thống Dependency Injection (DI) của .NET.

---

## 1. Nguyên tắc cấu hình

Toàn bộ việc cấu hình MassTransit được thực hiện thông qua các phương thức mở rộng (extension methods) trên `IServiceCollection`, bắt đầu bằng `AddMassTransit` (cho bus đầy đủ) hoặc `AddMediator` (cho bus nội bộ).

```csharp
// Trong Program.cs
builder.Services.AddMassTransit(x => 
{
    // ... Cấu hình components (consumers, sagas...)

    // ... Cấu hình transport (RabbitMQ, Azure Service Bus...)
});
```

---

## 2. Cấu hình Bus và Transport

Bên trong `AddMassTransit`, bạn cần chỉ định transport sẽ được sử dụng.

* **`x.UsingRabbitMq((context, cfg) => { ... })`**
* **`x.UsingAzureServiceBus((context, cfg) => { ... })`**
* **`x.UsingAmazonSqs((context, cfg) => { ... })`**
* **`x.UsingInMemory((context, cfg) => { ... })`** (thường dùng cho testing)

Phương thức cấu hình transport này nhận vào 2 tham số quan trọng:
* `context` (`IBusRegistrationContext`): Dùng để truy cập và giải quyết các DI container, cho phép các component của bạn (như consumer) có thể inject các service khác.
* `cfg` (`IBusFactoryConfigurator`): Dùng để cấu hình các chi tiết của bus và các receive endpoint.

```csharp
x.UsingRabbitMq((context, cfg) =>
{
    // Cấu hình host RabbitMQ
    cfg.Host("localhost", "/", h =>
    {
        h.Username("guest");
        h.Password("guest");
    });
    
    // Cấu hình các receive endpoint
    cfg.ConfigureEndpoints(context);
});
```

---

## 3. Đăng ký Components (Consumers, Sagas, etc.)

Bạn cần đăng ký tất cả các component mà bus sẽ sử dụng.

### Đăng ký thủ công từng component:
```csharp
services.AddMassTransit(x =>
{
    x.AddConsumer<SubmitOrderConsumer>();
    x.AddSaga<OrderStateMachine>();
});
```

### Quét và đăng ký tự động (Assembly Scanning):
Đây là cách làm phổ biến và tiện lợi hơn, MassTransit sẽ tự động tìm tất cả các component trong một hoặc nhiều assembly.

```csharp
services.AddMassTransit(x =>
{
    // Quét assembly chứa class Program để tìm tất cả consumers, sagas...
    x.AddConsumers(typeof(Program).Assembly);
    x.AddSagas(typeof(Program).Assembly);
});
```

---

## 4. Cấu hình Endpoints

Endpoint là nơi message được nhận từ broker. MassTransit cung cấp hai cách chính để cấu hình chúng.

### A. Cấu hình tự động với `ConfigureEndpoints` 🪄

Đây là phương pháp được khuyến nghị. Bằng cách gọi `cfg.ConfigureEndpoints(context)`, MassTransit sẽ tự động:
1.  Quét tất cả các consumer, saga đã được đăng ký.
2.  Tạo ra các **receive endpoint** (tương ứng với các queue trong RabbitMQ) cho chúng.
3.  Áp dụng các cấu hình mặc định hoặc các **Definitions** (xem bên dưới) nếu có.
4.  Kết nối (bind) các exchange và queue cần thiết.

Nó giúp đơn giản hóa việc cấu hình một cách đáng kể.

### B. Cấu hình thủ công với `ReceiveEndpoint`

Nếu cần kiểm soát chi tiết hơn, bạn có thể tự định nghĩa các receive endpoint.

```csharp
cfg.ReceiveEndpoint("order-queue", e =>
{
    // Cấu hình retry policy cho riêng endpoint này
    e.UseMessageRetry(r => r.Interval(5, 1000));
    
    // Chỉ định consumer nào sẽ xử lý message trên endpoint này
    e.ConfigureConsumer<SubmitOrderConsumer>(context);
    e.ConfigureConsumer<CancelOrderConsumer>(context);
});
```

---

## 5. Sử dụng Definitions để tái sử dụng cấu hình

**Definitions** (ví dụ: `ConsumerDefinition`, `SagaDefinition`) là một cách tuyệt vời để đóng gói các cấu hình chung cho một component.

Thay vì cấu hình retry policy cho `SubmitOrderConsumer` ở nhiều nơi, bạn có thể tạo một `SubmitOrderConsumerDefinition`.

```csharp
public class SubmitOrderConsumerDefinition : 
    ConsumerDefinition<SubmitOrderConsumer>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, 
        IConsumerConfigurator<SubmitOrderConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Tất cả endpoint sử dụng consumer này sẽ có retry policy này
        endpointConfigurator.UseMessageRetry(r => r.Interval(3, 500));
    }
}
```

Khi bạn gọi `cfg.ConfigureEndpoints(context)`, nó sẽ tự động tìm và áp dụng các `Definition` này.