namespace NFive.Queue.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class Init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.QueuePlayers",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Status = c.Int(nullable: false),
                        JoinTime = c.DateTime(nullable: false, precision: 0),
                        JoinCount = c.Short(nullable: false),
                        Priority = c.Short(nullable: false),
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
