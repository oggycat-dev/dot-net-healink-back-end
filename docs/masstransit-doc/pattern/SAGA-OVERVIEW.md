# Tổng quan về Saga Pattern trong MassTransit

Tài liệu này tổng hợp các khái niệm cốt lõi về **Saga Pattern** và cách MassTransit triển khai nó, dựa trên documentation chính thức. Saga là một pattern để quản lý các "giao dịch" (transactions) kéo dài và phức tạp qua nhiều microservice khác nhau.

---

## 1. Vấn đề: Giao dịch trong Microservices

Trong một ứng dụng monolith, bạn có thể dùng database transaction để đảm bảo tính toàn vẹn dữ liệu. Tuy nhiên, trong kiến trúc microservices, một quy trình nghiệp vụ (ví dụ: đặt hàng) có thể liên quan đến nhiều service, mỗi service có database riêng. Bạn không thể dùng một database transaction duy nhất cho tất cả.

**Saga Pattern** giải quyết vấn đề này bằng cách điều phối một chuỗi các giao dịch cục bộ (local transactions) tại từng service.



---

## 2. Saga là gì? 📜

Một **Saga** là một **máy trạng thái (state machine)**, có khả năng điều phối một quy trình nghiệp vụ. Nó hoạt động bằng cách:
1.  **Lắng nghe** các sự kiện (events).
2.  **Thay đổi** trạng thái nội tại của nó.
3.  **Ra lệnh** cho các service khác thực hiện công việc (bằng cách gửi commands).

Nếu một bước trong chuỗi thất bại, Saga có trách nhiệm thực hiện các **hành động bồi thường (compensating actions)** để hoàn tác các bước đã thành công trước đó, đảm bảo dữ liệu toàn hệ thống được nhất quán.

**Ví dụ:** Quy trình đặt hàng
1.  **Saga Bắt đầu:** Nhận sự kiện `OrderSubmitted`.
2.  **Hành động 1:** Gửi command `ProcessPayment` đến Service Thanh toán. Saga chuyển sang trạng thái `AwaitingPayment`.
3.  **Sự kiện 2:** Nhận sự kiện `PaymentCompleted`.
4.  **Hành động 2:** Gửi command `ShipOrder` đến Service Giao hàng. Saga chuyển sang trạng thái `AwaitingShipment`.
5.  **Hoàn tất:** Nhận sự kiện `OrderShipped`. Saga chuyển sang trạng thái `Completed`.

---

## 3. Orchestration vs. Choreography

Có hai cách chính để triển khai Saga:

### A. Choreography (Tự điều phối)
* **Cách hoạt động:** Các service tự lắng nghe sự kiện của nhau và phản ứng. Không có một "nhạc trưởng" trung tâm.
* **Ví dụ:** Service Order publish `OrderSubmitted`. Service Payment nghe thấy và tự xử lý. Sau đó Service Payment publish `PaymentCompleted`, và Service Shipping nghe thấy để tự xử lý.
* **Ưu/Nhược điểm:** Rất lỏng lẻo (decoupled), nhưng luồng nghiệp vụ bị phân tán và khó theo dõi.

### B. Orchestration (Điều phối)
* **Cách hoạt động:** Có một **bộ điều phối trung tâm (Orchestrator)** ra lệnh cho các service khác.
* **Ví dụ:** Saga `Order` nhận `OrderSubmitted`, nó sẽ ra lệnh cho Service Payment. Khi nhận được phản hồi, nó tiếp tục ra lệnh cho Service Shipping.
* **Ưu/Nhược điểm:** Luồng nghiệp vụ rõ ràng, tập trung, dễ quản lý và theo dõi. Đây là **mô hình mà MassTransit Saga State Machine triển khai**.

---

## 4. Saga trong MassTransit

Trong MassTransit, một Saga được triển khai dưới dạng một **State Machine**, kế thừa từ `MassTransitStateMachine<TState>`.

### Các thành phần chính:
* **State (`TState`):** Một class chứa dữ liệu của một instance saga (ví dụ: `OrderState`), được lưu trữ trong một repository (in-memory, database...).
* **Event:** Một message đến, kích hoạt một sự thay đổi trong saga.
* **Behavior:** Các hành động (Activities) được thực thi khi một event xảy ra, ví dụ: `.Then(...)`, `.Publish(...)`, `.Request(...)`.
* **State (trạng thái):** Các "điểm dừng" trong quy trình (`Initial`, `Submitted`, `Paid`, `Final`).

### Correlation (Tương quan)
Đây là yếu-tố cốt-lõi. Mỗi saga instance phải có một `CorrelationId` duy-nhất. Khi một message đến, MassTransit phải biết cách tìm đúng saga instance để xử-lý. Ví dụ, tất-cả các sự kiện liên-quan đến một đơn-hàng phải có cùng một giá-trị `OrderId` để tương-quan.

### Compensation (Bồi thường)
Saga pattern cung cấp khả năng phục-hồi mạnh-mẽ. Nếu Service Giao-hàng báo lỗi, Saga có-thể gửi một command `RefundPayment` đến Service Thanh-toán để hoàn tác lại giao-dịch đã thực-hiện ở bước trước.