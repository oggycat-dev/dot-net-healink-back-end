# Tổng quan về Messages trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về message trong MassTransit, dựa trên documentation chính thức. **Message** là một "hợp đồng" (contract) được định nghĩa dưới dạng một đối tượng .NET, dùng để giao tiếp giữa các service.

## Các loại Message

MassTransit phân loại message dựa trên **ý định (intent)**. Có ba loại chính: Commands, Events, và Documents.

---

### 1. Commands (Lệnh) ➡️

**Ý định:** Ra lệnh cho một service khác **làm một điều gì đó**.

* **Đặc điểm:**
    * Tên thường là một động từ ở dạng mệnh lệnh (ví dụ: `SubmitOrder`, `UpdateCustomerAddress`).
    * Thể hiện mối quan hệ **1-1** (một người gửi, một người nhận).
    * Được **gửi (sent)** đến một endpoint (một queue) cụ thể.
* **Cách dùng:**
    * Sử dụng `ISendEndpoint` hoặc `IBus` để `Send()` một command.
    * Lệnh không nên được publish.

```csharp
// Ví dụ về một Command
public interface SubmitOrder
{
    Guid OrderId { get; }
    DateTime OrderDate { get; }
    string CustomerNumber { get; }
}
```

---

### 2. Events (Sự kiện) 📢

**Ý định:** Thông báo rằng **một điều gì đó đã xảy ra**.

* **Đặc điểm:**
    * Tên thường là một động từ ở thì quá khứ (ví dụ: `OrderSubmitted`, `CustomerAddressUpdated`).
    * Thể hiện mối quan hệ **1-n** (một người phát hành, nhiều người đăng ký).
    * Được **xuất bản (published)** cho tất cả các consumer quan tâm.
    * Sự kiện phải là **bất biến (immutable)**.
* **Cách dùng:**
    * Sử dụng `IPublishEndpoint` hoặc `IBus` để `Publish()` một event.

```csharp
// Ví dụ về một Event
public interface OrderSubmitted
{
    Guid OrderId { get; }
    DateTime OrderDate { get; }
    string CustomerNumber { get; }
}
```

---

### 3. Documents (Tài liệu) 📄

**Ý định:** Một loại message lớn hơn, dùng để chuyển giao dữ liệu hoặc trạng thái. Thường được sử dụng trong các mẫu kiến trúc nâng cao như Event Sourcing hoặc CQRS. MassTransit không có định nghĩa chặt chẽ cho loại này, nhưng nó khác với command và event.

---

## Nguyên tắc thiết kế Message Contracts

Để đảm bảo hệ thống dễ bảo trì và mở rộng, hãy tuân thủ các nguyên tắc sau khi thiết kế message.

### ✅ Nên dùng Interfaces
Sử dụng interface cho message contracts là cách làm tốt nhất (best practice).
* **Tách biệt:** Tách biệt hoàn toàn "hợp đồng" khỏi việc triển khai.
* **Linh hoạt:** Dễ dàng cho việc versioning và chia sẻ giữa các service.
* **Gọn nhẹ:** Chỉ định nghĩa những gì cần thiết.

### ✅ Nên là Bất biến (Immutable)
Message contracts nên là bất biến.
* Các thuộc tính chỉ nên có `get`.
* Với C# 9+, có thể dùng `init` để cho phép khởi tạo giá trị ban đầu.
* Điều này đảm bảo message không bị thay đổi trong quá trình vận chuyển và xử lý.

```csharp
// Ví dụ về contract bất biến
public interface UpdateCustomerAddress
{
    Guid CustomerId { get; init; }
    string NewAddress { get; init; }
}
```

### ✅ Giữ cho Message tinh gọn
* Chỉ bao gồm các thuộc tính dữ liệu đơn giản (primitive types, strings, numbers, booleans) và các kiểu phức hợp (complex types).
* **Không** bao gồm các hành vi (methods) hay logic nghiệp vụ.
* **Không** bao gồm các kiểu dữ liệu khó hoặc không thể serialize, như `Stream`, `Task`, `Delegate`.

Message chỉ đơn giản là một "Đối tượng Truyền tải Dữ liệu" (Data Transfer Object - DTO).