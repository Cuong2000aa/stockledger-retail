import {
  ArrowLeftRight,
  BadgeDollarSign,
  BarChart3,
  Boxes,
  Clock3,
  FileText,
  History,
  LayoutDashboard,
  Lightbulb,
  LucideIcon,
  Package,
  PackageCheck,
  ServerCog,
  Shield,
  ShoppingCart,
  Store,
  Tags,
  Truck,
  Users,
  UsersRound,
  Warehouse,
} from "lucide-react";

export type SubmenuItem = {
  href: string;
  labelKey: string;
  active?: boolean;
};

export type MenuItem = {
  href: string;
  labelKey: string;
  icon: LucideIcon;
  active?: boolean;
  submenus?: SubmenuItem[];
  adminOnly?: boolean;
};

export type MenuGroup = {
  groupLabelKey: string;
  adminOnly?: boolean;
  menus: MenuItem[];
};

export function getSidebarMenuList(
  pathname: string,
  isSystemAdmin: boolean
): MenuGroup[] {
  const isPathActive = (href: string) => {
    if (href === "/") return pathname === "/";
    return pathname.startsWith(href);
  };

  const groups: MenuGroup[] = [
    {
      groupLabelKey: "groupOverview",
      menus: [
        {
          href: "/",
          labelKey: "dashboard",
          icon: LayoutDashboard,
          active: isPathActive("/"),
        },
        {
          href: "/insights",
          labelKey: "insights",
          icon: Lightbulb,
          active: isPathActive("/insights"),
        },
        {
          href: "/reports",
          labelKey: "reports",
          icon: BarChart3,
          active: isPathActive("/reports"),
        },
      ],
    },
    {
      groupLabelKey: "groupCatalog",
      menus: [
        {
          href: "/products",
          labelKey: "catalogMenu",
          icon: Package,
          active: isPathActive("/products") || isPathActive("/product-variants"),
          submenus: [
            {
              href: "/products",
              labelKey: "products",
              active: pathname === "/products" || pathname.startsWith("/products/"),
            },
            {
              href: "/product-variants",
              labelKey: "productVariants",
              active: pathname.startsWith("/product-variants"),
            },
          ],
        },
        {
          href: "/warehouses",
          labelKey: "networkMenu",
          icon: Warehouse,
          active: isPathActive("/warehouses") || isPathActive("/suppliers"),
          submenus: [
            {
              href: "/warehouses",
              labelKey: "warehouses",
              active: isPathActive("/warehouses"),
            },
            {
              href: "/suppliers",
              labelKey: "suppliers",
              active: isPathActive("/suppliers"),
            },
          ],
        },
      ],
    },
    {
      groupLabelKey: "groupProcurement",
      menus: [
        {
          href: "/purchase-orders",
          labelKey: "procurementMenu",
          icon: ShoppingCart,
          active: isPathActive("/purchase-orders") || isPathActive("/goods-receipts"),
          submenus: [
            {
              href: "/purchase-orders",
              labelKey: "purchaseOrders",
              active: isPathActive("/purchase-orders"),
            },
            {
              href: "/goods-receipts",
              labelKey: "goodsReceipts",
              active: isPathActive("/goods-receipts"),
            },
          ],
        },
      ],
    },
    {
      groupLabelKey: "groupInventory",
      menus: [
        {
          href: "/inventory-documents",
          labelKey: "inventoryDocuments",
          icon: FileText,
          active: isPathActive("/inventory-documents"),
        },
        {
          href: "/current-stocks",
          labelKey: "currentStocks",
          icon: Boxes,
          active: isPathActive("/current-stocks"),
        },
        {
          href: "/stock-transactions",
          labelKey: "stockTransactions",
          icon: History,
          active: isPathActive("/stock-transactions"),
        },
        {
          href: "/stock-reservations",
          labelKey: "stockReservations",
          icon: Clock3,
          active: isPathActive("/stock-reservations"),
        },
      ],
    },
  ];

  if (isSystemAdmin) {
    groups.push({
      groupLabelKey: "groupAdmin",
      adminOnly: true,
      menus: [
        {
          href: "/admin/users",
          labelKey: "adminUsersMenu",
          icon: Users,
          active:
            isPathActive("/admin/users") ||
            isPathActive("/admin/teams") ||
            isPathActive("/admin/permissions"),
          submenus: [
            {
              href: "/admin/users",
              labelKey: "users",
              active: isPathActive("/admin/users"),
            },
            {
              href: "/admin/teams",
              labelKey: "teams",
              active: isPathActive("/admin/teams"),
            },
            {
              href: "/admin/permissions",
              labelKey: "permissions",
              active: isPathActive("/admin/permissions"),
            },
          ],
        },
        {
          href: "/admin/brands",
          labelKey: "adminPoliciesMenu",
          icon: Store,
          active:
            isPathActive("/admin/brands") ||
            isPathActive("/admin/transfer-policies") ||
            isPathActive("/admin/markdown-policies"),
          submenus: [
            {
              href: "/admin/brands",
              labelKey: "brands",
              active: isPathActive("/admin/brands"),
            },
            {
              href: "/admin/transfer-policies",
              labelKey: "transferPolicies",
              active: isPathActive("/admin/transfer-policies"),
            },
            {
              href: "/admin/markdown-policies",
              labelKey: "markdownPolicies",
              active: isPathActive("/admin/markdown-policies"),
            },
          ],
        },
        {
          href: "/admin/operations",
          labelKey: "adminSystemMenu",
          icon: ServerCog,
          active:
            isPathActive("/admin/operations") ||
            isPathActive("/admin/audit-logs"),
          submenus: [
            {
              href: "/admin/operations",
              labelKey: "operations",
              active: isPathActive("/admin/operations"),
            },
            {
              href: "/admin/audit-logs",
              labelKey: "auditLogs",
              active: isPathActive("/admin/audit-logs"),
            },
          ],
        },
      ],
    });
  }

  return groups;
}
