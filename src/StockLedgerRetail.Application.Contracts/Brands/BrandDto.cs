using StockLedgerRetail.Enums;

namespace StockLedgerRetail.Brands;

public class BrandDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public BrandStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public class CreateBrandDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public class UpdateBrandDto
{
    public string Name { get; set; } = string.Empty;

    public BrandStatus Status { get; set; }
}
