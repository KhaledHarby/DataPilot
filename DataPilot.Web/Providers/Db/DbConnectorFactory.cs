using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DataPilot.Web.Providers.Db;

public class DbConnectorFactory : IDbConnectorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<DataPilot.Web.Data.DbKind, Type> _connectorTypes;

    public DbConnectorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _connectorTypes = new Dictionary<DataPilot.Web.Data.DbKind, Type>
        {
            { DataPilot.Web.Data.DbKind.SqlServer, typeof(SqlServerConnector) },
            // Add other connectors as they are implemented
            // { DataPilot.Web.Data.DbKind.MySql, typeof(MySqlConnector) },
            // { DataPilot.Web.Data.DbKind.Oracle, typeof(OracleConnector) },
            // { DataPilot.Web.Data.DbKind.Mongo, typeof(MongoConnector) }
        };
    }

    public IDbConnector Create(DataPilot.Web.Data.DbKind kind)
    {
        if (!_connectorTypes.TryGetValue(kind, out var connectorType))
        {
            throw new NotSupportedException($"Database kind {kind} is not supported");
        }

        return (IDbConnector)ActivatorUtilities.CreateInstance(_serviceProvider, connectorType);
    }
}
