# Tổng quan về State Machine Sagas trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **State Machine Saga**, cách triển khai Saga Pattern mạnh mẽ và rõ ràng nhất trong MassTransit, dựa trên documentation chính thức.

---

## 1. State Machine Saga là gì? 🤖

**State Machine Saga** là cách triển khai pattern Orchestration Saga, trong đó toàn bộ quy trình nghiệp vụ được định nghĩa một cách tường minh như một **máy trạng thái (state machine)**. Nó cho phép bạn mô tả một quy trình phức tạp bằng các khái niệm quen thuộc: **Trạng thái (States)**, **Sự kiện (Events)**, và **Hành vi (Behaviors)**.

Đây là cách tiếp cận được khuyến nghị cho hầu hết các quy trình nghiệp vụ có nhiều bước, nhiều nhánh, hoặc cần xử lý lỗi và bồi thường phức tạp.

---

## 2. Các thành phần chính

### `MassTransitStateMachine<TState>`
Đây là class cơ sở mà mọi state machine của bạn phải kế thừa, với `TState` là class chứa dữ liệu trạng thái của saga.

### `TState` (Saga Instance)
Một class implement `SagaStateMachineInstance`, chứa tất cả dữ liệu cần được lưu trữ cho một quy trình (ví dụ: `OrderId`, `CustomerId`, `CurrentState`).

### `State`
Một thuộc tính trong state machine, đại diện cho một "điểm dừng" trong quy trình. MassTransit cung cấp các trạng thái có sẵn là `Initial` và `Final`.

### `Event`
Một thuộc tính trong state machine, đại diện cho một message đến có thể kích hoạt một sự thay đổi trạng thái hoặc một hành vi.

---

## 3. Cấu trúc của một State Machine

Toàn bộ logic của state machine được định nghĩa trong hàm khởi tạo (constructor) của nó.

```csharp
public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    // 1. Khai báo các Trạng thái
    public State Submitted { get; private set; }
    public State Accepted { get; private set; }
    public State Cancelled { get; private set; }

    // 2. Khai báo các Events
    public Event<OrderSubmitted> OrderSubmittedEvent { get; private set; }
    public Event<OrderAccepted> OrderAcceptedEvent { get; private set; }
    public Event<OrderCancelled> OrderCancelledEvent { get; private set; }

    // 3. Định nghĩa toàn bộ workflow trong hàm khởi tạo
    public OrderStateMachine()
    {
        // Chỉ định thuộc tính nào trong OrderState sẽ lưu tên trạng thái hiện tại
        InstanceState(x => x.CurrentState);

        // Cấu hình correlation cho các events
        Event(() => OrderSubmittedEvent, x => x.CorrelateBy(s => s.OrderId, ctx => ctx.Message.OrderId).Select(SelectMode.Insert));
        Event(() => OrderAcceptedEvent, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => OrderCancelledEvent, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        // 4. Định nghĩa các hành vi
        Initially(
            When(OrderSubmittedEvent)
                .Then(context => {
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.CustomerNumber = context.Message.CustomerNumber;
                })
                .Publish(context => new OrderReceived(context.Saga.CorrelationId))
                .TransitionTo(Submitted)
        );

        During(Submitted,
            When(OrderAcceptedEvent)
                .TransitionTo(Accepted),
            
            When(OrderCancelledEvent)
                .TransitionTo(Cancelled)
        );
        
        // Khi một saga ở trạng thái Final, nó có thể được xóa khỏi repository
        SetCompletedWhenFinalized();
    }
}
```

---

## 4. Các "Động từ" hành vi (Behaviors)

Bên trong các khối `When(Event)`, bạn sử dụng các "động từ" để định nghĩa những gì saga sẽ làm.

* **`.Then(context => ...)`:** Thực thi một đoạn code C# tùy ý. Dùng để gán dữ liệu, logging, v.v.
* **`.Publish<T>(...)`:** Xuất bản (publish) một event mới.
* **`.Send<T>(...)`:** Gửi (send) một command đến một endpoint cụ thể.
* **`.Request<TRequest, TResponse>(...)`:** Gửi một request và chờ response.
* **`.TransitionTo(State)`:** Thay đổi trạng thái hiện tại của saga.
* **`.Finalize()`:** Chuyển saga đến trạng thái cuối cùng. Instance saga sẽ bị xóa nếu `SetCompletedWhenFinalized()` được gọi.
* **`.Schedule<T>(...)`:** Lên lịch gửi một message trong tương lai (ví dụ: để xử lý timeout).
* **`.Respond<T>(...)`:** Trả lời một request.

Sự kết hợp của các khối `Initially`/`During` và các "động từ" này cho phép bạn xây dựng các quy trình nghiệp vụ phức tạp một cách rất rõ ràng và dễ đọc.