using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubscriptionService.InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsActiveColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate data: Set Status based on IsActive for PostgreSQL
            // EntityStatusEnum: Active = 1, Inactive = 2
            // Only update records where Status is invalid (0, NULL) or needs migration
            migrationBuilder.Sql(@"
                UPDATE ""SubscriptionPlans""
                SET ""Status"" = CASE 
                    WHEN ""IsActive"" = true THEN 1
                    WHEN ""IsActive"" = false THEN 2
                    ELSE 1
                END
                WHERE ""Status"" IS NULL OR ""Status"" = 0;
            ");
            
            // Now safe to drop IsActive column
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SubscriptionPlans");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SubscriptionPlans",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }
    }
}
