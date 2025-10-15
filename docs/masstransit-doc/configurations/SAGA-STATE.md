# Tổng quan về Cấu hình Trạng thái Saga (Saga State)

Tài liệu này đi sâu vào cách định nghĩa và cấu hình **lớp trạng thái (State)** cho một Saga State Machine, dựa trên documentation chính thức. Lớp trạng thái là nơi lưu trữ tất cả dữ liệu của một instance saga.

---

## 1. Định nghĩa Lớp Trạng thái

Mỗi Saga State Machine cần một class để lưu trữ trạng thái của nó. Class này phải implement interface `SagaStateMachineInstance`.

### `SagaStateMachineInstance`
Interface này yêu cầu một thuộc tính duy nhất:
* **`CorrelationId` (`Guid`):** Đây là khóa chính (primary key) của một instance saga, dùng để định danh duy nhất cho một quy trình nghiệp vụ.

```csharp
using MassTransit;

public class OrderState : SagaStateMachineInstance
{
    // Khóa chính, bắt buộc phải có
    public Guid CorrelationId { get; set; }

    // Tên trạng thái hiện tại của Saga (ví dụ: "Submitted", "Paid")
    public string CurrentState { get; set; }

    // Các thuộc tính khác để lưu trữ dữ liệu của quy trình
    public Guid? OrderId { get; set; }
    public string CustomerNumber { get; set; }
    public DateTime? SubmitDate { get; set; }
    
    // Thuộc tính để lạc quan hóa việc xử lý đồng thời (optimistic concurrency)
    public int Version { get; set; }
}
```

---

## 2. Correlation (Tương quan)

Correlation là cơ chế MassTransit dùng để tìm đúng instance saga khi nhận được một message. Mặc định, MassTransit dùng `CorrelationId`. Tuy nhiên, bạn thường cần tìm saga dựa trên một thuộc tính nghiệp vụ khác, ví dụ như `OrderId`.

### `CorrelatedBy<TKey>`
Để cho phép MassTransit tìm saga bằng một thuộc tính khác, bạn cần cho lớp trạng thái implement thêm interface `CorrelatedBy<TKey>`.

```csharp
// Giả sử message OrderSubmitted có thuộc tính OrderId
public class OrderSubmitted 
{
    public Guid OrderId { get; set; }
}

// Lớp trạng thái implement CorrelatedBy<Guid>
public class OrderState : SagaStateMachineInstance, CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
    
    // Thuộc tính dùng để tương quan, tên phải khớp với interface
    // MassTransit sẽ dùng thuộc tính này để tìm saga khi nhận được message OrderSubmitted
    public Guid Id { get; set; } // Tên 'Id' là mặc định cho CorrelatedBy<T>
}
```
* **Lưu ý:** Việc đặt tên thuộc tính trong `CorrelatedBy` rất quan trọng. Mặc định nó tìm thuộc tính tên là `Id`.

---

## 3. Cấu hình Persistence với Entity Framework 💾

Để lưu trữ trạng thái saga vào database bằng Entity Framework, bạn cần:

### A. Tạo một `SagaDbContext`
Tạo một `DbContext` riêng cho các trạng thái saga của bạn.

```csharp
public class OrderStateDbContext : SagaDbContext
{
    public OrderStateDbContext(DbContextOptions<OrderStateDbContext> options)
        : base(options)
    {
    }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new OrderStateMap(); }
    }
}
```

### B. Tạo một `SagaClassMap`
Đây là bước quan trọng nhất. Bạn cần tạo một class map để chỉ cho Entity Framework cách ánh xạ lớp `OrderState` của bạn vào một bảng trong database.

```csharp
public class OrderStateMap : SagaClassMap<OrderState>
{
    protected override void Configure(EntityTypeBuilder<OrderState> entity, ModelBuilder model)
    {
        // Đặt tên cột cho thuộc tính
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.CustomerNumber);

        // Tạo chỉ mục (index) cho các thuộc tính thường xuyên được truy vấn
        entity.HasIndex(x => x.SubmitDate);
    }
}
```
* `SagaClassMap` đã tự động cấu hình `CorrelationId` làm khóa chính cho bạn.

### C. Đăng ký trong `Program.cs`
Cuối cùng, đăng ký `DbContext` và cấu hình repository cho saga.

```csharp
// Trong AddMassTransit
x.AddSagaStateMachine<OrderStateMachine, OrderState>()
    .EntityFrameworkRepository(r =>
    {
        // Đăng ký DbContext
        r.AddDbContext<DbContext, OrderStateDbContext>((provider, builder) =>
        {
            builder.UseSqlServer(provider.GetRequiredService<IConfiguration>()
                .GetConnectionString("DefaultConnection"));
        });
        
        // Cấu hình các tùy chọn khác nếu cần
        r.LockStatementProvider = new SqlServerLockStatementProvider();
    });
```