using System;
using System.IO;
using System.Threading;
using AntiRain.DatabaseUtils.SqliteTool;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using SqlSugar;

namespace AntiRain.DatabaseUtils
{
    internal static class DatabaseInit//数据库初始化类
    {
        /// <summary>
        /// 初始化数据库
        /// </summary>
        /// <param name="eventArgs">CQAppEnableEventArgs</param>
        public static void Init(ConnectEventArgs eventArgs)
        {
            string DBPath = SugarUtils.GetDBPath(eventArgs.LoginUid.ToString());
            ConsoleLog.Debug("IO",$"获取数据路径{DBPath}");
            if (!File.Exists(DBPath))//查找数据文件
            {
                //数据库文件不存在，新建数据库
                ConsoleLog.Warning("数据库初始化", "未找到数据库文件，创建新的数据库");
                Directory.CreateDirectory(Path.GetPathRoot(DBPath) ?? String.Empty);
                File.Create(DBPath).Close();
            }
            SqlSugarClient dbClient = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString      = $"DATA SOURCE={DBPath}",
                DbType                = DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType           = InitKeyType.Attribute
            });
            try
            {
                if (!SugarUtils.TableExists<SuiseiData>(dbClient)) //彗酱数据库初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到慧酱数据表 - 创建一个新表");
                    SugarUtils.CreateTable<SuiseiData>(dbClient);
                }
                if (!SugarUtils.TableExists<MemberInfo>(dbClient)) //成员状态表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到成员状态表 - 创建一个新表");
                    SugarUtils.CreateTable<MemberInfo>(dbClient);
                }
                if (!SugarUtils.TableExists<BiliSubscription>(dbClient)) //动态记录表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到动态记录表 - 创建一个新表");
                    SugarUtils.CreateTable<BiliSubscription>(dbClient);
                }
                if (!SugarUtils.TableExists<GuildBattleBoss>(dbClient)) //会战数据表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到会战数据表 - 创建一个新表");
                    SugarUtils.CreateTable<GuildBattleBoss>(dbClient);
                    //写入初始化数据
                    dbClient.Insertable(GuildBattleBoss.GetInitBossInfos()).ExecuteCommand();
                }
                if (!SugarUtils.TableExists<GuildInfo>(dbClient)) //会战状态表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到会战状态表 - 创建一个新表");
                    SugarUtils.CreateTable<GuildInfo>(dbClient);
                }
                if (!SugarUtils.TableExists<AuctionInfo>(dbClient)) //拍卖会表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到拍卖会表 - 创建一个新表");
                    SugarUtils.CreateTable<AuctionInfo>(dbClient);
                }
                if (!SugarUtils.TableExists<AuctionGroupInfo>(dbClient)) //拍卖小组表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到拍卖小组表 - 创建一个新表");
                    SugarUtils.CreateTable<AuctionGroupInfo>(dbClient);
                }
                if (!SugarUtils.TableExists<AuctionMemberInfo>(dbClient)) //拍卖小组成员表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到拍卖小组成员表 - 创建一个新表");
                    SugarUtils.CreateTable<AuctionMemberInfo>(dbClient);
                }
                if (!SugarUtils.TableExists<AuctionLot>(dbClient)) //拍卖品组表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到拍卖品组表 - 创建一个新表");
                    SugarUtils.CreateTable<AuctionLot>(dbClient);
                }
                if (!SugarUtils.TableExists<AuctionBid>(dbClient)) //拍卖出价表的初始化
                {
                    ConsoleLog.Warning("数据库初始化", "未找到拍卖出价表 - 创建一个新表");
                    SugarUtils.CreateTable<AuctionBid>(dbClient);
                }
            }
            catch (Exception exception)
            {
                ConsoleLog.Fatal("数据库初始化错误",ConsoleLog.ErrorLogBuilder(exception));
                Thread.Sleep(5000);
                throw;
            }
        }
    }
}
