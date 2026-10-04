using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGames.Migrations.Migrations;

/// <inheritdoc />
public partial class _20261004015113_FusionV15OperationsSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ItemsJson",
            table: "_Operations");

        migrationBuilder.RenameColumn(
            name: "NestedOperations",
            table: "_Operations",
            newName: "InvalidationCallsJson");

        migrationBuilder.AlterColumn<string>(
            name: "CommandJson",
            table: "_Operations",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AddColumn<byte[]>(
            name: "CommandData",
            table: "_Operations",
            type: "bytea",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "InvalidationCallsData",
            table: "_Operations",
            type: "bytea",
            nullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "ValueJson",
            table: "_Events",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AddColumn<byte[]>(
            name: "ValueData",
            table: "_Events",
            type: "bytea",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CommandData",
            table: "_Operations");

        migrationBuilder.DropColumn(
            name: "InvalidationCallsData",
            table: "_Operations");

        migrationBuilder.DropColumn(
            name: "ValueData",
            table: "_Events");

        migrationBuilder.RenameColumn(
            name: "InvalidationCallsJson",
            table: "_Operations",
            newName: "NestedOperations");

        migrationBuilder.AlterColumn<string>(
            name: "CommandJson",
            table: "_Operations",
            type: "text",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ItemsJson",
            table: "_Operations",
            type: "text",
            nullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "ValueJson",
            table: "_Events",
            type: "text",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }
}
