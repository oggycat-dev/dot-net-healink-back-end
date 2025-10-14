# Tổng quan về Đăng ký Saga (Saga Registration)

Tài liệu này đi sâu vào cách **đăng ký** một Saga với container Dependency Injection (DI), dựa trên documentation chính thức. Đây là bước đầu tiên và bắt buộc trước khi bạn có thể cấu hình Saga trên một endpoint.

---

## 1. Tại sao phải Đăng ký?

Việc đăng ký sẽ "thông báo" cho MassTransit và DI container biết về sự tồn tại của Saga, State Machine, và Repository của bạn. Khi bạn gọi `ConfigureEndpoints`, MassTransit sẽ sử dụng thông tin đã đăng ký này để tự động cấu hình các endpoint cần thiết.

---

## 2. Các phương thức Đăng ký

Việc đăng ký được thực hiện bên trong `services.AddMassTransit(x => { ... })`.

### A. Đăng ký một Saga duy nhất
Sử dụng `AddSaga` hoặc `AddSagaStateMachine`.

* **Với Saga State Machine (phổ biến nhất):**
  ```csharp
  x.AddSagaStateMachine<OrderStateMachine, OrderState>();
  ```
* **Với Saga thông thường (kế thừa từ `ISaga`):**
  ```csharp
  x.AddSaga<OrderSaga>();
  ```

### B. Đăng ký Saga cùng với Repository
Đây là cách làm được khuyến nghị, giúp gắn liền Saga với cơ chế lưu trữ của nó. Bạn có thể nối chuỗi (chain) phương thức cấu hình repository ngay sau khi đăng ký.

```csharp
// Đăng ký Saga và chỉ định dùng In-Memory Repository
x.AddSagaStateMachine<OrderStateMachine, OrderState>()
    .InMemoryRepository();

// Đăng ký Saga và chỉ định dùng Entity Framework Repository
x.AddSagaStateMachine<OrderStateMachine, OrderState>()
    .EntityFrameworkRepository(r => 
    {
        r.AddDbContext<DbContext, OrderStateDbContext>();
    });
```

### C. Quét và Đăng ký tự động (Assembly Scanning)
Để tránh phải đăng ký từng Saga một, bạn có thể yêu cầu MassTransit quét toàn bộ một assembly để tìm và đăng ký tất cả các Saga có trong đó.

```csharp
// Tự động tìm và đăng ký tất cả các Saga trong assembly chứa class Program
x.AddSagas(typeof(Program).Assembly);
```
* **Lưu ý:** Khi dùng assembly scanning, bạn cần cấu hình repository một cách riêng biệt bằng `SetSagaRepositoryProvider`.

---

## 3. Cấu hình Repository riêng biệt

Khi bạn dùng assembly scanning, bạn không thể nối chuỗi cấu hình repository. Thay vào đó, bạn phải cấu hình nó cho tất cả các Saga đã được quét.

```csharp
services.AddMassTransit(x => 
{
    // 1. Quét và đăng ký tất cả Sagas
    x.AddSagas(typeof(Program).Assembly);

    // 2. Cấu hình repository mặc định cho TẤT CẢ các saga đã đăng ký ở trên
    x.SetInMemorySagaRepositoryProvider();

    // Hoặc dùng Entity Framework
    x.SetEntityFrameworkSagaRepositoryProvider(r => 
    {
        r.ExistingDbContext<OrderStateDbContext>();
        // ...
    });

    // ... Cấu hình bus
});
```
---

## 4. Những gì xảy ra khi Đăng ký?

Khi bạn gọi `AddSagaStateMachine<T, TInstance>`, MassTransit sẽ thêm các dịch vụ sau vào DI container:
* `TStateMachine` (class State Machine).
* `ISagaRepository<TInstance>` (repository đã được cấu hình).
* `SagaDefinition<TInstance>` (class definition để cấu hình mặc định).

Điều này đảm bảo rằng khi `ConfigureEndpoints` được gọi, MassTransit có đủ thông tin để tạo và cấu hình endpoint cho Saga một cách chính xác.