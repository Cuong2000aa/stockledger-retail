# Authorization & Permissions (RBAC)

Phân quyền theo **email**, nhóm quyền lưu trong PostgreSQL. Trưởng nhóm có thể quản lý và duyệt phiếu do thành viên trong team tạo.

---

## Nhận diện user

**Frontend:** màn hình `/vi/login` (hoặc `/en/login`) — tạm thời `admin` / `1234`.

**API:** `POST /api/auth/login` (không cần header) → trả email + quyền; các request sau gửi:

```http
X-User-Email: admin@stockledger.local
```

User phải tồn tại trong bảng `app_users` và `IsActive = true`.

Cấu hình `host/StockLedgerRetail.HttpApi.Host/appsettings.json`:

```json
"Auth": {
  "RequireUserEmail": true,
  "BootstrapAdminEmail": "admin@stockledger.local"
}
```

Lần chạy API đầu tiên sẽ seed quyền/nhóm và tạo admin bootstrap (nếu chưa có).

---

## Nhóm quyền mặc định

| Nhóm | Mã | Quyền chính |
|------|-----|-------------|
| System Admin | `SYSTEM_ADMIN` | `system.admin` (toàn quyền) |
| Team Leader | `TEAM_LEADER` | Xem, tạo, sửa, hủy; **duyệt/nhận chuyển kho** phiếu của thành viên team |
| Warehouse Clerk | `WAREHOUSE_CLERK` | Xem, tạo, sửa, hủy **phiếu của chính mình** |
| Viewer | `VIEWER` | Chỉ xem |

### Mã quyền (permissions)

| Mã | Mô tả |
|----|--------|
| `inventory.documents.view` | Xem phiếu |
| `inventory.documents.create` | Tạo phiếu Draft |
| `inventory.documents.update` | Sửa Draft (của mình hoặc team nếu là trưởng nhóm) |
| `inventory.documents.cancel` | Hủy Draft |
| `inventory.documents.approve` | Duyệt mọi phiếu (admin) |
| `inventory.documents.approve.team` | Trưởng nhóm duyệt phiếu thành viên |
| `inventory.documents.receive-transfer` | Nhận chuyển kho |
| `admin.users.manage` | Quản lý user |
| `admin.groups.manage` | Xem nhóm/quyền |
| `admin.teams.manage` | Quản lý team |

---

## Team & trưởng nhóm

- Mỗi **Team** có một `LeaderUserId` và danh sách `Members`.
- Trưởng nhóm (nhóm `TEAM_LEADER`) có thể **sửa / hủy / duyệt / receive-transfer** phiếu mà `CreatedBy` là email thành viên trong team mình.
- Nhân viên kho chỉ thao tác phiếu do chính họ tạo.

---

## API quản trị

| API | Mô tả |
|-----|--------|
| `GET /api/auth/me` | User hiện tại + quyền |
| `GET/POST/PUT /api/admin/users` | CRUD user, gán nhóm |
| `GET /api/admin/permissions` | Danh sách quyền |
| `GET /api/admin/permissions/groups` | Nhóm + quyền |
| `GET/POST/PUT /api/admin/teams` | CRUD team |

Yêu cầu quyền `admin.*` hoặc `system.admin`.

---

## Luồng thiết lập

1. Start API → seed permissions/groups + bootstrap admin.
2. Đăng nhập Swagger với header `X-User-Email: admin@stockledger.local`.
3. `POST /api/admin/users` — tạo clerk, gán `WAREHOUSE_CLERK`.
4. `POST /api/admin/users` — tạo leader, gán `TEAM_LEADER`.
5. `POST /api/admin/teams` — gán leader + members.
6. Clerk tạo phiếu (header email clerk) → Leader duyệt (header email leader).

---

## Bảng DB

- `app_users`
- `permissions`
- `permission_groups`
- `group_permissions`
- `user_group_assignments`
- `teams`
- `team_members`

Migration: `AddIdentityAndPermissions`

---

## Ghi chú

- Integration API (`/api/integration/*`) không yêu cầu email — dùng API key như trước.
- JWT/OAuth có thể thay thế header email ở bước sau; quyền vẫn đọc từ DB.
- `CreatedBy` / `ApprovedBy` trên phiếu lưu **email** từ `X-User-Email`.
