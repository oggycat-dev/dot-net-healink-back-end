# Tổng quan về Job Consumers trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **Job Consumer**, một pattern nâng cao trong MassTransit được thiết kế để xử lý các tác vụ **chạy dài (long-running)** và tốn nhiều tài nguyên, dựa trên documentation chính thức.

---

## 1. Vấn đề cần giải quyết ⏳

Consumer thông thường có một số hạn chế khi xử lý các tác vụ kéo dài (ví dụ: xử lý video, tạo báo cáo lớn):

1.  **Message Lock Timeout:** Một message được khóa trên queue khi consumer nhận nó. Nếu consumer xử lý quá lâu (ví dụ > 30 phút), khóa này sẽ hết hạn. Broker sẽ cho rằng consumer đã chết và **giao lại message cho một consumer khác**, dẫn đến việc xử lý bị trùng lặp.
2.  **Quản lý tài nguyên:** Các tác vụ nặng (như encode video) tiêu tốn nhiều CPU/RAM. Bạn có thể muốn giới hạn chỉ chạy **một vài tác vụ** như vậy cùng lúc trên một server, ngay cả khi bạn có nhiều instance consumer.

**Job Consumer** được tạo ra để giải quyết chính xác các vấn đề này.

---

## 2. Kiến trúc của Job Consumer

Job Consumer hoạt động dựa trên một thành phần trung tâm gọi là **Job Service**.



1.  **Client (Người gửi việc):** Thay vì `Send` hay `Publish` trực tiếp, client sẽ gửi một command `SubmitJob<TJob>` đến Job Service.
2.  **Job Service (Quản lý công việc):**
    * Nhận yêu cầu, tạo một `JobId` duy nhất và lưu trạng thái công việc.
    * Nó chịu trách nhiệm "giữ khóa" với broker, liên tục làm mới để message gốc không bị timeout.
    * Sau đó, nó bắt đầu công việc bằng cách gửi message `StartJob<TJob>` đến một worker.
3.  **Job Consumer (Người làm việc):**
    * Là một consumer đặc biệt, nhận message `StartJob`.
    * Bắt đầu thực hiện tác vụ nặng. Trong quá trình này, nó có thể báo cáo tiến trình về cho Job Service.
    * Khi hoàn thành, nó báo lại cho Job Service.
4.  **Job Service:** Cập nhật trạng thái công việc thành `Completed` (hoặc `Faulted` nếu có lỗi) và publish các sự kiện tương ứng.

---

## 3. Cách tạo một Job Consumer

Một Job Consumer là một class implement `IJobConsumer<TJob>`.

```csharp
// TJob là message chứa dữ liệu của công việc
public record ConvertVideo
{
    public string VideoPath { get; init; }
}

public class ConvertVideoJobConsumer : IJobConsumer<ConvertVideo>
{
    // Thay vì Consume, nó là Run
    public async Task Run(JobContext<ConvertVideo> context)
    {
        var videoPath = context.Job.VideoPath;
        
        // Bắt đầu công việc nặng...
        Console.WriteLine($"Bắt đầu chuyển mã video: {videoPath}");
        
        // Giả lập công việc kéo dài
        for (var i = 0; i < 100; i++)
        {
            await Task.Delay(1000);
            // Có thể báo cáo tiến trình nếu muốn
        }
        
        Console.WriteLine($"Hoàn thành chuyển mã video: {videoPath}");
    }
}
```
* **`JobContext<TJob>`:** Cung cấp thông tin về công việc, bao gồm `JobId`, dữ liệu (`context.Job`), và `CancellationToken`.

---

## 4. Cách cấu hình

Việc cấu hình bao gồm thiết lập Job Service và Job Consumer.

```csharp
// Trong AddMassTransit
services.AddMassTransit(x => 
{
    // Đăng ký Job Consumer
    x.AddConsumer<ConvertVideoJobConsumer>();
    
    x.UsingRabbitMq((context, cfg) => 
    {
        // 1. Cấu hình endpoint cho Job Service để nó quản lý trạng thái
        // Endpoint này sẽ tự động xử lý các message SubmitJob, CancelJob...
        cfg.ReceiveEndpoint("job-service", e => 
        {
            e.ConfigureJobServiceEndpoints();
        });
        
        // 2. Cấu hình endpoint cho các worker
        cfg.ReceiveEndpoint("video-conversion", e =>
        {
            // Giới hạn chỉ chạy 2 job cùng lúc trên endpoint này
            e.UseConcurrencyLimit(2);
            
            // Kết nối Job Consumer
            e.ConfigureConsumer<ConvertVideoJobConsumer>(context, c => 
            {
                // Cấu hình Job Options, ví dụ thời gian timeout của job
                c.Options<JobOptions<ConvertVideo>>(options => options.SetJobTimeout(TimeSpan.FromHours(1)));
            });
        });
    });
});
```

---

## 5. Gửi và Theo dõi Job

Client sử dụng `IJobSubmissionClient` để gửi job và `IJobConsumerClient` để theo dõi.

```csharp
// Gửi một job
var client = provider.GetRequiredService<IJobSubmissionClient>();
JobSubmissionResult<ConvertVideo> result = await client.SubmitJob(new ConvertVideo { VideoPath = "..." });

Console.WriteLine($"Job Submitted: {result.JobId}");

// Theo dõi trạng thái
// Các service khác có thể subscribe vào các event như JobSubmitted, JobStarted, JobCompleted, JobFaulted
// để theo dõi tiến trình của job một cách bất đồng bộ.
```