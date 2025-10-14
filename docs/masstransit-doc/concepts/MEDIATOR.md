# Tổng quan về Mediator trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **Mediator**, một tính năng của MassTransit để giao tiếp message **hoàn toàn trong bộ nhớ (in-memory)**, dựa trên documentation chính thức.

---

## 1. Mediator là gì? 🧠

**Mediator** là một message bus có hiệu năng cực cao, hoạt động **bên trong một tiến trình duy nhất (in-process)**. Nó cho phép bạn sử dụng các pattern mạnh mẽ của MassTransit (như Consumers, Request/Response) để khớp nối lỏng các thành phần trong cùng một ứng dụng mà **không cần đến một message broker** như RabbitMQ.

Nó thực chất là một triển khai của **Mediator Pattern**, giúp giảm sự phụ thuộc trực tiếp giữa các class.



---

## 2. So sánh Mediator và Message Bus đầy đủ

| Tiêu chí | Mediator (`AddMediator`) | Message Bus (`AddMassTransit`) |
| :--- | :--- | :--- |
| **Phạm vi** | Chỉ trong một service/ứng dụng | Giao tiếp giữa nhiều service |
| **Broker** | ⛔ **Không** cần broker | ✅ **Cần** broker (RabbitMQ, Azure Service Bus, etc.) |
| **Độ trễ** | Cực thấp (gần như gọi hàm trực tiếp) | Cao hơn (do mạng và serialization) |
| **Độ bền (Durability)** | Không (message mất khi ứng dụng tắt) | Có (message được lưu trữ bởi broker) |
| **Mục đích** | Khớp nối lỏng các thành phần nội bộ | Tích hợp và giao tiếp giữa các hệ thống |

---

## 3. Khi nào nên dùng Mediator?

Mediator là một lựa chọn tuyệt vời để **cải thiện kiến trúc bên trong một service**, đặc biệt là các ứng dụng monolith hoặc một microservice phức tạp.

* **Tách biệt các mối quan tâm trong API Controller:** Thay vì gọi trực tiếp các service, controller chỉ cần gửi một command hoặc request đến Mediator.
* **Xử lý các sự kiện bên trong ứng dụng:** Khi một hành động xảy ra (ví dụ: người dùng cập nhật hồ sơ), bạn có thể publish một event qua Mediator để các thành phần khác (như xóa cache, cập nhật log) có thể phản ứng mà không cần耦合 cứng.
* **Thay thế cho các thư viện Mediator khác (như MediatR):** Nếu bạn đã dùng MassTransit cho giao tiếp liên-service, bạn có thể dùng `MassTransit.Mediator` để có một API nhất quán cho cả giao tiếp nội bộ và bên ngoài.

---

## 4. Cấu hình Mediator

Việc cấu hình Mediator rất đơn giản, chỉ cần gọi `AddMediator()` thay vì `AddMassTransit()`.

```csharp
// Trong Program.cs hoặc Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddMediator(cfg =>
    {
        // Đăng ký consumer như bình thường
        cfg.AddConsumer<SubmitOrderConsumer>();
        
        // Đăng ký request client
        cfg.AddRequestClient<SubmitOrder>();
    });
}
```

---

## 5. Sử dụng các Pattern của MassTransit với Mediator

Bạn có thể sử dụng gần như tất cả các pattern quen thuộc của MassTransit:

### Consumers
Hoạt động y hệt như với bus đầy đủ. Một `IConsumer<T>` sẽ được gọi khi có message `T` được publish hoặc send qua Mediator.

### Requests
`IRequestClient<T>` hoạt động hoàn hảo với Mediator để thực hiện các cuộc gọi request/response bên trong ứng dụng. Đây là một cách tuyệt vời để tách biệt logic nghiệp vụ khỏi các lớp giao diện như API Controller.

```csharp
// Trong một API Controller
[ApiController]
public class OrderController : ControllerBase
{
    private readonly IRequestClient<SubmitOrder> _submitOrderClient;

    public OrderController(IRequestClient<SubmitOrder> submitOrderClient)
    {
        _submitOrderClient = submitOrderClient;
    }

    [HttpPost]
    public async Task<IActionResult> Post(OrderModel model)
    {
        // Gửi request đến consumer nội bộ qua Mediator
        var response = await _submitOrderClient.GetResponse<OrderSubmissionAccepted>(model);
        
        return Ok(response.Message);
    }
}
```

### Sagas
Bạn cũng có thể sử dụng Saga State Machine với Mediator, nhưng chúng sẽ chỉ tồn tại trong bộ nhớ (in-memory saga repository).