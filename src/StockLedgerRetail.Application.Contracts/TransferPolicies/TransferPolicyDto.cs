namespace StockLedgerRetail.TransferPolicies;

public class TransferPolicyDto
{
    public Guid Id { get; set; }

    public Guid? SourceBrandId { get; set; }

    public string? SourceBrandName { get; set; }

    public Guid? DestinationBrandId { get; set; }

    public string? DestinationBrandName { get; set; }

    public bool AllowCrossBrand { get; set; }

    public bool IsActive { get; set; }

    public string? Note { get; set; }
}

public class CreateTransferPolicyDto
{
    public Guid? SourceBrandId { get; set; }

    public Guid? DestinationBrandId { get; set; }

    public bool AllowCrossBrand { get; set; } = true;

    public string? Note { get; set; }
}

public class UpdateTransferPolicyDto
{
    public bool AllowCrossBrand { get; set; }

    public bool IsActive { get; set; }

    public string? Note { get; set; }
}
