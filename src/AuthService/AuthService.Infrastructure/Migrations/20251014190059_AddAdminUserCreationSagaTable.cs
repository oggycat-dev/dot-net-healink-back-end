using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminUserCreationSagaTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUserCreationSagaStates",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuthUserCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserProfileUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    AuthUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsFailed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUserCreationSagaStates", x => x.CorrelationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUserCreationSagaStates_AuthUserId",
                table: "AdminUserCreationSagaStates",
                column: "AuthUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUserCreationSagaStates_CompletedAt",
                table: "AdminUserCreationSagaStates",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUserCreationSagaStates_CreatedAt",
                table: "AdminUserCreationSagaStates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUserCreationSagaStates_CurrentState",
                table: "AdminUserCreationSagaStates",
                column: "CurrentState");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUserCreationSagaStates_Email",
                table: "AdminUserCreationSagaStates",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUserCreationSagaStates_Email_State_Created",
                table: "AdminUserCreationSagaStates",
                columns: new[] { "Email", "CurrentState", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUserCreationSagaStates_StartedAt",
                table: "AdminUserCreationSagaStates",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUserCreationSagaStates_Status",
                table: "AdminUserCreationSagaStates",
                columns: new[] { "IsCompleted", "IsFailed" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUserCreationSagaStates_UserProfileId",
                table: "AdminUserCreationSagaStates",
                column: "UserProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUserCreationSagaStates");
        }
    }
}
