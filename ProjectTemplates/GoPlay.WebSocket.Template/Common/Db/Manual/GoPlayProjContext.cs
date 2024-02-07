using System.Drawing;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Console = Colorful.Console;

namespace GoPlayProj.Database;

public partial class GoPlayProjContext
{
    private static readonly string[] ignores = new string[]{
        /* 忽略同步blockchain的transaction到数据库的相关sql */
        @"FROM `transaction` AS `t`
      WHERE (`t`.`to_address` = @__address_0) OR (`t`.`from_address` = @__address_0)
      ORDER BY `t`.`timestamp` DESC
      LIMIT 1",
        @"FROM `transaction` AS `t`
      WHERE (`t`.`to_address` = @__address_0) OR (`t`.`from_address` = @__address_0)
      ORDER BY `t`.`block_number` DESC
      LIMIT 1",
        @"FROM `sys_wallet` AS `s`
      WHERE (`s`.`enabled` AND (`s`.`pay_direction_type` = 0)) AND (`s`.`chain_type` = 1)
      ORDER BY `s`.`api_key`",
        @"FROM `sys_wallet` AS `s`
      WHERE (`s`.`enabled` AND (`s`.`pay_direction_type` = 0)) AND (`s`.`chain_type` = 2)
      ORDER BY `s`.`api_key`",
        @"FROM `user_order` AS `u`
      WHERE ((`u`.`chain_type` = 2) AND (`u`.`status` = 0)) AND (`u`.`expire_time` >= UTC_TIMESTAMP())",
        @"FROM `user_order` AS `u`
      WHERE ((`u`.`chain_type` = 1) AND (`u`.`status` = 0)) AND (`u`.`expire_time` >= UTC_TIMESTAMP())",
        @"FROM `balance_gathering` AS `b`
      WHERE `b`.`gather_time` IS NULL AND (`b`.`chain_type` = @__chainType_0)",
        @"FROM `sys_wallet` AS `s`
      WHERE (`s`.`chain_type` = @__chainType_0) AND (`s`.`wallet_type` = ",
    };
    
    public static void Log(string content)
    {
        try
        {
            if (ignores.Any(content.Contains)) return;
            Console.WriteLineFormatted(content, Color.Aqua);
        }
        catch (Exception err)
        {
            Console.Error.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] GoPlayProjContext.Log: Exception:\n{err}");
        }
    }
    
    public static bool LogFilter(Microsoft.Extensions.Logging.EventId id, LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Warning:
            case LogLevel.None:
                return false;
            case LogLevel.Error:
            case LogLevel.Critical:
            case LogLevel.Information:
                return true;
            default:
                return false;
        }
    }
}