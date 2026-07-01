# Development Guide (Kỹ thuật)

Tài liệu dành cho **developer / DevOps** — cấu hình local, test, smoke, health check, scope kho, và pricing contract.  
Quy tắc nghiệp vụ cho end-user xem [BusinessRules.vi.md](BusinessRules.vi.md); use case xem [UseCases.md](UseCases.md).

---

## Kiến trúc tính toán

| Layer | Trách nhiệm |
|-------|-------------|
| **Backend (authoritative)** | Giá VAT, margin, duyệt phiếu, ghi `StockTransaction`, scope kho, validation nghiệp vụ |
| **Frontend (preview)** | Hiển thị, nhập liệu, tính preview giá — **không** là nguồn sự thật; BE từ chối nếu lệch |

Pricing dùng chung logic: `src/StockLedgerRetail.Domain.Shared/Pricing/PricingCalculator.cs`.

---

## Warehouse scope (`IWarehouseScopeService`)

Service: `src/StockLedgerRetail.Application/Authorization/WarehouseScopeService.cs`

| Method | Mục đích |
|--------|----------|
| `ResolveListScope(warehouseId?)` | List API: trả `WarehouseId` đơn hoặc `ScopedWarehouseIds` |
| `EnsureWarehouseAccess(warehouseId)` | Get/mutation: ném `UnauthorizedAccessException` nếu user không có quyền kho |
| `GetWarehouseFilterForLists()` | `null` = không giới hạn (admin / `inventory.scope.all_warehouses`) |

**Unrestricted** khi: chưa auth, `system.admin`, `inventory.scope.all_warehouses`, hoặc `AllowedWarehouseIds == null`.

**Đã áp scope** (list + mutation where applicable):

- Current stock, stock transactions, inventory documents
- Purchase orders, goods receipts
- Stock reservations, reconciliation
- Reports, analytics summary, inventory insights
- Warehouses list/get

Gán kho user: bảng `user_warehouse_assignments` + UI `admin/users` (`warehouseAssignments`, `isPrimary`).  
Demo clerk: `clerk@stockledger.local` / `1234` — seed `DemoUserSeedService` gán `DOMINOS-ST-Q1` nếu thiếu.

---

## Health endpoints

| Route | Mục đích |
|-------|----------|
| `GET /health` | Liveness — process up |
| `GET /health/ready` | Readiness — `Database.CanConnectAsync()` |

Không yêu cầu `X-User-Email`. Controller: `host/StockLedgerRetail.HttpApi.Host/Controllers/HealthController.cs`.

---

## Automated tests

### Unit tests

```bash
dotnet test tests/StockLedgerRetail.Domain.Shared.Tests
dotnet test tests/StockLedgerRetail.Application.Tests
```

- **Pricing contract:** `shared/pricing-contract-cases.json` — BE `PricingCalculatorTests`, FE `npm run test:pricing` (`frontend/scripts/pricing-contract.test.mjs`).

### Integration tests (PO → GR → Stock-in)

Project: `tests/StockLedgerRetail.Integration.Tests`

```bash
dotnet test tests/StockLedgerRetail.Integration.Tests
```

**Yêu cầu:** PostgreSQL chạy; connection trong `tests/.../appsettings.Testing.json` hoặc env `STOCKLEDGER_TEST_CONNECTION`.

**Fixture:** migrate DB, seed F&B (`IFbDataSeedService`), auth `system.admin` + unrestricted warehouse.

| Test | Assert |
|------|--------|
| `FullFlow_PoGrStockIn_*` | PO Received, GR Approved, stock-in PROCUREMENT, `current_stocks` + `stock_transactions` |
| `PartialReceipt_*` | PartiallyReceived → Received, 2 GR |
| `HighValuePo_*` | PO ≥ 10M VND → 2-step approval |
| `ApproveGoodsReceiptTwice_*` | Idempotency |

Dữ liệu test dùng reference `IT-PO-*` / `IT-GR-*` — không xóa sau test (shared dev DB).

---

## Smoke scripts (PowerShell)

Chạy khi API đang listen (mặc định `http://localhost:5270`):

```powershell
.\scripts\api-scope-smoke.ps1
.\scripts\rollout-smoke.ps1
```

| Script | Kiểm tra |
|--------|----------|
| `api-scope-smoke.ps1` | Clerk ≤ admin trên reports + current stocks; pricing reject path |
| `rollout-smoke.ps1` | `/health`, `/health/ready`, scope trên 6 list API, analytics summary |

Headers: `X-User-Email: admin@stockledger.local` / `clerk@stockledger.local`.

---

## Audit log (dev tra cứu)

Bảng `transaction_logs` — không cần UI nặng; query DB hoặc API:

```
GET /api/audit-logs?entityName=AppUser&createdFrom=...&createdBy=...&action=...
```

Composite indexes: migration `AddTransactionLogCompositeIndexes` (`entity_name+created_at`, `created_by+created_at`, `action+created_at`).

AppUser create/update được ghi audit (không log password).

---

## Migrations gần đây

| Migration | Nội dung |
|-----------|----------|
| `AddUserWarehouseAssignments` | `user_warehouse_assignments` |
| `AddTransactionLogCompositeIndexes` | Index audit log |

```bash
dotnet ef database update \
  --project src/StockLedgerRetail.EntityFrameworkCore \
  --startup-project host/StockLedgerRetail.HttpApi.Host
```

---

## Local troubleshooting

| Vấn đề | Gợi ý |
|--------|--------|
| Build host fail (file locked) | Dừng process API đang chạy rồi build lại |
| Next.js Internal Server Error sau build | `cd frontend && npm run dev:fresh` |
| Clerk không thấy dữ liệu kho | Restart API (seed clerk warehouse); kiểm tra assignment trong admin users |
| Integration test skip DB | Kiểm tra Postgres + connection string |

---

## Liên kết

| Doc | Đối tượng |
|-----|-----------|
| [RBAC.md](RBAC.md) | Permission codes, teams, warehouse assignment API |
| [BusinessRules.vi.md](BusinessRules.vi.md) | Quy tắc nghiệp vụ (user/BA) |
| [ERD.md](ERD.md) | Schema DB |
