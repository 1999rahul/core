using System.Reflection;

namespace UnitOfWorks.Helper
{
    public static class TableNameHelper
    {
        private static Dictionary<Type, string> tableNameCache = new();

        public static string GetTableName<T>()
        {
            var type = typeof(T);

            if (!tableNameCache.TryGetValue(type, out var tableName))
            {
                var attribute = type.GetCustomAttribute<TableNameAttribute>();
                tableName = attribute?.Name ?? $"{type.Name}s";
                tableNameCache[type] = tableName;
            }

            return tableName;
        }
    }
}
