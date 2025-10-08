Chắc chắn rồi. Đây là nội dung cho file `.md` tổng hợp về **Testing** trong MassTransit.

-----

````markdown
# Tổng quan về Testing trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **Testing** (Kiểm thử) trong MassTransit, dựa trên documentation chính thức. MassTransit cung cấp một bộ công cụ kiểm thử mạnh mẽ (`MassTransit.Testing`) để giúp bạn viết các bài test đáng tin cậy cho consumer, saga và producer.

---

## 1. Test Harness: Môi trường thử nghiệm trong bộ nhớ 🔬

**Test Harness** là thành phần trung tâm của bộ công cụ testing. Nó tạo ra một "message bus ảo" chạy hoàn toàn **trong bộ nhớ (in-memory)**, cho phép bạn kiểm thử logic của mình mà không cần kết nối đến một message broker thực sự như RabbitMQ.

* **Lợi ích:**
    * **Tốc độ:** Test chạy cực kỳ nhanh.
    * **Cô lập:** Test không bị ảnh hưởng bởi các yếu tố bên ngoài.
    * **Đáng tin cậy:** Loại bỏ các lỗi kết nối mạng không mong muốn.

---

## 2. Thiết lập Test Harness

Bạn thường thiết lập Test Harness trong project test của mình (sử dụng các framework như xUnit, NUnit).

```csharp
// Ví dụ thiết lập Test Harness dùng Microsoft.Extensions.DependencyInjection
await using var provider = new ServiceCollection()
    .AddMassTransitTestHarness(x =>
    {
        // Đăng ký consumer bạn muốn test
        x.AddConsumer<SubmitOrderConsumer>();
    })
    .BuildServiceProvider(true);

var harness = provider.GetRequiredService<ITestHarness>();

await harness.Start();
try
{
    // ... Viết logic test của bạn ở đây ...
}
finally
{
    await harness.Stop();
}
```

---

## 3. Các thành phần chính để kiểm thử

Test Harness cung cấp các thuộc tính và phương thức để bạn kiểm tra trạng thái của bus sau khi thực hiện một hành động.

* **`harness.Sent`:** Một `IAsyncList<ISentMessage<T>>` chứa tất cả các message đã được **gửi (sent)**.
* **`harness.Published`:** Một `IAsyncList<IPublishedMessage<T>>` chứa tất cả các message đã được **xuất bản (published)**.
* **`harness.Consumed`:** Một `IAsyncList<IConsumedMessage<T>>` chứa tất cả các message đã được **tiêu thụ (consumed)** bởi bất kỳ consumer nào.
* **`harness.GetConsumerHarness<TConsumer>()`:** Lấy một "harness" riêng cho một consumer cụ thể, cho phép kiểm tra các message đã được tiêu thụ bởi chính consumer đó.

---

## 4. Các kịch bản kiểm thử

### A. Kiểm thử một Consumer

Đây là kịch bản phổ biến nhất. Luồng kiểm thử sẽ là:
1. Gửi một message đến bus.
2. Chờ cho consumer xử lý message đó.
3. Kiểm tra (assert) kết quả.

```csharp
// Lấy harness của consumer cụ thể
var consumerHarness = harness.GetConsumerHarness<SubmitOrderConsumer>();

// Gửi một message đến bus
await harness.Bus.Send(new SubmitOrder { OrderId = Guid.NewGuid() });

// Chờ và kiểm tra xem consumer đã nhận được message hay chưa
Assert.True(await consumerHarness.Consumed.Any<SubmitOrder>());
```

### B. Kiểm thử một Saga (State Machine)

MassTransit cung cấp `SagaTestHarness` để kiểm thử logic của Saga.
1. Đăng ký Saga Harness.
2. Gửi một event để khởi tạo hoặc tương tác với Saga.
3. Kiểm tra xem instance của Saga có được tạo ra, có chuyển đến đúng trạng thái hay không.

```csharp
// Đăng ký saga trong harness
x.AddSagaStateMachine<OrderStateMachine, OrderState>();

// Lấy harness của saga
var sagaHarness = provider.GetRequiredService<ISagaHarness<OrderState>>();
var orderId = Guid.NewGuid();

// Gửi event khởi tạo Saga
await harness.Bus.Publish<OrderSubmitted>(new { CorrelationId = orderId });

// Kiểm tra xem có instance saga nào được tạo ra không
Assert.True(await sagaHarness.Created.Any(x => x.CorrelationId == orderId));

// Kiểm tra xem instance đó có đang ở đúng trạng thái không
var instance = sagaHarness.Created.FirstOrDefault(x => x.CorrelationId == orderId);
Assert.That(instance.CurrentState, Is.EqualTo("Submitted"));
```

### C. Kiểm thử một Producer

Để kiểm thử một thành phần gửi đi message (ví dụ: một API Controller), bạn có thể:
1. Gọi phương thức trên component của bạn.
2. Dùng `harness.Published` hoặc `harness.Sent` để kiểm tra xem message có được publish/send ra bus với đúng nội dung hay không.

```csharp
// Giả sử _orderService là một service publish event OrderSubmitted
await _orderService.SubmitOrder(orderData);

// Kiểm tra xem có message IOrderSubmitted nào được publish ra bus hay không
Assert.True(await harness.Published.Any<IOrderSubmitted>());
```

---

## 5. Xử lý bất đồng bộ

Việc kiểm thử message vốn dĩ là bất đồng bộ. Test Harness của MassTransit đã tích hợp sẵn các cơ chế `await` và timeout. Các thuộc tính như `Consumed`, `Published`, `Sent` sẽ tự động chờ trong một khoảng thời gian nhất định để message được xử lý trước khi trả về kết quả, giúp cho việc viết test của bạn trở nên đơn giản hơn rất nhiều.
````