namespace StockLedgerRetail;

/// <summary>Mã lỗi nghiệp vụ ổn định — frontend map sang thông báo i18n.</summary>
public static class BusinessErrorCodes
{
    public const string CannotApproveDocument = "AUTH_CANNOT_APPROVE_DOCUMENT";
    public const string CannotApproveGoodsReceipt = "AUTH_CANNOT_APPROVE_GOODS_RECEIPT";
    public const string CannotManageDocument = "AUTH_CANNOT_MANAGE_DOCUMENT";
    public const string CannotReceiveTransfer = "AUTH_CANNOT_RECEIVE_TRANSFER";
    public const string HighValueSubmitRequired = "WORKFLOW_HIGH_VALUE_SUBMIT_REQUIRED";

    public static string MissingPermission(string permissionCode) =>
        $"AUTH_MISSING_PERMISSION:{permissionCode}";
}
