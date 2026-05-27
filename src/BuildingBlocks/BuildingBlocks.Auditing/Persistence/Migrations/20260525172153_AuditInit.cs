using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingBlocks.Auditing.Persistence.Migrations;

/// <inheritdoc />
public partial class AuditInit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "audit");

        migrationBuilder.CreateTable(
            name: "audit_logs",
            schema: "audit",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                action = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                service_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                method_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                parameters = table.Column<string>(type: "jsonb", nullable: true),
                return_value = table.Column<string>(type: "jsonb", nullable: true),
                http_method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                http_status_code = table.Column<int>(type: "integer", nullable: true),
                client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                client_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                browser_info = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                user_id = table.Column<Guid>(type: "uuid", nullable: true),
                user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                impersonator_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                execution_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                execution_duration_ms = table.Column<int>(type: "integer", nullable: false),
                exception = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_audit_logs", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "entity_changes",
            schema: "audit",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                audit_log_id = table.Column<Guid>(type: "uuid", nullable: false),
                change_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                change_type = table.Column<short>(type: "smallint", nullable: false),
                entity_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                entity_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                entity_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                module = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_entity_changes", x => x.id);
                table.ForeignKey(
                    name: "fk_entity_changes_audit_logs_audit_log_id",
                    column: x => x.audit_log_id,
                    principalSchema: "audit",
                    principalTable: "audit_logs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "entity_property_changes",
            schema: "audit",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                entity_change_id = table.Column<Guid>(type: "uuid", nullable: false),
                property_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                property_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                original_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                new_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_entity_property_changes", x => x.id);
                table.ForeignKey(
                    name: "fk_entity_property_changes_entity_changes_entity_change_id",
                    column: x => x.entity_change_id,
                    principalSchema: "audit",
                    principalTable: "entity_changes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_audit_logs_action",
            schema: "audit",
            table: "audit_logs",
            column: "action");

        migrationBuilder.CreateIndex(
            name: "ix_audit_logs_correlation_id",
            schema: "audit",
            table: "audit_logs",
            column: "correlation_id");

        migrationBuilder.CreateIndex(
            name: "ix_audit_logs_execution_time",
            schema: "audit",
            table: "audit_logs",
            column: "execution_time");

        migrationBuilder.CreateIndex(
            name: "ix_audit_logs_user_id_execution_time",
            schema: "audit",
            table: "audit_logs",
            columns: new[] { "user_id", "execution_time" });

        migrationBuilder.CreateIndex(
            name: "ix_entity_changes_audit_log_id",
            schema: "audit",
            table: "entity_changes",
            column: "audit_log_id");

        migrationBuilder.CreateIndex(
            name: "ix_entity_changes_change_time",
            schema: "audit",
            table: "entity_changes",
            column: "change_time");

        migrationBuilder.CreateIndex(
            name: "ix_entity_changes_entity_type_entity_id",
            schema: "audit",
            table: "entity_changes",
            columns: new[] { "entity_type", "entity_id" });

        migrationBuilder.CreateIndex(
            name: "ix_entity_property_changes_entity_change_id",
            schema: "audit",
            table: "entity_property_changes",
            column: "entity_change_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "entity_property_changes",
            schema: "audit");

        migrationBuilder.DropTable(
            name: "entity_changes",
            schema: "audit");

        migrationBuilder.DropTable(
            name: "audit_logs",
            schema: "audit");
    }
}
