using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils.SqliteTool;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using SqlSugar;

namespace AntiRain.DatabaseUtils.Helpers.AuctionDB
{
    internal abstract class BaseAuctionDBHelper
    {
        #region 属性
        protected GroupMessageEventArgs GuildEventArgs { set; get; }
        protected string DBPath { get; set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 基类构造函数
        /// </summary>
        /// <param name="eventArgs">群聊事件参数</param>
        protected BaseAuctionDBHelper(GroupMessageEventArgs eventArgs)
        {
            GuildEventArgs = eventArgs;
            DBPath = SugarUtils.GetDBPath(eventArgs.LoginUid.ToString());
        }
        #endregion

        #region 通用查询函数
        /// <summary>
        /// 检查拍卖是否存在
        /// </summary>
        /// <returns>
        /// <para><see langword="1"/> 拍卖会会存在</para>
        /// <para><see langword="0"/> 拍卖会不存在</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int AuctionExists()
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<AuctionInfo>().Where(auction => auction.Aid == GuildEventArgs.SourceGroup.Id).Any()
                    ? 1
                    : 0;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error", ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 获取拍卖会名
        /// </summary>
        /// <param name="groupid"></param>
        /// <returns>
        /// <para>拍卖会名</para>
        /// <para><see langword="空字符串"/> 拍卖会不存在</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public string GetAuctionName(long groupid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                var data = dbClient.Queryable<AuctionInfo>().Where(i => i.Aid == groupid);
                if (data.Any())
                {
                    return data.First().AuctionName;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error", ConsoleLog.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 获取拍卖会成员数
        /// </summary>
        /// <param name="aid">拍卖会群号</param>
        /// <returns>
        /// <para>成员数</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int GetMemberCount(long aid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<AuctionMemberInfo>().Where(member => member.Aid == aid).Count();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error", ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 检查拍卖会是否有这个成员
        /// </summary>
        /// <param name="qid">QQ号</param>
        /// <returns>
        /// <para><see langword="1"/> 存在</para>
        /// <para><see langword="0"/> 不存在</para>
        /// <para><see langword="-1"/> 数据库错误</para>
        /// </returns>
        public int CheckMemberExists(long qid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<AuctionMemberInfo>()
                               .Where(i => i.Qid == qid && i.Aid == GuildEventArgs.SourceGroup.Id)
                               .Any() ? 1 : 0;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error", ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }
        /// <summary>
        /// 获取成员信息
        /// </summary>
        /// <param name="qid"></param>
        /// <returns>
        /// <para>成员信息</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public AuctionMemberInfo GetMemberInfo(long qid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<AuctionMemberInfo>()
                               .Where(i => i.Qid == qid && i.Aid == GuildEventArgs.SourceGroup.Id)
                               .First();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error", ConsoleLog.ErrorLogBuilder(e));
                return null;
            }
        }

        /// <summary>
        /// 获取所有成员信息
        /// </summary>
        /// <param name="aid">群号</param>
        /// <returns>
        /// <para>成员信息</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public List<AuctionMemberInfo> GetAllMembersInfo(long aid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<AuctionMemberInfo>()
                               .Where(i => i.Aid == aid)
                               .ToList();
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error", ConsoleLog.ErrorLogBuilder(e));
                return null;
            }
        }
        /// <summary>
        /// 获取拍卖会信息
        /// </summary>
        /// <param name="gid"></param>
        /// <returns>
        /// <para>成员信息</para>
        /// <para><see langword="null"/> 数据库错误</para>
        /// </returns>
        public AuctionInfo GetAuctionInfo(long gid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                return dbClient.Queryable<AuctionInfo>()
                               .InSingle(GuildEventArgs.SourceGroup.Id); //单主键查询
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database error", ConsoleLog.ErrorLogBuilder(e));
                return null;
            }
        }
        #endregion
    }
}
