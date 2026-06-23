using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockLedgerRetail.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class ApplyInventoryConcurrencySql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS "AddressLine" character varying(300);
                ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS "Ward" character varying(100);
                ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS "District" character varying(100);
                ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS "Province" character varying(100);
                ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS "PostalCode" character varying(20);
                ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS "Phone" character varying(30);
                ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS "ContactName" character varying(150);
                ALTER TABLE warehouses ADD COLUMN IF NOT EXISTS "FullAddress" character varying(1000);
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'ck_current_stocks_on_hand_non_negative') THEN
                        ALTER TABLE current_stocks
                            ADD CONSTRAINT ck_current_stocks_on_hand_non_negative
                            CHECK ("QuantityOnHand" >= 0);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'ck_current_stocks_reserved_non_negative') THEN
                        ALTER TABLE current_stocks
                            ADD CONSTRAINT ck_current_stocks_reserved_non_negative
                            CHECK ("QuantityReserved" >= 0);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'ck_current_stocks_reserved_lte_on_hand') THEN
                        ALTER TABLE current_stocks
                            ADD CONSTRAINT ck_current_stocks_reserved_lte_on_hand
                            CHECK ("QuantityReserved" <= "QuantityOnHand");
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE current_stocks DROP CONSTRAINT IF EXISTS ck_current_stocks_reserved_lte_on_hand;
                ALTER TABLE current_stocks DROP CONSTRAINT IF EXISTS ck_current_stocks_reserved_non_negative;
                ALTER TABLE current_stocks DROP CONSTRAINT IF EXISTS ck_current_stocks_on_hand_non_negative;
                """);
        }
    }
}
