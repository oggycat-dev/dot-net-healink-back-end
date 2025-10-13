# Tổng quan về Routing Slips trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **Routing Slip**, một pattern nâng cao trong MassTransit để điều phối một chuỗi các tác vụ (workflow) qua nhiều service khác nhau một cách đáng tin cậy.

---

## 1. Routing Slip Pattern là gì? 🗺️

Hãy tưởng tượng **Routing Slip** như một **lịch trình du lịch** hoặc một **quy trình lắp ráp trên dây chuyền**. Nó định nghĩa một chuỗi các "bước" (gọi là Activities) cần được thực hiện theo một thứ tự cụ thể.

* **Lịch trình (Routing Slip):** Chứa danh sách tất cả các điểm đến (activities).
* **Điểm đến (Activity):** Một trạm xử lý, một service cụ thể thực hiện một công việc.

Message sẽ mang theo "lịch trình" này. Sau khi một trạm xử lý xong, nó sẽ xem lịch trình và gửi message đến trạm tiếp theo.

Pattern này đặc biệt mạnh mẽ vì nó hỗ trợ **bồi thường (compensation)**. Nếu một bước nào đó thất bại, quy trình có thể tự động "quay lui", thực hiện các hành động ngược lại để hủy bỏ các tác vụ đã hoàn thành trước đó.



---

## 2. Các thành phần chính

### Activity (Hoạt động)
**Activity** là một đơn vị công việc độc lập, một bước trong workflow. Nó là một class implement interface `IActivity` hoặc `IExecuteActivity`. Mỗi Activity có hai phương thức chính:

1.  **`Execute`:** Thực hiện công việc chính. Nhận vào dữ liệu `Arguments`.
2.  **`Compensate`:** Hoàn tác lại công việc đã làm trong `Execute`. Nhận vào dữ liệu `Log`.

### Arguments (Tham số)
Là dữ liệu đầu vào cho phương thức `Execute` của một Activity.

### Log (Nhật ký)
Là dữ liệu được tạo ra bởi phương thức `Execute` và được dùng làm đầu vào cho phương thức `Compensate` tương ứng khi cần hoàn tác.

---

## 3. Luồng hoạt động

### A. Luồng thực thi (Execute Path)
1.  Một **Routing Slip** được tạo ra với một danh sách các Activity theo thứ tự.
2.  Khi được thực thi, message sẽ được gửi đến Activity đầu tiên.
3.  Activity đầu tiên thực hiện phương thức `Execute`. Nếu thành công, nó trả về kết quả `Completed`.
4.  MassTransit tự động lấy Activity tiếp theo trong danh sách và gửi message đến đó.
5.  Quá trình lặp lại cho đến khi tất cả các Activity hoàn thành.

### B. Luồng bồi thường (Compensation Path)
1.  Giả sử Activity thứ 3 trong chuỗi bị lỗi và trả về `Faulted`.
2.  Routing Slip sẽ dừng luồng thực thi và bắt đầu quá trình bồi thường.
3.  Nó sẽ quay ngược lại và gọi phương thức `Compensate` của Activity thứ 2.
4.  Sau khi Activity 2 bồi thường xong, nó tiếp tục gọi `Compensate` của Activity 1.
5.  Quá trình kết thúc, đảm bảo hệ thống quay về trạng thái nhất quán.

---

## 4. Cách xây dựng và thực thi một Routing Slip

Bạn sử dụng `RoutingSlipBuilder` để tạo ra một lịch trình.

```csharp
var builder = new RoutingSlipBuilder(Guid.NewGuid());

// Thêm activity đầu tiên với arguments
builder.AddActivity("ProcessVideo", 
    new Uri("queue:process-video_execute"), 
    new { VideoId = "123" });

// Thêm activity thứ hai
builder.AddActivity("SendNotification", 
    new Uri("queue:send-notification_execute"),
    new { Text = "Your video is ready!" });

// Thêm dữ liệu có thể được truy cập bởi tất cả activities
builder.AddVariable("OriginalRequestTimestamp", DateTime.UtcNow);

var routingSlip = builder.Build();

// Bắt đầu thực thi routing slip
await _bus.Execute(routingSlip);
```

---

## 5. Cách tạo một Activity

Một Activity là một class kế thừa từ `IActivity` hoặc các interface tiện ích khác.

```csharp
// Ví dụ một activity để xử lý video
public class ProcessVideoActivity : IActivity<ProcessVideoArguments, ProcessVideoLog>
{
    // 1. Thực thi công việc
    public async Task<ExecutionResult> Execute(ExecuteContext<ProcessVideoArguments> context)
    {
        var videoId = context.Arguments.VideoId;
        Console.WriteLine($"Processing video: {videoId}");
        
        // Giả sử xử lý thành công và trả về đường dẫn video đã xử lý
        var processedPath = $"/processed/{videoId}.mp4";

        // Trả về Completed cùng với dữ liệu Log cho việc bồi thường
        return context.CompletedWithVariables<ProcessVideoLog>(
            new { OriginalVideoPath = $"/raw/{videoId}.mp4" }, // Log
            new { ProcessedVideoPath = processedPath } // Variables cho activity tiếp theo
        );
    }

    // 2. Hoàn tác công việc
    public async Task<CompensationResult> Compensate(CompensateContext<ProcessVideoLog> context)
    {
        // Lấy dữ liệu từ Log
        var originalPath = context.Log.OriginalVideoPath;
        Console.WriteLine($"Compensating: Deleting processed video and restoring {originalPath}");

        // ... Logic xóa file đã xử lý ...
        
        return context.Compensated();
    }
}
```

---

## 6. Theo dõi trạng thái

Bạn có thể tạo một Consumer để lắng nghe các sự kiện của Routing Slip và theo dõi trạng thái của workflow:
* `RoutingSlipCompleted`: Khi tất cả các activity hoàn thành thành công.
* `RoutingSlipFaulted`: Khi một activity thất bại và quá trình bồi thường đã kết thúc.
* `RoutingSlipActivityCompleted`, `RoutingSlipActivityFaulted`, v.v.