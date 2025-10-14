# Tổng quan về Filters trong MassTransit

Tài liệu này đi sâu vào cách **tạo và sử dụng các Filter tùy chỉnh**, dựa trên documentation chính thức. Filter là các class cụ thể implement logic của middleware, cho phép bạn chèn hành vi vào pipeline xử lý message.

---

## 1. Filter là gì?

Filter là một class implement interface `IFilter<TContext>`, trong đó `TContext` là loại "hoàn cảnh" mà filter sẽ hoạt động. Đây là viên gạch xây dựng nên pipeline.



Mỗi filter có một phương thức `Send` duy nhất, nhận vào 2 tham số:
* `context`: Chứa message và tất cả metadata liên quan.
* `next`: Đại diện cho phần còn lại của pipeline.

Bằng cách gọi `await next.Send(context)`, bạn cho phép message tiếp tục đi đến filter tiếp theo.

---

## 2. Cách tạo một Filter tùy chỉnh

Để tạo một filter, bạn cần tạo một class implement `IFilter<TContext>`.

**Ví dụ: Một Filter để thiết lập `TenantId` vào header cho mọi message được gửi đi.**

```csharp
using MassTransit.Context;
using MassTransit.Middleware;

// Filter này hoạt động trên SendContext<T>, tức là khi message được gửi đi
public class TenantSendFilter<T> : IFilter<SendContext<T>> where T : class
{
    private readonly ITenantProvider _tenantProvider;

    // Filter có thể được inject các dependency khác
    public TenantSendFilter(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public async Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        // 1. Hành động TRƯỚC khi message được gửi đi
        var tenantId = _tenantProvider.GetTenantId();
        context.Headers.Set("TenantId", tenantId);

        Console.WriteLine($"[Filter] Set TenantId: {tenantId} for message {context.MessageId}");

        // 2. Chuyển message đến bước tiếp theo trong pipeline
        await next.Send(context);

        // 3. Hành động SAU KHI message đã được gửi xong (ít phổ biến hơn)
    }
    
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("tenant-send-filter");
    }
}
```

---

## 3. Cách thêm Filter vào Pipeline

Bạn sử dụng phương thức `UseFilter` ở các cấp độ cấu hình khác nhau.

### A. Thêm một Instance Filter
Cách này đơn giản nếu filter của bạn không có dependency.

```csharp
cfg.ReceiveEndpoint("my-queue", e => 
{
    e.UseFilter(new MySimpleLoggingFilter());
});
```

### B. Thêm Filter từ Dependency Injection
Đây là cách làm phổ biến và mạnh mẽ nhất, cho phép filter của bạn được inject các service khác.

**1. Đăng ký Filter vào DI Container:**
```csharp
// Trong Program.cs
services.AddScoped(typeof(TenantSendFilter<>));
services.AddScoped<ITenantProvider, HttpTenantProvider>();
```
*Lưu ý: Đăng ký filter dưới dạng generic `typeof(TenantSendFilter<>)`.*

**2. Thêm Filter vào Pipeline bằng `UseFilter`:**
```csharp
// Trong cấu hình bus
cfg.UseSendFilter(typeof(TenantSendFilter<>), context);

// Hoặc trong một endpoint
e.UseFilter(typeof(MyConsumerFilter<>), context);
```
* `context` (`IRegistrationContext`) sẽ được dùng để tự động giải quyết (resolve) instance của filter từ DI container cho mỗi message.

---

## 4. `IFilterObserver`: Quan sát Pipeline

`IFilterObserver` là một cơ chế nâng cao cho phép bạn "quan sát" các hành động của pipeline mà không cần phải là một phần của nó. Nó hữu ích cho các mục đích chẩn đoán hoặc logging sâu.

Bạn có thể `Connect` một observer vào một pipeline cụ thể. Observer sẽ được thông báo trước (`PreSend`) và sau (`PostSend`) khi mỗi filter trong pipeline đó được thực thi.

```csharp
// Một Observer đơn giản
public class MyFilterObserver : IFilterObserver
{
    public void PreSend<T>(T context) where T : class, PipeContext { /* ... */ }
    public void PostSend<T>(T context) where T : class, PipeContext { /* ... */ }
    public void SendFault<T>(T context, Exception exception) where T : class, PipeContext { /* ... */ }
}

// Kết nối observer vào pipeline
e.ConnectFilterObserver(new MyFilterObserver());
```