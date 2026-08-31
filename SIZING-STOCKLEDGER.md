# Kế hoạch & Bảng Sizing Hạ tầng — StockLedger Retail

Tài liệu ước tính tải và đề xuất cấu hình hạ tầng cho hệ thống **StockLedger Retail** với quy mô chuỗi **400 – 600 cửa hàng**, tối ưu hóa chi phí đầu tư hạ tầng theo mô hình *Start Lean & Auto-Scale*.

---

## 1. Ước tính quy mô & Tải hệ thống (ACFC 500 – 600 Stores)

| Chỉ số | Quy mô ước tính | Ghi chú |
|---|---|---|
| **Số cửa hàng / Điểm bán** | 500 – 600 stores + 3–5 Kho tổng (DC) | Quản lý đa thương hiệu (Nike, Mango, Levi's, GAP...) |
| **Số lượng SKU quản lý** | 50.000 – 100.000 SKUs | Master data & biến thể kích cỡ / màu sắc |
| **Người dùng đồng thời (Human CCU)** | **600 – 1.000 CCU** | Nhân viên store scan barcode + thủ kho + HQ duyệt đơn |
| **Tải ngày thường (Average RPS)** | **150 – 300 Requests/s** | POS sync, store kiểm tồn lẻ tẻ |
| **Tải giờ cao điểm / Siêu Sale (Peak RPS)** | **1.000 – 1.500 Requests/s** | Cuối tuần, Mega Campaign (Black Friday, 11/11, Payday) |

---

## 2. Bảng Sizing Hạ tầng Tối ưu Chi phí

### 2.1. Kubernetes Workloads (Ứng dụng)

| Thành phần | Số lượng Pods | CPU Request / Limit | RAM Request / Limit | Cơ chế Scaling |
|---|:---:|:---:|:---:|---|
| **Backend API**<br>*(.NET 10 Web API)* | **2 Pods** | `250m` / `1000m` *(0.25 - 1.0 Core)* | `512Mi` / `1024Mi` *(0.5 - 1.0 GB)* | Tự động tăng lên **4–5 Pods** khi CPU > 70% (HPA) |
| **Frontend Web**<br>*(Next.js 15 Standalone)* | **2 Pods** | `150m` / `500m` *(0.15 - 0.5 Core)* | `256Mi` / `512Mi` *(0.25 - 0.5 GB)* | Giữ cố định 2 Pods phục vụ giao diện |
| **👉 Tổng K8s Resource** | **4 Pods** | **~1.0 – 1.5 Cores** | **~2.0 – 3.0 GB RAM** | **Chi phí vận hành rất nhẹ** |

---

### 2.2. Cơ sở dữ liệu & Bộ nhớ đệm (Ngoài Cluster / Dedicated VM)

| Thành phần | Thông số Đề xuất | Mục đích sử dụng | Đánh giá Chi phí |
|---|---|---|:---:|
| **PostgreSQL 16** | • **CPU:** 4 vCPU<br>• **RAM:** 8 GB<br>• **Ổ cứng:** 150 – 200 GB SSD (NVMe)<br>• **IOPS:** 2.000 – 3.000 IOPS | Lưu trữ sổ cái biến động kho (`stock_transactions`), đơn hàng PO/GR, kiểm kê, audit trail. | 🟡 **Vừa phải**<br>(Dùng tốt trong 2–3 năm) |
| **Redis 7** | • **CPU:** 1 vCPU<br>• **RAM:** 2 GB<br>• **Chính sách:** `maxmemory-policy: allkeys-lru` | Bộ nhớ đệm tra cứu tồn kho tức thì (< 2ms), xác thực phiên làm việc, chống quá tải cho PostgreSQL. | 🟢 **Rất rẻ** |

---

## 3. Cấu hình Tổng Hợp Toàn Bộ Hệ Thống (Total Hardware Spec)

Bảng gộp toàn bộ tài nguyên phần cứng (K8s App + PostgreSQL + Redis) để Infra cấp máy ảo (VM) hoặc Cloud Instance:

| Hạng mục tài nguyên | Mức Tải Ngày Thường (Tiết kiệm) | Mức Tải Đỉnh / Siêu Sale (Max Scale) | Ghi chú |
|---|:---:|:---:|---|
| **Tổng CPU (vCPU / Cores)** | **6 Cores**<br>*(K8s: 1.0 + DB: 4.0 + Redis: 1.0)* | **10 Cores**<br>*(K8s: 5.0 + DB: 4.0 + Redis: 1.0)* | K8s tự động scale CPU khi tải tăng, ngày thường chỉ tốn 6 cores. |
| **Tổng RAM (Bộ nhớ)** | **12.5 GB**<br>*(K8s: 2.5GB + DB: 8GB + Redis: 2GB)* | **16.5 GB**<br>*(K8s: 6.5GB + DB: 8GB + Redis: 2GB)* | Đảm bảo bộ nhớ đệm cho DB & K8s không bị OOM. |
| **Tổng Ổ cứng (SSD/NVMe)** | **180 – 230 GB SSD** | **180 – 230 GB SSD** | DB: 150-200 GB, K8s Pods: ~20 GB, Redis: ~10 GB. |

---

## 4. Tóm tắt nhanh gói tài nguyên gửi Đội ngũ Hạ tầng (DevOps/Infra)

```text
📋 TỔNG HỢP YÊU CẦU CẤP PHÁT TÀI NGUYÊN (SUMMARY SPEC REQUEST):

1. Kubernetes Namespace: stockledger-retail
   - CPU Quota: 4 Cores (Dùng thực tế ngày thường ~1.0 - 1.5 Cores)
   - RAM Quota: 6 GB (Dùng thực tế ngày thường ~2.5 - 3.0 GB)

2. PostgreSQL 16 Dedicated VM / Instance:
   - 4 vCPU, 8 GB RAM, 150 - 200 GB SSD NVMe

3. Redis 7 Dedicated VM / Instance:
   - 1 vCPU, 2 GB RAM, 10 GB SSD

👉 TỔNG CỘNG TOÀN HỆ THỐNG: 6 - 10 vCPU | 12.5 - 16.5 GB RAM | ~200 GB SSD
```

---

## 5. Tại sao cấu hình này vừa rẻ vừa chịu tải tốt?

1. **.NET 10 Kestrel Web Server:** Tối ưu hóa bộ nhớ và throughput rất cao, 1 Core có thể xử lý hàng ngàn request đồng thời.
2. **Chiến lược Cache-Aside với Redis:** ~80% các truy vấn kiểm tra tồn kho, barcode, quyền hạn được giải quyết ngay tại Redis, giảm 80% tải đọc trực tiếp vào database PostgreSQL.
3. **Next.js Standalone Build:** Đóng gói tối giản, chỉ tốn ~150MB RAM mỗi container.
4. **Cơ chế HPA (Horizontal Pod Autoscaler):** Ngày thường chạy cấu hình thấp để tiết kiệm ngân sách, khi có đợt Sale K8s sẽ tự sinh thêm container để gánh tải và tự hủy khi hết tải.
