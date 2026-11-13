using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeCodeAndImageInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateSequence<int>(
                name: "EmployeeCodeSequence",
                schema: "dbo");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                table: "Employees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'EMP-' + RIGHT('00000' + CAST(NEXT VALUE FOR dbo.EmployeeCodeSequence AS varchar(10)), 5)");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ImageUploadedDate",
                table: "Employees",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees",
                column: "EmployeeCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ImageUploadedDate",
                table: "Employees");

            migrationBuilder.DropSequence(
                name: "EmployeeCodeSequence",
                schema: "dbo");
        }
    }
}
