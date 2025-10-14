# Tổng quan về Giám sát Routing Slip bằng Saga

Tài liệu này tổng hợp các khái niệm cốt lõi về việc sử dụng một **Saga** để theo dõi và quản lý trạng thái của một **Routing Slip**, dựa trên documentation chính thức.

---

## 1. Tại sao cần giám sát Routing Slip? 📡

Một Routing Slip có thể tự chạy một cách độc lập. Tuy nhiên, trong nhiều quy trình nghiệp vụ, bạn cần một nơi trung tâm để:
* **Theo dõi trạng thái tổng thể:** Biết được toàn bộ workflow đã hoàn thành, thất bại hay đang chạy.
* **Lưu trữ kết quả:** Giữ lại kết quả cuối cùng hoặc các biến (variables) từ routing slip.
* **Thực hiện hành động phức tạp khi hoàn tất/thất bại:** Ví dụ, gửi một thông báo đặc biệt, hoặc kích hoạt một quy trình bồi thường khác.

Một **Saga** chính là công cụ hoàn hảo để đóng vai trò "người giám sát" cho một Routing Slip.

---

## 2. Luồng hoạt động



1.  **Người khởi tạo (Initiator):** Tạo ra một Routing Slip như bình thường.
2.  **Đăng ký (Subscription):** Trước khi thực thi, người khởi tạo sẽ **thêm một đăng ký** vào Routing Slip. Đăng ký này chứa địa chỉ endpoint của Saga giám sát.
3.  **Thực thi Routing Slip:** Routing Slip bắt đầu thực hiện các Activity của nó.
4.  **Phát sự kiện (Publish Events):** Trong quá trình chạy, Routing Slip sẽ tự động **publish** các sự kiện về trạng thái của nó, ví dụ:
    * `RoutingSlipActivityCompleted`
    * `RoutingSlipFaulted` (khi một activity thất bại)
    * `RoutingSlipCompleted` (khi toàn bộ workflow hoàn thành)
5.  **Saga nhận sự kiện:** Do đã được đăng ký ở bước 2, Saga sẽ nhận được các sự kiện này.
6.  **Saga cập nhật trạng thái:** Saga sử dụng các sự kiện này để cập nhật trạng thái nội tại của nó (ví dụ: chuyển từ `Running` sang `Completed` hoặc `Faulted`).

---

## 3. Cách triển khai Saga giám sát

### A. Định nghĩa State và State Machine
Bạn tạo một State Machine Saga như bình thường. `CorrelationId` của saga sẽ chính là `TrackingNumber` của Routing Slip.

```csharp
public class RoutingSlipState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; } // Sẽ khớp với TrackingNumber
    public string CurrentState { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? FaultReason { get; set; }
}

public class RoutingSlipStateMachine : MassTransitStateMachine<RoutingSlipState>
{
    // Khai báo các events mà saga sẽ lắng nghe
    public Event<RoutingSlipCompleted> Completed { get; private set; }
    public Event<RoutingSlipFaulted> Faulted { get; private set; }

    public RoutingSlipStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // CorrelationId của saga chính là TrackingNumber của event
        Event(() => Completed, x => x.CorrelateById(context => context.Message.TrackingNumber));
        Event(() => Faulted, x => x.CorrelateById(context => context.Message.TrackingNumber));

        Initially(
            // Khi saga được tạo, nó ở trạng thái "Running" (không tường minh)
            // Chúng ta chỉ cần xử lý khi nó hoàn thành hoặc thất bại
        );

        During(Initial,
            When(Completed)
                .Then(context => context.Saga.EndTime = context.Message.Timestamp)
                .Finalize()), // Hoàn tất saga
            
            When(Faulted)
                .Then(context => 
                {
                    context.Saga.EndTime = context.Message.Timestamp;
                    context.Saga.FaultReason = context.GetExceptionInfo();
                })
                .Finalize()) // Hoàn tất saga
        );

        SetCompletedWhenFinalized();
    }
}
```

### B. Cách "Đăng ký" Saga vào Routing Slip (tại người khởi tạo)

Đây là bước quan trọng nhất, kết nối Routing Slip với Saga.

```csharp
// Lấy địa chỉ endpoint của Saga
var sagaAddress = await _requestClient.GetSendEndpoint(new Uri("queue:routing-slip-state"));

var builder = new RoutingSlipBuilder(NewId.NextGuid());
builder.AddActivity("DoSomething", new Uri("queue:do-something_execute"));

// Thêm đăng ký để gửi các sự kiện của slip đến địa chỉ của Saga
builder.AddSubscription(sagaAddress.Address, RoutingSlipEvents.All);

var routingSlip = builder.Build();

// Tạo instance saga TRƯỚC KHI thực thi slip
// Điều này đảm bảo saga đã tồn tại để nhận event đầu tiên
await sagaAddress.Send<RoutingSlipState>(new { CorrelationId = routingSlip.TrackingNumber });

// Thực thi routing slip
await _bus.Execute(routingSlip);
```
* **`RoutingSlipEvents.All`**: Chỉ định rằng tất cả các sự kiện (hoàn thành, lỗi, v.v.) sẽ được gửi đến saga. Bạn có thể chọn lọc chỉ gửi một số sự kiện nhất định.
* **`await sagaAddress.Send<RoutingSlipState>(...)`**: Đây là một bước quan trọng để "mồi" (prime) saga, tạo ra một instance trong repository trước khi routing slip bắt đầu gửi sự kiện.