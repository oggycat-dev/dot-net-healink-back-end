# Tổng quan về Cấu hình RabbitMQ Transport

Tài liệu này tổng hợp các khái niệm cốt lõi về cách cấu hình **RabbitMQ** làm message broker cho MassTransit, dựa trên documentation chính thức.

---

## 1. Cấu hình cơ bản

Để sử dụng RabbitMQ, bạn cần cài đặt gói NuGet `MassTransit.RabbitMQ` và gọi phương thức `UsingRabbitMq` trong `AddMassTransit`.

```csharp
services.AddMassTransit(x =>
{
    // ... đăng ký consumers, sagas...

    x.UsingRabbitMq((context, cfg) =>
    {
        // Cấu hình kết nối và các endpoint
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        
        cfg.ConfigureEndpoints(context);
    });
});
```

### A. Cấu hình Host
Bạn có thể cấu hình kết nối đến RabbitMQ theo nhiều cách:
* **Dùng các thuộc tính riêng lẻ (như ví dụ trên).**
* **Dùng một chuỗi kết nối (Connection String):**
  ```csharp
  cfg.Host("amqp://guest:guest@localhost:5672");
  ```
* **Cấu hình Cluster (để có tính sẵn sàng cao):**
  ```csharp
  cfg.Host("localhost", "/", h => 
  {
      h.Username("guest");
      h.Password("guest");
      
      // Thêm các node khác trong cluster
      h.UseCluster(c => 
      {
          c.Node("rabbit-1");
          c.Node("rabbit-2");
      });
  });
  ```
### B. SSL/TLS
MassTransit hỗ trợ đầy đủ kết nối bảo mật qua SSL/TLS.
```csharp
cfg.Host("secure.rabbitmq.com", 5671, "/", h =>
{
    h.UseSsl(s => 
    {
        s.Protocol = SslProtocols.Tls12;
        // ... các cấu hình certificate khác
    });
});
```

---

## 2. Topology: Cách MassTransit tạo Exchange và Queue

**Topology** là cách MassTransit tổ chức các exchange, queue, và binding trong RabbitMQ. Mặc định, MassTransit sử dụng một kiến trúc rất linh hoạt:

* **Một Fanout Exchange cho mỗi loại Message:** Khi bạn publish một message `IOrderSubmitted`, MassTransit sẽ gửi nó đến một exchange tên là `IOrderSubmitted`.
* **Một Queue cho mỗi Receive Endpoint:** Mỗi endpoint (thường tương ứng với một consumer hoặc saga) sẽ có một queue riêng.
* **Binding:** MassTransit sẽ tự động tạo một **binding** từ exchange của message đến queue của endpoint.



Nhờ kiến trúc này, khi bạn publish một event, nó sẽ được tự động định tuyến đến tất cả các queue của các consumer đã đăng ký nhận event đó.

---

## 3. Cấu hình Receive Endpoint

Bạn có thể tùy chỉnh các thuộc tính của RabbitMQ ngay tại nơi cấu hình receive endpoint.

```csharp
cfg.ReceiveEndpoint("my-queue", e =>
{
    // e là IRabbitMqReceiveEndpointConfigurator

    // -- Các thuộc tính chung --
    e.Durable = true;        // Queue sẽ tồn tại sau khi broker khởi động lại (mặc định là true)
    e.AutoDelete = false;    // Queue sẽ không bị xóa khi consumer cuối cùng ngắt kết nối (mặc định là false)
    e.PrefetchCount = 16;    // Số lượng message mà consumer có thể nhận cùng lúc

    // -- Các thuộc tính riêng của RabbitMQ --
    e.Bind<IOrderSubmitted>(); // Tường minh bind message type này vào endpoint
    
    // Đặt các arguments tùy chỉnh cho queue, ví dụ: message TTL
    e.SetQueueArgument("x-message-ttl", 60000); // message sẽ tự hết hạn sau 60s
});
```

---

## 4. Tùy chỉnh Entity Name (Tên Exchange và Queue)

Mặc định, MassTransit sử dụng tên đầy đủ của kiểu .NET (bao gồm cả namespace) để đặt tên cho exchange. Đôi khi điều này khá dài dòng. Bạn có thể tùy chỉnh lại bằng cách cung cấp một `IEntityNameFormatter`.

MassTransit cung cấp sẵn `KebabCaseEndpointNameFormatter`.

```csharp
// Trong AddMassTransit
x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("my-prefix", false));

// ...
x.UsingRabbitMq((context, cfg) => 
{
    cfg.ConfigureEndpoints(context);
});
```
* **Kết quả:**
    * Tên queue của `SubmitOrderConsumer` sẽ là `my-prefix-submit-order`.
    * Tên exchange của message `OrderSubmitted` sẽ là `OrderSubmitted` (vì `includeNamespace` là `false`).