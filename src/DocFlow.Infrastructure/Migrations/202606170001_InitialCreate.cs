using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocFlow.Infrastructure.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "documents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OriginalFileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                StoredFileName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                Checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                FailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                RetryCount = table.Column<int>(type: "integer", nullable: false),
                MaxRetryCount = table.Column<int>(type: "integer", nullable: false),
                ExtractedTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                ExtractedTextPreview = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                PageCount = table.Column<int>(type: "integer", nullable: true),
                MetadataJson = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_documents", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "document_processing_history",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                FromStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                ToStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Action = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_processing_history", x => x.Id);
                table.ForeignKey(
                    name: "FK_document_processing_history_documents_DocumentId",
                    column: x => x.DocumentId,
                    principalTable: "documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_document_processing_history_DocumentId",
            table: "document_processing_history",
            column: "DocumentId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "document_processing_history");
        migrationBuilder.DropTable(name: "documents");
    }
}
