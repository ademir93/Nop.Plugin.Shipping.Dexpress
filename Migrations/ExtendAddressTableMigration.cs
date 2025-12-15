using FluentMigrator;
using Nop.Core.Domain.Common;
using Nop.Data.Migrations;

namespace Nop.Plugin.Shipping.Dexpress.Migrations;

[NopMigration("2025/12/15 09:00:00:0000000", "Shipping.Dexpress extend Address table", MigrationProcessType.Installation)]
public class ExtendAddressTableMigration : AutoReversingMigration
{
    public override void Up()
    {
        if (!Schema.Table(nameof(Address)).Column("MunicipalityId").Exists())
        {
            Alter.Table(nameof(Address))
                .AddColumn("MunicipalityId").AsInt32().Nullable();
        }

        if (!Schema.Table(nameof(Address)).Column("TownId").Exists())
        {
            Alter.Table(nameof(Address))
                .AddColumn("TownId").AsInt32().Nullable();
        }

        if (!Schema.Table(nameof(Address)).Column("StreetId").Exists())
        {
            Alter.Table(nameof(Address))
                .AddColumn("StreetId").AsInt32().Nullable();
        }
    }
}