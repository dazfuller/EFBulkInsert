using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using EFBulkInsert.Model;

namespace EFBulkInsert
{
    public class ExampleContext : DbContext
    {
        public IDbSet<Example> Example { get; set; }

        public ExampleContext()
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }
    }

    public static class DbContextExtensions
    {
        public static string GetTableName<T>(this DbContext context) where T : class
        {
            var workspace = ((IObjectContextAdapter) context).ObjectContext.MetadataWorkspace;
            var mappingItemCollection = (StorageMappingItemCollection) workspace.GetItemCollection(DataSpace.CSSpace);
            var storeContainer = ((EntityContainerMapping) mappingItemCollection[0]).StoreEntityContainer;
            var baseEntitySet = storeContainer.BaseEntitySets.Single(es => es.Name == typeof (T).Name);

            return String.Format("{0}.{1}", baseEntitySet.Schema, baseEntitySet.Table);
        }

        public static IEnumerable<Tuple<string, Type>> GetColumns<T>(this DbContext context) where T : class
        {
            var objectContext = ((IObjectContextAdapter)context).ObjectContext;
            
            var storageMetadata =
                ((EntityConnection) objectContext.Connection).GetMetadataWorkspace().GetItems(DataSpace.SSpace);
            
            var entityProperties = storageMetadata
                .Where(s => s.BuiltInTypeKind == BuiltInTypeKind.EntityType)
                .Select(s => s as EntityType);

            var tableProperties = entityProperties.Single(ep => ep.Name == typeof (T).Name).Properties;

            return tableProperties.Select(p => new Tuple<string, Type>(p.Name, p.UnderlyingPrimitiveType.ClrEquivalentType));
        }
    }
}
