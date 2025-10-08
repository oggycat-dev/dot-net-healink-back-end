# Tổng quan về Consumer Sagas trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **Consumer Saga**, một cách tiếp cận khác để triển khai Saga Pattern trong MassTransit mà không cần dùng State Machine, dựa trên documentation chính thức.

---

## 1. Consumer Saga là gì?

**Consumer Saga** là một cách triển khai Saga Pattern sử dụng một **class consumer thông thường**. Thay vì định nghĩa một State Machine phức tạp, bạn implement các interface đặc biệt để chỉ cho MassTransit biết cách điều phối một quy trình nghiệp vụ.

Đây là một cách tiếp cận đơn giản hơn, phù hợp cho các workflow không quá phức tạp và có luồng xử lý tương đối tuyến tính.

---

## 2. So sánh Consumer Saga và State Machine Saga

| Tiêu chí | Consumer Saga | State Machine Saga |
| :--- | :--- | :--- |
| **Cách triển khai** | Một class kế thừa từ `IConsumer` | Một class kế thừa từ `MassTransitStateMachine` |
| **Quản lý trạng thái** | Trạng thái **ẩn (implicit)**, thường được truyền qua lại giữa các message | Trạng thái **tường minh (explicit)**, được lưu trong một class `TState` riêng |
| **Luồng nghiệp vụ** | Phân tán hơn, giống Choreography | Tập trung, rõ ràng, giống Orchestration |
| **Độ phức tạp** | Đơn giản hơn | Phức tạp hơn, nhưng mạnh mẽ hơn |
| **Phù hợp cho** | Các workflow đơn giản, tuyến tính | Các workflow phức tạp, có nhiều nhánh, nhiều trạng thái |

---

## 3. Các Interface chính

Để tạo một Consumer Saga, bạn tạo một class và implement các interface sau:

### `ISaga`
Đây là interface cơ sở, đánh dấu class này là một saga. Nó yêu cầu một thuộc tính `CorrelationId`.

### `InitiatedBy<TMessage>`
* **Ý nghĩa:** Đánh dấu message `TMessage` là message có thể **khởi tạo** một instance saga mới.
* **Yêu cầu:** Class saga phải implement `IConsumer<TMessage>`.

### `Orchestrates<TMessage>`
* **Ý nghĩa:** Đánh dấu message `TMessage` là message sẽ **tương tác với một saga instance đã tồn tại**.
* **Yêu cầu:**
    * Class saga phải implement `IConsumer<TMessage>`.
    * Message `TMessage` phải implement `CorrelatedBy<Guid>` để MassTransit biết cách tìm saga instance tương ứng.

### `Observes<TMessage>`
* **Ý nghĩa:** Cho phép saga "quan sát" một message `TMessage` và thực hiện một hành động phụ, nhưng không làm thay đổi trạng thái chính của quy trình.
* **Yêu cầu:** Class saga phải implement `IConsumer<TMessage>`.

---

## 4. Ví dụ về một Consumer Saga

Hãy xem một ví dụ về quy trình fulfillment đơn hàng.

**A. Định nghĩa các Message Contracts:**

```csharp
// Message khởi tạo saga
public record SubmitOrder : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; init; }
    public string CustomerNumber { get; init; }
}

// Message tiếp theo trong chuỗi
public record OrderAccepted : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; init; }
}
```

**B. Triển khai Consumer Saga:**

```csharp
public class FulfillOrderSaga :
    ISaga, // Đánh dấu là saga
    InitiatedBy<SubmitOrder>, // Bắt đầu bởi SubmitOrder
    Orchestrates<OrderAccepted> // Tiếp tục bởi OrderAccepted
{
    public Guid CorrelationId { get; set; }
    public string CustomerNumber { get; set; }

    // Xử lý message khởi tạo
    public Task Consume(ConsumeContext<SubmitOrder> context)
    {
        CustomerNumber = context.Message.CustomerNumber;
        Console.WriteLine($"Order submitted for customer: {CustomerNumber}");
        
        // Gửi command tiếp theo...
        return Task.CompletedTask;
    }
    
    // Xử lý message tiếp theo
    public Task Consume(ConsumeContext<OrderAccepted> context)
    {
        Console.WriteLine($"Order accepted for customer: {CustomerNumber}");
        
        // Đánh dấu saga là hoàn thành
        IsCompleted = true; 
        
        return Task.CompletedTask;
    }

    // Thuộc tính để cho MassTransit biết khi nào cần xóa instance saga
    [JsonIgnore]
    public bool IsCompleted { get; private set; }
}
```
* **Lưu ý:** Bạn cần tự quản lý việc hoàn thành saga bằng cách đặt một thuộc tính (ví dụ `IsCompleted`) và đánh dấu nó để saga instance có thể được dọn dẹp.

---

## 5. Cấu hình

Việc cấu hình một Consumer Saga tương tự như một saga thông thường.

```csharp
// Đăng ký saga
services.AddMassTransit(x => 
{
    x.AddSaga<FulfillOrderSaga>()
        .InMemoryRepository(); // Hoặc EF Core repository

    // ... cấu hình transport và endpoint
});
```
MassTransit sẽ tự động hiểu các interface `InitiatedBy`, `Orchestrates` và cấu hình correlation tương ứng.