using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContentService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    ContentStatus = table.Column<int>(type: "integer", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    EmotionCategories = table.Column<string>(type: "text", nullable: true),
                    TopicCategories = table.Column<string>(type: "text", nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LikeCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ShareCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CommentCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AverageRating = table.Column<double>(type: "double precision", nullable: true),
                    ContentData = table.Column<string>(type: "text", nullable: true),
                    Discriminator = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    StoryContent = table.Column<string>(type: "text", nullable: true),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    AuthorDisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HelpfulCount = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    IsModeratorPick = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    TriggerWarnings = table.Column<string>(type: "text", nullable: true),
                    Question = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Answer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Explanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    DifficultyLevel = table.Column<int>(type: "integer", nullable: true, defaultValue: 1),
                    RelatedPodcastIds = table.Column<string>(type: "text", nullable: true),
                    LetterContent = table.Column<string>(type: "text", nullable: true),
                    ScheduledDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDelivered = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    Mood = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Intentions = table.Column<string>(type: "text", nullable: true),
                    AudioUrl = table.Column<string>(type: "text", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    TranscriptUrl = table.Column<string>(type: "text", nullable: true),
                    HostName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GuestName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EpisodeNumber = table.Column<int>(type: "integer", nullable: true),
                    SeriesName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AverageListenTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CompletionRate = table.Column<double>(type: "double precision", nullable: true),
                    Postcard_ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QuoteText = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    QuoteAuthor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BackgroundColor = table.Column<string>(type: "text", nullable: true),
                    FontStyle = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventData = table.Column<string>(type: "text", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetryCount = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ReplyCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Comments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Comments_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InteractionType = table.Column<int>(type: "integer", nullable: false),
                    InteractionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    AdditionalData = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentInteractions_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Review = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentRatings_Contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "Contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentLikes_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentLikes_CommentId",
                table: "CommentLikes",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentLikes_UserId_CommentId",
                table: "CommentLikes",
                columns: new[] { "UserId", "CommentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ContentId",
                table: "Comments",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ContentId_IsApproved_ParentCommentId",
                table: "Comments",
                columns: new[] { "ContentId", "IsApproved", "ParentCommentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CreatedBy",
                table: "Comments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentInteractions_ContentId",
                table: "ContentInteractions",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentInteractions_UserId_ContentId_InteractionType",
                table: "ContentInteractions",
                columns: new[] { "UserId", "ContentId", "InteractionType" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentRatings_ContentId",
                table: "ContentRatings",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentRatings_UserId_ContentId",
                table: "ContentRatings",
                columns: new[] { "UserId", "ContentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contents_ContentStatus",
                table: "Contents",
                column: "ContentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Contents_ContentType",
                table: "Contents",
                column: "ContentType");

            migrationBuilder.CreateIndex(
                name: "IX_Contents_CreatedAt",
                table: "Contents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Contents_PublishedAt",
                table: "Contents",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_EventType",
                table: "OutboxEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_NextRetryAt",
                table: "OutboxEvents",
                column: "NextRetryAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_ProcessedAt",
                table: "OutboxEvents",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_ProcessedAt_NextRetryAt_RetryCount",
                table: "OutboxEvents",
                columns: new[] { "ProcessedAt", "NextRetryAt", "RetryCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommentLikes");

            migrationBuilder.DropTable(
                name: "ContentInteractions");

            migrationBuilder.DropTable(
                name: "ContentRatings");

            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Contents");
        }
    }
}
