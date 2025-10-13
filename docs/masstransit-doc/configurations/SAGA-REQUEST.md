# Tổng quan về Cấu hình Request trong Saga

Tài liệu này tổng hợp các khái niệm cốt lõi về cách một **Saga** có thể gửi đi một message **Request** đến một service khác và chờ đợi một message **Response** để tiếp tục quy trình của mình, dựa trên documentation chính thức.

---

## 1. Request trong Saga là gì? 🗣️

Trong một quy trình nghiệp vụ phức tạp, một Saga thường cần thông tin từ một service khác hoặc cần yêu cầu một service khác thực hiện một hành động trước khi nó có thể tiếp tục.

**Saga Request** là cơ chế cho phép Saga:
1.  **Gửi** một message yêu cầu (Request) đến một consumer.
2.  **Tạm dừng** và chờ đợi.
3.  **Phản ứng** với các message phản hồi (Response) khác nhau: thành công, thất bại, hoặc quá thời gian chờ (timeout).



---

## 2. Khai báo một Request

Một `Request` được khai báo như một thuộc tính bên trong class State Machine.

* **Cú pháp:** `Request<TState, TRequest, TResponse, TResponse2...>`
    * `TState`: Kiểu dữ liệu trạng thái của Saga.
    * `TRequest`: Kiểu dữ liệu của message yêu cầu.
    * `TResponse`: Kiểu dữ liệu của message phản hồi thành công.
    * `TResponse2...`: Các kiểu dữ liệu phản hồi khác (tùy chọn).

```csharp
public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    // Khai báo một request để xác thực khách hàng
    // Saga: OrderState
    // Request: IValidateCustomer
    // Response thành công: ICustomerValidated
    // Response thất bại: ICustomerInvalid
    public Request<OrderState, IValidateCustomer, ICustomerValidated, ICustomerInvalid> ValidateCustomerRequest { get; private set; }

    // ...
}
```

---

## 3. Cấu hình và Gửi Request

### A. Cấu hình trong hàm khởi tạo
Bạn cần cấu hình request trong hàm khởi tạo của State Machine, bao gồm cả địa chỉ của service sẽ nhận request và cài đặt timeout.

```csharp
public OrderStateMachine()
{
    // ...
    Request(() => ValidateCustomerRequest, x =>
    {
        // Endpoint của service khách hàng
        x.ServiceAddress = new Uri("queue:customer-service");
        // Thời gian chờ phản hồi
        x.Timeout = TimeSpan.FromSeconds(30);
    });
}
```

### B. Gửi Request trong một `Activity`
Request được gửi đi bên trong một khối hành vi (ví dụ: `Then` hoặc `Publish`).

```csharp
Initially(
    When(OrderSubmitted)
        .Request(ValidateCustomerRequest, context => 
            // Tạo message request từ dữ liệu của message kích hoạt
            context.Init<IValidateCustomer>(new { CustomerId = context.Message.CustomerId })
        )
        .TransitionTo(Submitted) // Chuyển sang trạng thái chờ xác thực
);
```
---

## 4. Xử lý các Phản hồi (Responses)

Sau khi gửi request, Saga sẽ ở trạng thái chờ. Bạn cần định nghĩa các hành vi tương ứng với từng loại phản hồi có thể xảy ra.

### A. Xử lý Response thành công
Sử dụng `When(MyRequest.Completed)` để xử lý khi nhận được response thành công (`ICustomerValidated` trong ví dụ này).

```csharp
During(Submitted,
    When(ValidateCustomerRequest.Completed)
        .Then(context => {
            Console.WriteLine("Customer validated!");
            // ... logic xử lý tiếp theo
        })
        .TransitionTo(Accepted) // Chuyển sang trạng thái tiếp theo
);
```

### B. Xử lý Response lỗi nghiệp vụ
Đây là response do consumer chủ động trả về để báo một lỗi nghiệp vụ (`ICustomerInvalid` trong ví dụ).

```csharp
During(Submitted,
    // Lưu ý: Response này cũng được coi là .Completed vì consumer đã trả lời
    When(ValidateCustomerRequest.Completed<ICustomerInvalid>)
        .Then(context => {
            Console.WriteLine($"Customer is invalid: {context.Message.Reason}");
        })
        .TransitionTo(Faulted) // Chuyển sang trạng thái lỗi
);
```

### C. Xử lý khi Request bị `Fault`
Sử dụng `When(MyRequest.Faulted)` để xử lý khi consumer nhận request ném ra một exception không mong muốn.

```csharp
During(Submitted,
    When(ValidateCustomerRequest.Faulted)
        .Then(context => Console.WriteLine("Request faulted!"))
        .TransitionTo(Faulted)
);
```

### D. Xử lý khi Request bị `Timeout`
Sử dụng `When(MyRequest.TimeoutExpired)` để xử lý khi không nhận được bất kỳ phản hồi nào trong khoảng thời gian đã định.

```csharp
During(Submitted,
    When(ValidateCustomerRequest.TimeoutExpired)
        .Then(context => Console.WriteLine("Request timed out!"))
        .TransitionTo(Faulted)
);
```