using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SubscriptionService.InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRegistrationSagaTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxMessage");

            migrationBuilder.DropTable(
                name: "RegistrationSagaStates");

            migrationBuilder.DropTable(
                name: "InboxState");

            migrationBuilder.DropTable(
                name: "OutboxState");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InboxState",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Consumed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiveCount = table.Column<int>(type: "integer", nullable: false),
                    Received = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxState", x => x.Id);
                    table.UniqueConstraint("AK_InboxState_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "OutboxState",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxState", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationSagaStates",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthUserCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuthUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EncryptedPassword = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExpiresInMinutes = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsFailed = table.Column<bool>(type: "boolean", nullable: false),
                    OtpCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    OtpSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OtpTimeoutTokenId = table.Column<Guid>(type: "uuid", nullable: true),
                    OtpVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserProfileCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationSagaStates", x => x.CorrelationId);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EnqueueTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FaultAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    InboxConsumerId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponseAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.SequenceNumber);
                    table.ForeignKey(
                        name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                        columns: x => new { x.InboxMessageId, x.InboxConsumerId },
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                table: "InboxState",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                table: "OutboxMessage",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_OutboxId_SequenceNumber",
                table: "OutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSagaStates_CompletedAt",
                table: "RegistrationSagaStates",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSagaStates_CreatedAt",
                table: "RegistrationSagaStates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSagaStates_CurrentState",
                table: "RegistrationSagaStates",
                column: "CurrentState");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSagaStates_Email",
                table: "RegistrationSagaStates",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSagaStates_Email_State_Created",
                table: "RegistrationSagaStates",
                columns: new[] { "Email", "CurrentState", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSagaStates_StartedAt",
                table: "RegistrationSagaStates",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationSagaStates_Status",
                table: "RegistrationSagaStates",
                columns: new[] { "IsCompleted", "IsFailed" });
        }
    }
}
