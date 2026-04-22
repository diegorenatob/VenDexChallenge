using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendSys.Infrastructure.Migrations
{
    public partial class AddStoredProcedures : Migration
    {
        private const string SaveDexMeterSql = """
            CREATE PROCEDURE [dbo].[SaveDEXMeter]
                @Machine              NVARCHAR(1),
                @DEXDateTime          DATETIME2,
                @MachineSerialNumber  NVARCHAR(50),
                @ValueOfPaidVends     DECIMAL(10,2),
                @DexMeterId           INT OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;

                MERGE [dbo].[DEXMeter] AS target
                USING (SELECT @Machine AS [Machine], @DEXDateTime AS [DEXDateTime]) AS source
                    ON target.[Machine] = source.[Machine]
                   AND target.[DEXDateTime] = source.[DEXDateTime]
                WHEN MATCHED THEN
                    UPDATE SET
                        [MachineSerialNumber] = @MachineSerialNumber,
                        [ValueOfPaidVends]    = @ValueOfPaidVends
                WHEN NOT MATCHED THEN
                    INSERT ([Machine], [DEXDateTime], [MachineSerialNumber], [ValueOfPaidVends])
                    VALUES (@Machine, @DEXDateTime, @MachineSerialNumber, @ValueOfPaidVends);

                SELECT @DexMeterId = [DexMeterId]
                FROM   [dbo].[DEXMeter]
                WHERE  [Machine]     = @Machine
                  AND  [DEXDateTime] = @DEXDateTime;
            END;
            """;

        private const string SaveDexLaneMeterSql = """
            CREATE PROCEDURE [dbo].[SaveDEXLaneMeter]
                @DexMeterId         INT,
                @ProductIdentifier  NVARCHAR(50),
                @Price              DECIMAL(10,2),
                @NumberOfVends      INT,
                @ValueOfPaidSales   DECIMAL(10,2)
            AS
            BEGIN
                SET NOCOUNT ON;

                INSERT INTO [dbo].[DEXLaneMeter]
                    ([DexMeterId], [ProductIdentifier], [Price], [NumberOfVends], [ValueOfPaidSales])
                VALUES
                    (@DexMeterId, @ProductIdentifier, @Price, @NumberOfVends, @ValueOfPaidSales);
            END;
            """;

        private const string ClearAllDataSql = """
            CREATE PROCEDURE [dbo].[ClearAllData]
            AS
            BEGIN
                SET NOCOUNT ON;

                DELETE FROM [dbo].[DEXLaneMeter];
                DBCC CHECKIDENT ('[dbo].[DEXLaneMeter]', RESEED, 0);

                DELETE FROM [dbo].[DEXMeter];
                DBCC CHECKIDENT ('[dbo].[DEXMeter]', RESEED, 0);
            END;
            """;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(SaveDexMeterSql);
            migrationBuilder.Sql(SaveDexLaneMeterSql);
            migrationBuilder.Sql(ClearAllDataSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[ClearAllData];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SaveDEXLaneMeter];");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS [dbo].[SaveDEXMeter];");
        }
    }
}
