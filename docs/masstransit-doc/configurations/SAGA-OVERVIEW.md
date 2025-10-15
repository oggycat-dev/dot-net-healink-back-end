# Tổng quan về Cấu hình Saga trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về cách **cấu hình Saga** (bao gồm cả State Machine), dựa trên documentation chính thức.

---

## 1. Các thành phần cần cấu hình

Việc cấu hình một Saga bao gồm hai phần chính:
1.  **Đăng ký Saga (Saga / State Machine):** Đăng ký class Saga hoặc State Machine của bạn với Dependency Injection (DI).
2.  **Cấu hình Saga Repository:** Chỉ định nơi MassTransit sẽ lưu trữ và truy xuất các trạng thái (instances) của Saga.

---

## 2. Đăng ký Saga

Giống như Consumer, bạn cần đăng ký Saga với container DI để MassTransit biết về sự tồn tại của nó.

```csharp
// Trong Program.cs
builder.Services.AddMassTransit(x =>
{
    // Đăng ký một Saga State Machine
    // TStateMachine là class State Machine của bạn (kế thừa từ MassTransitStateMachine<TState>)
    // TState là class chứa dữ liệu trạng thái của Saga (kế thừa từ SagaStateMachineInstance)
    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r => 
        {
            // Cấu hình repository ngay khi đăng ký
            r.AddDbContext<DbContext, OrderStateDbContext>();
        });
});
```
* **Lưu ý:** `AddSagaStateMachine` sẽ tự động đăng ký cả `SagaDefinition` tương ứng nếu có.

---

## 3. Cấu hình Saga Repository 💾

**Saga Repository** là thành phần chịu trách nhiệm **lưu trữ và tải (persist and load)** các instance của Saga. MassTransit hỗ trợ nhiều loại repository khác nhau.

### A. In-Memory (Trong bộ nhớ)
* **Mục đích:** Chủ yếu dùng cho **testing** hoặc các kịch bản đơn giản không cần lưu trữ bền vững.
* **Cách cấu hình:**
    ```csharp
    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .InMemoryRepository();
    ```

### B. Entity Framework
* **Mục đích:** Lưu trữ trạng thái Saga vào một cơ sở dữ liệu quan hệ (SQL Server, PostgreSQL, v.v.) thông qua Entity Framework Core.
* **Cách cấu hình:** Cần có `MassTransit.EntityFrameworkCore`.
    ```csharp
    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Pessimistic; // Hoặc Optimistic
            r.AddDbContext<DbContext, YourSagaDbContext>();
        });
    ```

### C. Các loại Repository khác
MassTransit còn hỗ trợ nhiều loại repository khác thông qua các gói NuGet riêng, ví dụ:
* **MongoDb**
* **Dapper**
* **Redis**
* **Azure Table Storage**

---

## 4. Cấu hình Endpoint cho Saga

Giống như Consumer, Saga cũng nhận message từ một receive endpoint (queue).

### A. Cấu hình tự động với `ConfigureEndpoints` 🪄
Đây là cách đơn giản nhất. `cfg.ConfigureEndpoints(context)` sẽ tự động tạo một endpoint cho Saga của bạn.
* **Tên Endpoint mặc định:** Được suy ra từ tên class `TState` theo quy tắc kebab-case. Ví dụ: `OrderState` ➡️ `order-state`.
* **Tự động áp dụng Definition:** Các cấu hình trong `SagaDefinition` sẽ được áp dụng.

### B. Cấu hình thủ công với `ReceiveEndpoint`
Cho phép bạn kiểm soát hoàn toàn endpoint, ví dụ như gộp chung xử lý của Saga và Consumer trên cùng một queue.

```csharp
cfg.ReceiveEndpoint("order-processing", e =>
{
    // Cấu hình Saga trên endpoint này
    e.ConfigureSaga<OrderState>(context);
    
    // Bạn cũng có thể cấu hình thêm consumer trên CÙNG endpoint này
    e.ConfigureConsumer<SubmitOrderConsumer>(context);
});
```

---

## 5. Sử dụng Saga Definition

`SagaDefinition` là nơi tập trung hóa các cấu hình cho Saga, giúp code sạch sẽ và nhất quán.

```csharp
public class OrderStateMachineDefinition :
    SagaDefinition<OrderState>
{
    public OrderStateMachineDefinition()
    {
        // Ghi đè tên endpoint mặc định
        EndpointName = "orders";
    }

    protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator, 
        ISagaConfigurator<OrderState> sagaConfigurator,
        IRegistrationContext context)
    {
        // Áp dụng cấu hình cho TẤT CẢ endpoint host saga này
        endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 1000, 5000));
        endpointConfigurator.UseInMemoryOutbox(context);
    }
}
```
Khi bạn gọi `cfg.ConfigureEndpoints(context)`, các `Definition` này sẽ được tự động tìm và áp dụng.