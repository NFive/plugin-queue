// <auto-generated />
// ReSharper disable all

using System;
using System.Data.Entity.Migrations;
using System.CodeDom.Compiler;
using System.Data.Entity.Migrations.Infrastructure;

namespace NFive.Queue.Server.Migrations
{
    [GeneratedCode("NFive.Migration", "0.3 Alpha Build 148")]
    public class Init : DbMigration, IMigrationMetadata
    {
        string IMigrationMetadata.Id => "201903222351423_Init";
        
        string IMigrationMetadata.Source => null;
        
        string IMigrationMetadata.Target => "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";
        
        public override void Up()
        {
            CreateTable(
                "dbo.QueuePlayers",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Priority = c.Int(nullable: false),
                        Position = c.Short(nullable: false),
                        SessionId = c.Guid(nullable: false),
                        Created = c.DateTime(nullable: false, precision: 0),
                        Deleted = c.DateTime(precision: 0),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Sessions", t => t.SessionId, cascadeDelete: true)
                .Index(t => t.SessionId);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.QueuePlayers", "SessionId", "dbo.Sessions");
            DropIndex("dbo.QueuePlayers", new[] { "SessionId" });
            DropTable("dbo.QueuePlayers");
        }
    }
}
