namespace EFBulkInsert.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateToDateTime : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Example", "LastModified", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Example", "LastModified", c => c.DateTimeOffset(nullable: false, precision: 7));
        }
    }
}
