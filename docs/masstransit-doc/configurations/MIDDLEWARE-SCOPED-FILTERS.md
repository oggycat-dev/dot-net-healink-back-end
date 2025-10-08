# Tổng quan về Scoped Filters trong MassTransit

Tài liệu này đi sâu vào **Scoped Filters**, một loại filter đặc biệt được tích hợp chặt chẽ với Dependency Injection (DI) container để quản lý các service có vòng đời scoped, dựa trên documentation chính thức.

---

## 1. Scoped Filter là gì? 📦

Trong DI, một service có vòng đời **scoped** nghĩa là một instance duy nhất của service đó sẽ được tạo ra và tái sử dụng trong một "scope" (phạm vi) nhất định, thường là một request HTTP.

**Scoped Filter** là một cơ chế của MassTransit cho phép bạn tạo ra một DI scope mới cho mỗi message được xử lý. Tất cả các filter và consumer bên trong scope đó sẽ chia sẻ cùng một instance của các service scoped.



**Trường hợp sử dụng phổ biến nhất:** Quản lý `DbContext` của Entity Framework. Bạn muốn đảm bảo rằng filter và consumer cùng sử dụng chung một instance `DbContext` và `DbConnection` trong cùng một transaction.

---

## 2. `UseScopedFilter` so với `UseFilter`

* **`UseFilter`:** Dùng cho các filter có vòng đời **Singleton**. Filter được tạo một lần và tái sử dụng. Nó không thể inject các service có vòng đời scoped.
* **`UseScopedFilter`:** Dùng cho các filter có vòng đời **Scoped**. Một instance filter mới sẽ được tạo ra cho mỗi message, cùng với một DI scope mới. Nó có thể inject các service scoped một cách an toàn.

---

## 3. Cách tạo và sử dụng Scoped Filter

### A. Tạo một Filter với Scoped Dependencies
Tạo một filter và inject các service scoped (ví dụ: `AppDbContext`) vào hàm khởi tạo của nó.

```csharp
// Filter này sẽ quản lý một transaction cho mỗi message
public class TransactionFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly AppDbContext _dbContext;

    // Inject DbContext (được đăng ký là scoped)
    public TransactionFilter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        // Bắt đầu một transaction
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            // Chuyển message đến consumer
            await next.Send(context);
            
            // Nếu không có lỗi, commit transaction
            await transaction.CommitAsync(context.CancellationToken);
        }
        catch (Exception)
        {
            // Nếu có lỗi, rollback transaction
            await transaction.RollbackAsync(context.CancellationToken);
            throw;
        }
    }
    
    // ...
}
```

### B. Đăng ký Filter và Dependencies vào DI
Đăng ký `DbContext` và filter của bạn với vòng đời scoped.
```csharp
// Trong Program.cs
services.AddDbContext<AppDbContext>(...); // DbContext mặc định là scoped
services.AddScoped(typeof(TransactionFilter<>));
```

### C. Áp dụng Filter vào Pipeline
Sử dụng phương thức `UseScopedFilter` trong cấu hình endpoint.

```csharp
cfg.ReceiveEndpoint("my-queue", e =>
{
    // Yêu cầu MassTransit tạo một scope mới và resolve TransactionFilter từ đó
    e.UseScopedFilter<TransactionFilter>(context);

    e.ConfigureConsumer<MyConsumer>(context);
});
```
---

## 4. `ScopedConsumeContext` và chia sẻ Dependencies

Khi bạn dùng `UseScopedFilter`, consumer của bạn (nếu được cấu hình trên cùng endpoint) sẽ chạy **bên trong cùng một DI scope** với filter.

Điều này có nghĩa là khi `MyConsumer` inject `AppDbContext`, nó sẽ nhận được **chính xác cùng một instance** mà `TransactionFilter` đang sử dụng.

```csharp
public class MyConsumer : IConsumer<MyMessage>
{
    private readonly AppDbContext _dbContext;

    // Nhận được CÙNG một instance DbContext với TransactionFilter
    public MyConsumer(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<MyMessage> context)
    {
        // Mọi thay đổi trên _dbContext.Users...
        var user = new User(...);
        _dbContext.Users.Add(user);
        
        // Sẽ được commit hoặc rollback bởi TransactionFilter
        await _dbContext.SaveChangesAsync();
    }
}
```
Cơ chế này giúp triển khai **Unit of Work pattern** một cách cực kỳ gọn gàng và hiệu quả, đảm bảo tính toàn vẹn dữ liệu cho mỗi message được xử lý.