# Runbook Deployment — StockLedger Retail trên Kubernetes

Tài liệu hướng dẫn triển khai hệ thống **StockLedger Retail** (.NET 10 Web API + Next.js 15 Web Client) trên môi trường Kubernetes theo chuẩn CI/CD GitOps của ACFC.

---

## 1. Kiến trúc tổng quan

```text
                        ┌──────── Ingress Nginx ────────┐
                        │                               │
        stockledger-uat.acfc.com.vn        stockledger-api-uat.acfc.com.vn
                        │                               │
                        ▼                               ▼
             stockledger-frontend              stockledger-backend
               (Next.js 15, 2 pods)           (.NET 10 API, 2 pods)
                        │                               │
                        └───────► /api/v1 ──────────────┤
                                                        │
    ────────────────────────────────────────────────────┼───────────────────
    Hạ tầng ngoài cluster (Database & Middleware)       │
                                                        ▼
                                           ┌─────────────────────────┐
                                           │ PostgreSQL 16           │
                                           │ Redis (Cache & Session) │
                                           └─────────────────────────┘
```

---

## 2. Danh mục tài nguyên Kubernetes (`k8s/`)

| File | Loại | Mô tả |
|---|---|---|
| `00-namespace.yaml` | Namespace | Tạo namespace `stockledger-retail` |
| `01-configmap.yaml` | ConfigMap | Cấu hình chung không bảo mật (URLs, Port, Rates...) |
| `02-secrets.example.yaml` | Secret | Template chứa ConnectionStrings, JWT Secret, Admin Auth |
| `10-backend.yaml` | Deployment + Service | .NET 10 API với Health Probes (`/health`, `/health/ready`) |
| `20-frontend.yaml` | Deployment + Service | Next.js Standalone Runner |
| `30-ingress.sample.yaml` | Ingress | Định tuyến Domain cho Frontend và Backend API |
| `kustomization.yaml` | Kustomize | Quản lý version tag của Docker Images phục vụ GitOps |

---

## 3. Các bước triển khai lần đầu (Day 1)

### Bước 1: Chuẩn bị Secret
Sao chép `k8s/02-secrets.example.yaml` thành `02-secrets.yaml` và điền giá trị thật:
```bash
cp k8s/02-secrets.example.yaml k8s/02-secrets.yaml
# Chỉnh sửa ConnectionStrings, JWT Signing Key, Redis Password...
kubectl apply -f k8s/02-secrets.yaml
```

### Bước 2: Apply Namespace và ConfigMap
```bash
kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/01-configmap.yaml
```

### Bước 3: Deploy Workloads bằng Kustomize
```bash
kubectl apply -k k8s/
```

### Bước 4: Kiểm tra trạng thái Pods
```bash
kubectl get pods -n stockledger-retail -o wide
kubectl logs -n stockledger-retail -l app=stockledger-backend --tail=50
```

---

## 4. Biến môi trường quan trọng cần cấu hình trong Bitbucket Pipeline

Khi cấu hình **Repository Variables** trên Bitbucket:

* `REGISTRY_URL`: Địa chỉ Docker Registry (ví dụ: `registry.acfc.com.vn` hoặc `harbor.acfc.com.vn`).
* `REGISTRY_USERNAME`: Tài khoản robot push image.
* `REGISTRY_PASSWORD`: Mật khẩu robot push image.
* `IMAGE_NAME_BACKEND`: Tên image backend (ví dụ: `stockledger/backend`).
* `IMAGE_NAME_FRONTEND`: Tên image frontend (ví dụ: `stockledger/frontend`).
* `GITOPS_REPO`: Tên repository chứa k8s manifest GitOps (ví dụ: `acfc-it/stockledger-deployment`).
* `GITOPS_ACCESS_TOKEN`: Access Token có quyền ghi vào repo GitOps.
