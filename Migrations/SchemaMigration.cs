using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Shipping.Dexpress.Domain;

namespace Nop.Plugin.Shipping.Dexpress.Migrations;

[NopMigration("2025/10/22 08:40:55:1687541", "Shipping.Dexpress base schema", MigrationProcessType.Installation)]
public class SchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.TableFor<Municipality>();
        Create.TableFor<Street>();
        Create.TableFor<Town>();
        Create.TableFor<DexpressOrder>();
    }
}