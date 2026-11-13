using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentSummaryStoredProcedure : Migration
    {
        /// <inheritdoc />
        // A hand-written migration: EF can't generate stored procedures, so we write the SQL ourselves.
        // Called from ReportRepository with Dapper (the way SafeSpace.SeniorLiving.Api calls its procs).
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.usp_GetDepartmentSummary
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        Department,
                        COUNT(*)                          AS EmployeeCount,
                        CAST(AVG(Salary) AS decimal(18,2)) AS AverageSalary,
                        MAX(DateOfJoining)                AS LatestJoiningDate
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
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetDepartmentSummary;");
        }
    }
}
