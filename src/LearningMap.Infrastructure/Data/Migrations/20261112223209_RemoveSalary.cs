using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Salary",
                table: "Employees");

            // The stored procedure used AVG(Salary); EF doesn't track procs, so update it by hand.
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.usp_GetDepartmentSummary
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        Department,
                        COUNT(*)           AS EmployeeCount,
                        MAX(DateOfJoining) AS LatestJoiningDate
                    FROM dbo.Employees
                    WHERE IsDeleted = 0
                    GROUP BY Department
                    ORDER BY Department;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Salary",
                table: "Employees",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.usp_GetDepartmentSummary
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        Department,
                        COUNT(*)                           AS EmployeeCount,
                        CAST(AVG(Salary) AS decimal(18,2)) AS AverageSalary,
                        MAX(DateOfJoining)                 AS LatestJoiningDate
                    FROM dbo.Employees
                    WHERE IsDeleted = 0
                    GROUP BY Department
                    ORDER BY Department;
                END
                """);
        }
    }
}
