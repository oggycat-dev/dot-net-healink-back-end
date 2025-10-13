# Tổng quan về Saga Guidance trong MassTransit

Tài liệu này tổng hợp các **chỉ dẫn, lời khuyên, và thực hành tốt nhất (best practices)** khi thiết kế và triển khai Saga, dựa trên documentation chính thức.

---

## 1. Tư duy Điều phối (Think Orchestration) 🎼

Saga trong MassTransit được thiết kế theo mô hình **Orchestration**. Hãy coi Saga của bạn như một "nhạc trưởng".
* **Nhiệm vụ của Saga:** Điều phối các service khác, ra lệnh cho chúng thực hiện công việc.
* **Nguyên tắc "Tell, Don't Ask":** Saga nên **ra lệnh (tell)** cho các service khác (`Send<ProcessPayment>`) thay vì **hỏi (ask)** dữ liệu rồi tự ra quyết định. Hãy để các service tự chịu trách nhiệm cho logic nghiệp vụ của chúng.

---

## 2. Giữ cho Trạng thái (State) tinh gọn

Lớp trạng thái Saga (`TState`) chỉ nên chứa những dữ liệu **tối thiểu** cần thiết cho việc điều phối.
* **Nên chứa:**
    * Các ID (CorrelationId, OrderId, CustomerId...).
    * Trạng thái hiện tại (`CurrentState`).
    * Dữ liệu cần thiết để ra quyết định hoặc gửi command tiếp theo.
* **Không nên chứa:**
    * Toàn bộ object `Order` với đầy đủ chi tiết sản phẩm.
    * Dữ liệu nghiệp vụ lớn không liên quan đến việc điều phối.

Nếu bạn cần dữ liệu chi tiết, hãy lưu `OrderId` và khi cần thì truy vấn từ service Order. Điều này giúp Saga state nhẹ, nhanh và giảm xung đột dữ liệu.

---

## 3. Đặt tên nhất quán 🏷️

Việc đặt tên rõ ràng giúp cho State Machine của bạn dễ đọc và dễ hiểu.
* **States (Trạng thái):** Dùng **danh từ** hoặc **tính từ** ở dạng quá khứ.
    * `Submitted`, `AwaitingPayment`, `Shipped`, `Faulted`.
* **Events (Sự kiện):** Dùng **động từ ở thì quá khứ**.
    * `OrderSubmitted`, `PaymentCompleted`, `ShipmentFailed`.
* **Commands (Lệnh):** Dùng **động từ nguyên mẫu/mệnh lệnh**.
    * `ProcessPayment`, `ShipOrder`, `CancelOrder`.

---

## 4. Idempotency là bắt buộc

Do các cơ chế retry và đặc tính của message bus, một event có thể được giao đến saga của bạn **nhiều hơn một lần**.
* **Thiết kế Idempotent:** Logic của bạn phải đảm bảo rằng việc xử lý cùng một event nhiều lần không gây ra lỗi hoặc tác dụng phụ không mong muốn.
* **Ví dụ:** Khi nhận `PaymentCompleted`, thay vì chỉ `TransitionTo(Paid)`, hãy dùng `During(AwaitingPayment, When(PaymentCompleted).TransitionTo(Paid))`. Điều này đảm bảo rằng chỉ khi saga đang ở trạng thái `AwaitingPayment` thì nó mới chuyển sang `Paid`. Nếu nó đã ở trạng thái `Paid` rồi và nhận lại event này, nó sẽ bỏ qua.

---

## 5. Xử lý Concurrency (Đồng thời)

* **Ưu tiên Pessimistic Locking:** Khi sử dụng repository là database (như EF Core), hãy ưu tiên dùng chế độ **Pessimistic (khóa bi quan)**. Nó giúp đơn giản hóa đáng kể việc xử lý xung đột khi nhiều message cho cùng một saga đến cùng lúc.
* **Tránh Deadlock:** Cẩn thận khi saga của bạn `await` các tác vụ I/O dài. Nếu bạn đang dùng pessimistic lock, việc này có thể giữ khóa database quá lâu và gây ra deadlock.

---

## 6. Hoàn tất Saga (Finalize)

Một saga nên luôn có một trạng thái kết thúc.
* **Sử dụng `.Finalize()`:** Dùng hành vi này để chuyển saga đến trạng thái `Final`.
* **Dọn dẹp Repository:** Gọi `SetCompletedWhenFinalized()` trong hàm khởi tạo của state machine. Việc này sẽ tự động xóa instance saga khỏi repository sau khi nó được `Finalize`, giúp giữ cho bảng dữ liệu của bạn gọn gàng.
* **Lưu ý:** Nếu bạn cần giữ lại lịch sử của saga để kiểm tra, bạn có thể không muốn xóa nó. Thay vào đó, bạn có thể publish một event `OrderProcessCompleted` và lưu nó vào một nơi khác.