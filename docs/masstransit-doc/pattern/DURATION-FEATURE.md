# Tổng quan về Durable Futures trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **Durable Future**, một pattern nâng cao trong MassTransit để điều phối các quy trình nghiệp vụ phức tạp, có nhiều bước và cần theo dõi kết quả cuối cùng, dựa trên documentation chính thức.

---

## 1. Durable Future là gì? 🔮

Hãy tưởng tượng **Durable Future** như một phiên bản nâng cao, bền vững và mạnh mẽ hơn của pattern Request/Response. Nó được thiết kế để điều phối các workflow bao gồm nhiều bước, có thể là tuần tự hoặc song song, và sau đó tổng hợp tất cả các kết quả lại.

Về bản chất, một Future là một **Saga State Machine được chuyên môn hóa**, có nhiệm vụ:
1.  **Khởi tạo** một quy trình.
2.  **Gửi đi** một hoặc nhiều command (các "bước" của quy trình).
3.  **Chờ đợi** và **thu thập** các kết quả từ mỗi bước.
4.  **Tổng hợp** các kết quả đó thành một kết quả cuối cùng.
5.  **Lưu trữ** trạng thái và kết quả một cách bền vững.

**Analogy:** Giống như một hệ thống **theo dõi đơn hàng phức tạp**. Bạn đặt một đơn hàng (khởi tạo Future), hệ thống sẽ gửi yêu cầu đến kho (bước 1), hãng vận chuyển (bước 2), và cổng thanh toán (bước 3). Hệ thống sẽ theo dõi trạng thái của cả ba yêu cầu này và chỉ báo cáo "Hoàn thành" khi cả ba đều thành công.

---

## 2. So sánh Future và Request/Response

| Tiêu chí | Request/Response (`IRequestClient`) | Durable Future |
| :--- | :--- | :--- |
| **Số bước** | Một yêu cầu, một phản hồi | **Nhiều** yêu cầu, **nhiều** phản hồi |
| **Độ bền vững** | Không. Nếu client sập, context sẽ mất. | **Có.** Trạng thái được lưu trong database (giống Saga). |
| **Khả năng theo dõi** | Khó. Chỉ client mới biết trạng thái. | Dễ. Bất kỳ service nào cũng có thể truy vấn trạng thái Future bằng ID. |
| **Mô hình** | Giao tiếp điểm-điểm đơn giản | Điều phối (Orchestration) quy trình phức tạp |

---

## 3. Các thành phần chính

* **Future:** Class State Machine định nghĩa toàn bộ quy trình, kế thừa từ `MassTransit.Futures.Future`.
* **Arguments:** Dữ liệu đầu vào để khởi tạo Future.
* **Result:** Dữ liệu kết quả cuối cùng sau khi Future hoàn thành.
* **Itinerary (Lịch trình):** Danh sách các bước (command) mà Future sẽ thực hiện.
* **Step (Bước):** Một command được gửi đến một service khác. Service đó sẽ xử lý và trả về một `Result`.

---

## 4. Cách định nghĩa một Future

Một Future được định nghĩa như một State Machine.

```csharp
// Dữ liệu đầu vào
public record OrderArguments
{
    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
}

// Kết quả cuối cùng
public record OrderResult
{
    public Guid OrderId { get; init; }
    public string Status { get; init; }
}

// Định nghĩa Future
public class OrderFuture : Future<OrderArguments, OrderResult>
{
    public OrderFuture()
    {
        // Định nghĩa quy trình trong hàm khởi tạo
        ConfigureCommand(x => x.OrderId); // Chỉ định thuộc tính tương quan

        // Bước 1: Gửi command `ProcessPayment`
        var processPayment = When((future, command) => command.CustomerId != Guid.Empty)
            .Then(context => context.SendCommand<ProcessPayment>(new { /* ... */ }));

        // Bước 2: Gửi command `AllocateInventory`
        var allocateInventory = When((future, command) => /* ... */)
            .Then(context => context.SendCommand<AllocateInventory>(new { /* ... */ }));

        // Chỉ định các bước cần thực hiện
        // .All() nghĩa là các bước sẽ chạy song song
        Itinerary = Collect(processPayment, allocateInventory).All();

        // Xử lý khi tất cả các bước hoàn thành
        WhenAllCompleted(itinerary => 
        {
            itinerary.SetCompleted(context => new OrderResult { /* ... */ });
        });
    }
}
```

---

## 5. Cách sử dụng Future

### A. Khởi tạo Future (Client)
Client sử dụng `IFutureClient` để bắt đầu một quy trình.

```csharp
var client = provider.GetRequiredService<IFutureClient<OrderFuture>>();

// Gửi yêu cầu khởi tạo Future và nhận về một FutureResult
FutureResult<OrderResult> resultContext = await client.Submit(
    new OrderArguments { OrderId = NewId.NextGuid(), ... }, 
    TimeSpan.FromSeconds(60));

// Chờ đợi kết quả cuối cùng
Response<OrderResult> response = await resultContext.GetResponse<OrderResult>();
```

### B. Xử lý một Bước của Future (Service)
Service thực hiện một bước chỉ đơn giản là một **Consumer** nhận command và trả về kết quả.

```csharp
public class ProcessPaymentConsumer : IConsumer<ProcessPayment>
{
    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        // ... logic xử lý thanh toán ...

        // Trả về kết quả
        await context.RespondAsync<PaymentProcessed>(new { /* ... */ });
    }
}
```
Future sẽ tự động lắng nghe và xử lý các response `PaymentProcessed` này.