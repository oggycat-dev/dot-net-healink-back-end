# Tổng quan về Claim Check Pattern trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **Claim Check**, một pattern trong MassTransit để xử lý các message có dung lượng lớn, dựa trên documentation chính thức.

---

## 1. Vấn đề cần giải quyết: Message quá lớn 🐘

Hầu hết các message broker (như RabbitMQ, Azure Service Bus) đều có giới hạn về kích thước tối đa của một message (ví dụ: 256KB hoặc vài MB). Nếu bạn cần gửi một lượng dữ liệu lớn, ví dụ như một file ảnh, video, hoặc một file PDF, bạn không thể nhúng trực tiếp nó vào message.

**Claim Check** là một pattern được thiết kế để giải quyết chính xác vấn đề này.

---

## 2. Claim Check Pattern hoạt động như thế nào? 🎟️

Ý tưởng của pattern này tương tự như việc bạn gửi hành lý ở sân bay:

1.  **Gửi hành lý (Dữ liệu lớn):** Thay vì mang chiếc vali cồng kềnh lên máy bay, bạn gửi nó tại quầy check-in. Dữ liệu lớn (payload) của bạn sẽ được lưu trữ ở một nơi lưu trữ bên ngoài (blob storage, file system).
2.  **Nhận vé (Claim Check):** Bạn nhận lại một chiếc vé (claim check) - một mẩu thông tin nhỏ chứa địa chỉ duy nhất của chiếc vali.
3.  **Gửi message (Lên máy bay):** Bạn chỉ cần mang theo chiếc vé nhỏ gọn này. MassTransit sẽ gửi đi một message có kích thước rất nhỏ, chỉ chứa "claim check" (địa chỉ của dữ liệu lớn).
4.  **Nhận lại hành lý (Truy xuất dữ liệu):** Ở nơi đến, consumer nhận được message, đọc "claim check", và dùng nó để truy xuất dữ liệu lớn từ nơi lưu trữ bên ngoài.



Bằng cách này, message broker chỉ phải vận chuyển các message nhỏ, giúp hệ thống hoạt động nhanh và hiệu quả.

---

## 3. Triển khai trong MassTransit

MassTransit cung cấp sẵn các công cụ để triển khai pattern này một cách tự động.

### A. `IMessageDataRepository`
Đây là một interface trừu tượng đại diện cho nơi lưu trữ dữ liệu lớn. MassTransit cung cấp nhiều triển khai có sẵn:
* **`FileSystemMessageDataRepository`:** Lưu dữ liệu vào một thư mục trên hệ thống file.
* **`AzureStorageMessageDataRepository`:** Lưu dữ liệu vào Azure Blob Storage.
* **`AmazonS3MessageDataRepository`:** Lưu dữ liệu vào Amazon S3.
* **`MongoDbMessageDataRepository`:** Lưu dữ liệu vào MongoDB GridFS.

### B. Kiểu dữ liệu `MessageData<T>`
Để sử dụng claim check, bạn khai báo thuộc tính trong message contract của mình với kiểu `MessageData<T>`, với `T` là kiểu của dữ liệu lớn (ví dụ: `byte[]`, `string`, `Stream`).

```csharp
public interface SubmitDocument
{
    Guid DocumentId { get; }
    
    // Khai báo thuộc tính chứa dữ liệu lớn
    MessageData<byte[]> DocumentContent { get; }
}
```

---

## 4. Cách cấu hình và sử dụng

### A. Cấu hình Repository
Bạn cần cấu hình `IMessageDataRepository` khi thiết lập bus. Repository này sẽ được chia sẻ cho cả producer và consumer.

```csharp
// Đăng ký repository vào DI container
services.AddSingleton<IMessageDataRepository>(
    new FileSystemMessageDataRepository(new DirectoryInfo("message-data"))
);

// Trong AddMassTransit
services.AddMassTransit(x => 
{
    // ...
    // Báo cho MassTransit sử dụng repository đã đăng ký
    x.UseMessageData(provider.GetRequiredService<IMessageDataRepository>());
});
```

### B. Sử dụng trong Producer và Consumer

Điều tuyệt vời là MassTransit sẽ tự động hóa toàn bộ quy trình cho bạn.

**Producer:**
```csharp
// Dữ liệu lớn của bạn
byte[] largeDocument = await File.ReadAllBytesAsync("my-large-file.pdf");

// Chỉ cần gán dữ liệu vào thuộc tính MessageData
await _bus.Publish<SubmitDocument>(new 
{
    DocumentId = Guid.NewGuid(),
    DocumentContent = largeDocument 
});
```
*MassTransit sẽ tự động thấy `largeDocument`, lưu nó vào repository, và thay thế nó bằng một "claim check" trong message được gửi đi.*

**Consumer:**
```csharp
public class SubmitDocumentConsumer : IConsumer<SubmitDocument>
{
    public async Task Consume(ConsumeContext<SubmitDocument> context)
    {
        // Truy cập thuộc tính .Value để lấy dữ liệu
        // MassTransit đã tự động tải dữ liệu từ repository cho bạn
        if (context.Message.DocumentContent.HasValue)
        {
            MessageData<byte[]> documentContent = context.Message.DocumentContent;
            
            // Lấy nội dung thực sự (dưới dạng stream hoặc byte array)
            byte[] bytes = await documentContent.Value;
            
            Console.WriteLine($"Received document with {bytes.Length} bytes.");
        }
    }
}
```
*Logic của consumer vẫn rất đơn giản, như thể dữ liệu được gửi trực tiếp trong message.*