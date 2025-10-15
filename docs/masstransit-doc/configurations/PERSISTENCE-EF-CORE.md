# Tổng quan về Persistence với Entity Framework trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõa về cách sử dụng **Entity Framework Core (EF Core)** để lưu trữ dữ liệu cho MassTransit, dựa trên documentation chính thức. EF Core chủ yếu được dùng cho hai mục đích: **Saga Persistence** và **Transactional Outbox**.

---

## 1. Cài đặt và Yêu cầu

Trước tiên, bạn cần cài đặt gói NuGet cần thiết:
```powershell
Install-Package MassTransit.EntityFrameworkCore
```
Bạn cũng cần có provider EF Core tương ứng cho database của mình (`Microsoft.EntityFrameworkCore.SqlServer` hoặc `Npgsql.EntityFrameworkCore.PostgreSQL`).

---

## 2. Thiết lập DbContext

MassTransit cần bạn thêm các model của nó vào `DbContext` của ứng dụng.

### A. Thêm các Model cần thiết
Trong phương thức `OnModelCreating` của `DbContext`, bạn cần gọi các phương thức mở rộng để MassTransit tự động thêm các entity của nó.

```csharp
using Microsoft.EntityFrameworkCore;
using MassTransit;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
    
    // DbSet cho Saga State của bạn
    public DbSet<OrderState> OrderStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Thêm các entity cho Transactional Outbox
        modelBuilder.AddMassTransitOutboxEntities();
    }
}
```
* **Lưu ý:** Đối với Saga, bạn sẽ dùng `SagaClassMap` để định nghĩa mapping, MassTransit sẽ tự động thêm nó vào `DbContext` khi được cấu hình đúng cách (xem lại phần Saga State).

### B. Database Migrations
Sau khi cập nhật `DbContext`, bạn cần tạo và áp dụng migrations để tạo các bảng cần thiết trong database.
```powershell
dotnet ef migrations add AddMassTransitSagaAndOutbox
dotnet ef database update
```
Các bảng sẽ được tạo ra bao gồm: `InboxState`, `OutboxState`, `OutboxMessage`, và bảng cho saga state của bạn (ví dụ: `OrderState`).

---

## 3. Cấu hình Saga Repository

Đây là cách bạn chỉ cho MassTransit dùng EF Core để lưu trữ trạng thái Saga.

```csharp
// Trong AddMassTransit
x.AddSagaStateMachine<OrderStateMachine, OrderState>()
    .EntityFrameworkRepository(r =>
    {
        // 1. Đăng ký DbContext với repository
        r.AddDbContext<DbContext, AppDbContext>();

        // 2. Chọn chế độ xử lý đồng thời (Concurrency Mode)
        // Pessimistic (khóa bi quan) là an toàn nhất và được khuyến nghị
        r.ConcurrencyMode = ConcurrencyMode.Pessimistic;

        // 3. Chỉ định Lock Statement Provider cho database của bạn
        // Dùng cho chế độ Pessimistic
        
        // Nếu bạn dùng SQL Server:
        r.LockStatementProvider = new SqlServerLockStatementProvider();
        
        // Nếu bạn dùng PostgreSQL:
        // r.LockStatementProvider = new PostgresLockStatementProvider();
    });
```
* **Pessimistic (Khóa bi quan):** MassTransit sẽ yêu cầu database khóa dòng dữ liệu của saga instance khi bắt đầu xử lý, ngăn chặn các xung đột (race condition).
* **Optimistic (Khóa lạc quan):** Dựa vào một cột `Version` để phát hiện xung đột. Nó có thể gây ra `DbUpdateConcurrencyException` và yêu cầu bạn phải cấu hình retry policy.

---

## 4. Cấu hình Transactional Outbox

Để kích hoạt Outbox với EF Core, bạn thêm middleware `UseEntityFrameworkOutbox` vào receive endpoint.

```csharp
cfg.ReceiveEndpoint("order-queue", e =>
{
    // Sử dụng cùng một DbContext đã đăng ký ở trên
    e.UseEntityFrameworkOutbox<AppDbContext>(context);

    e.ConfigureConsumer<SubmitOrderConsumer>(context);
});
```
Bằng cách này, các message được gửi/publish từ bên trong `SubmitOrderConsumer` sẽ được lưu vào các bảng outbox trong `AppDbContext` và chỉ được gửi đi khi `_dbContext.SaveChangesAsync()` thành công.

---

## 5. Đăng ký DbContext trong `Program.cs`

Cuối cùng, đừng quên đăng ký `DbContext` của bạn vào DI container như một service thông thường.

```csharp
// Trong Program.cs
var connectionString = builder.Configuration.GetConnectionString("Default");

// Đăng ký AppDbContext
// Dành cho SQL Server:
builder.Services.AddDbContext<AppDbContext>(x => x.UseSqlServer(connectionString));
// Dành cho PostgreSQL:
// builder.Services.AddDbContext<AppDbContext>(x => x.UseNpgsql(connectionString));

// Cấu hình MassTransit (như các ví dụ ở trên)
builder.Services.AddMassTransit(...);
```
Bằng cách thiết lập như vậy, bạn đã tạo ra một hệ thống messaging mạnh mẽ, nhất quán và đáng tin cậy, tận dụng transaction của database để đảm bảo không mất dữ liệu.