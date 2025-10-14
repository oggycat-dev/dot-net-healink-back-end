# Tổng quan về Requests trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về pattern **Request/Response** trong MassTransit, dựa trên documentation chính thức. Đây là một mẫu giao tiếp mạnh mẽ cho phép một service gửi một message yêu cầu và chờ đợi một message phản hồi.

---

## 1. Request/Response Pattern là gì? 🔄

Không giống như `Send` (gửi một chiều) hay `Publish` (phát sóng), Request/Response là một cuộc hội thoại hai chiều.

1.  **Client (Bên yêu cầu):** Gửi đi một message **Request**.
2.  **Server (Bên trả lời):** Nhận message Request, xử lý, và gửi lại một message **Response**.
3.  **Client:** Nhận message Response và tiếp tục công việc của mình.

MassTransit tự động hóa toàn bộ quá trình phức tạp phía sau, bao gồm việc tạo queue trả lời, quản lý `CorrelationId` để ghép cặp request và response, và xử lý timeout.



---

## 2. Client: Gửi Request với `IRequestClient`

`IRequestClient<TRequest>` là interface chính để gửi đi các request. Nó thường được đăng ký bằng Dependency Injection (DI) và inject vào các class của bạn (như API Controller).

```csharp
public class OrderController : ControllerBase
{
    private readonly IRequestClient<CheckOrderStatus> _client;

    public OrderController(IRequestClient<CheckOrderStatus> client)
    {
        _client = client;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        // Gửi request và chờ response (hoặc nhiều loại response)
        var (status, notFound) = await _client.GetResponse<OrderStatus, OrderNotFound>(new { OrderId = id });

        if (status.IsCompletedSuccessfully)
        {
            // Nhận được response OrderStatus
            var response = await status;
            return Ok(response.Message);
        }
        else
        {
            // Nhận được response OrderNotFound
            var response = await notFound;
            return NotFound(response.Message);
        }
    }
}
```

### Các điểm chính của Client:
* **`GetResponse<TResponse1, TResponse2...>`:** Phương thức chính để gửi request. Nó trả về một `Task<Response<...>>` cho phép bạn `await` và xử lý nhiều loại response khác nhau.
* **Timeout:** `IRequestClient` có timeout mặc định (thường là 30 giây). Nếu không nhận được response trong khoảng thời gian này, một `RequestTimeoutException` sẽ được ném ra.
* **Cancellation Token:** Bạn có thể truyền một `CancellationToken` để hủy request giữa chừng.

---

## 3. Server: Trả lời Request trong Consumer

Bên trả lời (server) chỉ đơn giản là một **Consumer** bình thường. Điểm khác biệt duy nhất là thay vì chỉ xử lý, nó sẽ dùng `context.RespondAsync()` để gửi message trả lời lại cho người yêu cầu.

```csharp
// Consumer xử lý request CheckOrderStatus
public class CheckOrderStatusConsumer : IConsumer<CheckOrderStatus>
{
    private readonly IOrderRepository _orderRepository;

    public async Task Consume(ConsumeContext<CheckOrderStatus> context)
    {
        var order = await _orderRepository.Get(context.Message.OrderId);

        if (order != null)
        {
            // Trả về response OrderStatus nếu tìm thấy
            await context.RespondAsync<OrderStatus>(new 
            {
                order.OrderId,
                order.Status,
                order.Timestamp
            });
        }
        else
        {
            // Trả về response OrderNotFound nếu không tìm thấy
            await context.RespondAsync<OrderNotFound>(new 
            {
                context.Message.OrderId,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
```

### Các điểm chính của Server:
* **`context.RespondAsync<TResponse>(...)`:** Là phương thức dùng để gửi response. MassTransit sẽ tự động đọc các header cần thiết từ `ConsumeContext` (như `ResponseAddress`, `CorrelationId`) để đảm bảo response được gửi về đúng nơi.
* **Không cần cấu hình đặc biệt:** Bạn chỉ cần đăng ký consumer này như bất kỳ consumer nào khác.

---

## 4. Cơ chế hoạt động "ẩn"

MassTransit xử lý toàn bộ sự phức tạp của pattern này cho bạn:
1.  Khi Client gửi request, MassTransit tạo ra một **địa chỉ trả lời (Response Address)** tạm thời và độc nhất.
2.  Địa chỉ này và một **CorrelationId** duy nhất được đính kèm vào header của message request.
3.  Khi Server gọi `RespondAsync`, MassTransit đọc các header đó và gửi message response đến đúng địa chỉ trả lời với cùng `CorrelationId`.
4.  Client lắng nghe trên địa chỉ trả lời đó, nhận message có `CorrelationId` khớp, và hoàn thành `Task` mà bạn đang `await`.