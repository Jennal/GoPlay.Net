// using FreeSql;
// using Microsoft.Extensions.DependencyInjection;
// using MySqlConnector;
//
// namespace GoPlayProj.ServiceProviders;
//
// public static class FreeSqlServiceProvider
// {
//     public static IServiceCollection AddFreeSql(this IServiceCollection services, string connectionString)
//     {
//         services.AddSingleton<IFreeSql>(r =>
//         {
//             IFreeSql fsql = new FreeSql.FreeSqlBuilder()
//                 .UseConnectionString(FreeSql.DataType.MySql, connectionString)
//                 .CreateDatabaseIfNotExistsMySql(connectionString)
//                 //Automatically synchronize the entity structure to the database.
//                 //FreeSql will not scan the assembly, and will generate a table if and only when the CRUD instruction is executed.
// #if DEBUG
//                 .UseAutoSyncStructure(true)
// #endif
//                 .UseMonitorCommand(cmd => Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][FreeSQL] {cmd.CommandText}"))
//                 .Build();
//             return fsql;
//         });
//         return services;
//     }
//     
//     public static FreeSqlBuilder CreateDatabaseIfNotExistsMySql(this FreeSqlBuilder @this, string connectionString)
//     {
//         var builder = new MySqlConnectionStringBuilder(connectionString);
//         var createDatabaseSql = $"USE mysql;CREATE DATABASE IF NOT EXISTS `{builder.Database}` CHARACTER SET '{builder.CharacterSet}' COLLATE 'utf8mb4_general_ci'";
//         using var cnn = new MySqlConnection($"Data Source={builder.Server};Port={builder.Port};User ID={builder.UserID};Password={builder.Password};Initial Catalog=mysql;Charset=utf8;SslMode=none;Max pool size=1");
//         cnn.Open();
//         
//         using var cmd = cnn.CreateCommand();
//         cmd.CommandText = createDatabaseSql;
//         cmd.ExecuteNonQuery();
//
//         return @this;
//     }
// }