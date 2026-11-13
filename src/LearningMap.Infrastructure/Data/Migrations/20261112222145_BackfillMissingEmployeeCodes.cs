using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningMap.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMissingEmployeeCodes : Migration
    {
        /// <inheritdoc />
        // A DATA migration (no schema change): rows inserted while EmployeeCode defaulted to ""
        // got an empty code. Give each of them a real code from the same sequence.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE dbo.Employees
                SET EmployeeCode = 'EMP-' + RIGHT('00000' + CAST(NEXT VALUE FOR dbo.EmployeeCodeSequence AS varchar(10)), 5)
                WHERE EmployeeCode = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: the old empty codes were a bug.
        }
    }
}
