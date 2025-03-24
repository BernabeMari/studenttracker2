using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentTracker.Migrations
{
    public partial class UpdateLocationTracking : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocationTrackings_Users_StudentId",
                table: "LocationTrackings");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentParentConnections_Users_ParentId",
                table: "StudentParentConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentParentConnections_Users_StudentId",
                table: "StudentParentConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_TrackingSessions_Users_StudentId",
                table: "TrackingSessions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "LocationTrackings");

            migrationBuilder.RenameColumn(
                name: "TrackingDateTime",
                table: "LocationTrackings",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "StudentId",
                table: "LocationTrackings",
                newName: "SessionId");

            migrationBuilder.RenameColumn(
                name: "TrackingId",
                table: "LocationTrackings",
                newName: "LocationId");

            migrationBuilder.RenameIndex(
                name: "IX_LocationTrackings_StudentId",
                table: "LocationTrackings",
                newName: "IX_LocationTrackings_SessionId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "LocationTrackings",
                type: "decimal(11,8)",
                precision: 11,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "LocationTrackings",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    ParentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parents", x => x.ParentId);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.StudentId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parents_Email",
                table: "Parents",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parents_Username",
                table: "Parents",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Email",
                table: "Students",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Username",
                table: "Students",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationTrackings_TrackingSessions_SessionId",
                table: "LocationTrackings",
                column: "SessionId",
                principalTable: "TrackingSessions",
                principalColumn: "SessionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentParentConnections_Parents_ParentId",
                table: "StudentParentConnections",
                column: "ParentId",
                principalTable: "Parents",
                principalColumn: "ParentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentParentConnections_Students_StudentId",
                table: "StudentParentConnections",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackingSessions_Students_StudentId",
                table: "TrackingSessions",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "StudentId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LocationTrackings_TrackingSessions_SessionId",
                table: "LocationTrackings");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentParentConnections_Parents_ParentId",
                table: "StudentParentConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentParentConnections_Students_StudentId",
                table: "StudentParentConnections");

            migrationBuilder.DropForeignKey(
                name: "FK_TrackingSessions_Students_StudentId",
                table: "TrackingSessions");

            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "LocationTrackings",
                newName: "TrackingDateTime");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "LocationTrackings",
                newName: "StudentId");

            migrationBuilder.RenameColumn(
                name: "LocationId",
                table: "LocationTrackings",
                newName: "TrackingId");

            migrationBuilder.RenameIndex(
                name: "IX_LocationTrackings_SessionId",
                table: "LocationTrackings",
                newName: "IX_LocationTrackings_StudentId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "LocationTrackings",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(11,8)",
                oldPrecision: 11,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "LocationTrackings",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,8)",
                oldPrecision: 10,
                oldScale: 8);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "LocationTrackings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_LocationTrackings_Users_StudentId",
                table: "LocationTrackings",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentParentConnections_Users_ParentId",
                table: "StudentParentConnections",
                column: "ParentId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentParentConnections_Users_StudentId",
                table: "StudentParentConnections",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrackingSessions_Users_StudentId",
                table: "TrackingSessions",
                column: "StudentId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
