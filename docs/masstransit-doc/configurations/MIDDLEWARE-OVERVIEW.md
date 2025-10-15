# Tổng quan về Middleware trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **Middleware** và **Pipeline** trong MassTransit, dựa trên documentation chính thức. Middleware cho phép bạn chèn các hành vi tùy chỉnh vào quá trình xử lý message.

---

## 1. Pipeline: Dây chuyền xử lý Message 🧩

Hãy tưởng tượng mỗi message khi được nhận và xử lý sẽ đi qua một **dây chuyền lắp ráp (pipeline)**. Trên dây chuyền này có nhiều "trạm" (gọi là **filter** hoặc **middleware**), mỗi trạm thực hiện một công việc cụ thể trước khi chuyển message đến trạm tiếp theo.



Ví dụ về các trạm trên dây chuyền:
1.  **Trạm Retry:** Cố gắng xử lý lại nếu có lỗi.
2.  **Trạm Outbox:** Đảm bảo message chỉ được gửi đi khi transaction thành công.
3.  **Trạm Deserialize:** Giải mã message từ JSON sang object .NET.
4.  **Trạm Consumer:** Trạm cuối cùng, nơi logic nghiệp vụ của bạn được thực thi.

MassTransit cho phép bạn thêm các "trạm" tùy chỉnh của riêng mình vào dây chuyền này.

---

## 2. Các loại Pipeline

MassTransit không chỉ có một mà có nhiều pipeline lồng nhau, mỗi pipeline có một phạm vi ảnh hưởng khác nhau:

1.  **`Consume` (Toàn cục):** Áp dụng cho mọi message đi qua bus.
2.  **`ReceiveEndpoint`:** Áp dụng cho mọi message đến một endpoint cụ thể.
3.  **`Consumer`:** Áp dụng cho một loại consumer cụ thể.
4.  **`Saga`:** Áp dụng cho một loại saga cụ thể.
5.  **`Message`:** Áp dụng cho một loại message cụ thể.

Pipeline được thực thi từ ngoài vào trong (từ toàn cục đến cụ thể).

---

## 3. Cách thêm Middleware (Filter)

Bạn sử dụng phương thức `UseFilter` ở các cấp độ cấu hình khác nhau để thêm middleware vào pipeline tương ứng.

```csharp
services.AddMassTransit(x => 
{
    x.UsingRabbitMq((context, cfg) => 
    {
        // 1. Filter ở cấp độ toàn cục (bus)
        cfg.UseFilter(new GlobalLoggingFilter());
        
        cfg.ReceiveEndpoint("my-queue", e => 
        {
            // 2. Filter ở cấp độ Endpoint
            e.UseFilter(new EndpointSpecificFilter());
            
            e.ConfigureConsumer<MyConsumer>(context, consumerCfg => 
            {
                // 3. Filter ở cấp độ Consumer
                consumerCfg.UseFilter(new ConsumerSpecificFilter<MyConsumer>());
            });
        });
    });
});
```

---

## 4. Cách tạo một Filter tùy chỉnh

Một filter là một class implement interface `IFilter<TContext>`, với `TContext` là loại context mà filter đó hoạt động (ví dụ: `ConsumeContext`, `SendContext`).

```csharp
// Một filter để đo thời gian xử lý của một message
public class ProcessingTimeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Chuyển message đến trạm tiếp theo trong pipeline
            await next.Send(context);
        }
        finally
        {
            stopwatch.Stop();
            // Log thời gian xử lý
            Log.Information("Processed {Message} in {Elapsed}", typeof(T).Name, stopwatch.Elapsed);
        }
    }
    
    public void Probe(ProbeContext context) 
    {
        // Dùng để chẩn đoán, không bắt buộc implement
    }
}
```
* **`await next.Send(context);`** là dòng lệnh quan trọng nhất. Nó quyết định khi nào sẽ chuyển quyền kiểm soát cho middleware tiếp theo. Code bạn viết trước dòng này sẽ chạy trước, và code sau dòng này sẽ chạy sau.

---

## 5. Các Middleware tích hợp sẵn

MassTransit cung cấp nhiều middleware mạnh mẽ mà bạn có thể sử dụng ngay:

* **`UseMessageRetry`:** Tự động thử lại message khi có lỗi.
* **`UseInMemoryOutbox` / `UseEntityFrameworkOutbox`:** Triển khai Outbox pattern để đảm bảo tính nhất quán.
* **`UseRateLimiter`:** Giới hạn tốc độ xử lý message (ví dụ: không quá 100 message/giây).
* **`UseCircuitBreaker`:** Tự động ngắt kết nối đến một service đang bị lỗi để tránh làm sập hệ thống.
* **`UseTransaction`:** Bọc quá trình xử lý trong một `TransactionScope`.
* **`UseScheduledRedelivery`:** Thử lại message với độ trễ được quản lý bởi một scheduler.