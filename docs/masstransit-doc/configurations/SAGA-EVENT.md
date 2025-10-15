# Tổng quan về Cấu hình Sự kiện Saga (Saga Event)

Tài liệu này đi sâu vào cách khai báo và cấu hình các **Events** bên trong một Saga State Machine, dựa trên documentation chính thức. Event là "cánh cửa" để message từ bên ngoài có thể tương tác và thay đổi trạng thái của Saga.

---

## 1. Khai báo Event

Một `Event` trong state machine là một khai báo cho biết saga có thể xử lý một loại message cụ thể.

* **Cách khai báo:** Khai báo một thuộc tính `Event<TMessage>` trong class state machine, với `TMessage` là kiểu dữ liệu của message mà event này đại diện.

```csharp
public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    // Khai báo một Event tên là OrderSubmittedEvent
    // được kích hoạt bởi message có kiểu IOrderSubmitted
    public Event<IOrderSubmitted> OrderSubmittedEvent { get; private set; }
    
    // Khai báo Event cho message OrderAccepted
    public Event<OrderAccepted> OrderAcceptedEvent { get; private set; }
    
    // ...
}
```

---

## 2. Correlation: Chìa khóa để tìm đúng Saga 🔑

**Correlation** (Tương quan) là cơ chế quan trọng nhất khi cấu hình event. Nó chỉ cho MassTransit biết cách **tìm đúng instance saga** để xử lý một message đến, dựa trên dữ liệu chứa trong message đó.

Việc cấu hình correlation được thực hiện trong hàm khởi tạo của state machine.

### A. CorrelateById
Đây là cách phổ biến và đơn giản nhất, dùng khi message của bạn chứa `CorrelationId`.

```csharp
// Trong hàm khởi tạo của State Machine
Event(() => OrderAcceptedEvent, x => x.CorrelateById(context => context.Message.CorrelationId));
```
* **Luồng hoạt động:** Khi nhận được message `OrderAccepted`, MassTransit sẽ lấy giá trị của `context.Message.CorrelationId` và tìm một `OrderState` instance có cùng `CorrelationId` trong repository.

### B. CorrelateBy (dùng thuộc tính tùy chỉnh)
Thường dùng cho các event khởi tạo saga, khi `CorrelationId` chưa tồn tại. Bạn có thể tương quan bằng một thuộc tính nghiệp vụ, ví dụ `OrderId`.

```csharp
Event(() => OrderSubmittedEvent, x => 
    // Dùng thuộc tính OrderId của message để tìm saga instance có cùng OrderId
    x.CorrelateBy((saga, context) => saga.OrderId == context.Message.OrderId)
     // Nếu không tìm thấy, tạo một saga instance mới
     .Select(SelectMode.Insert)
);
```

### C. `InsertOnInitial`
Đây là một cách viết tắt tiện lợi cho các event có thể khởi tạo một saga mới.

```csharp
Initially(
    // Khi nhận được sự kiện OrderSubmittedEvent
    When(OrderSubmittedEvent)
        .Then(context => 
        {
            // Gán dữ liệu từ message vào trạng thái saga
            context.Saga.OrderId = context.Message.OrderId;
            context.Saga.SubmitDate = context.Message.Timestamp;
        })
        .TransitionTo(Submitted) // Chuyển sang trạng thái Submitted
);
```
Khi một event được đặt trong khối `Initially`, MassTransit sẽ tự động cấu hình **`InsertOnInitial`**. Điều này có nghĩa là: nếu không tìm thấy saga instance nào khớp với correlation, một instance mới sẽ được tạo ra.

---

## 3. Các khối hành vi: `Initially` và `During`

Hành vi của saga khi nhận một event được định nghĩa bên trong các khối `Initially` và `During`.

### `Initially`
* **Mục đích:** Định nghĩa các hành vi cho những event có thể **bắt đầu một saga mới**.
* Chỉ các event được khai báo trong khối `Initially` mới có thể tạo ra một instance saga.

### `During`
* **Mục đích:** Định nghĩa các hành vi cho những event xảy ra khi saga đã **ở một trạng thái nào đó** (không phải trạng thái `Initial` hoặc `Final`).

```csharp
public OrderStateMachine()
{
    // Khối hành vi khởi tạo
    Initially(
        When(OrderSubmittedEvent)
            .TransitionTo(Submitted)
    );
    
    // Khối hành vi cho các trạng thái đang hoạt động
    During(Submitted, Accepted,
        When(OrderCancelledEvent)
            .TransitionTo(Cancelled)
    );
}
```
* Trong ví dụ trên, `OrderSubmittedEvent` có thể tạo saga mới, nhưng `OrderCancelledEvent` thì không. Nếu một message `OrderCancelled` đến mà không có saga instance nào tồn tại, message đó sẽ bị loại bỏ.