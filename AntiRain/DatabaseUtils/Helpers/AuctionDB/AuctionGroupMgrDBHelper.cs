using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils.SqliteTool;
using AntiRain.Resource.TypeEnum;
using AntiRain.Resource.TypeEnum.AuctionType;
using AntiRain.Resource.TypeEnum.GuildBattleType;
using AntiRain.Tool;
using Sora.Entities.Info;
using Sora.Enumeration.ApiEnum;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;
using SqlSugar;

namespace AntiRain.DatabaseUtils.Helpers.AuctionDB
{
    internal class AuctionGroupMgrDBHelper : BaseAuctionDBHelper
    {
        #region 构造函数
        /// <summary>
        /// 在接受到群消息时使用
        /// </summary>
        /// <param name="guildEventArgs">CQAppEnableEventArgs类</param>
        public AuctionGroupMgrDBHelper(GroupMessageEventArgs guildEventArgs) : base(guildEventArgs)
        {
        }
        #endregion

        #region 指令
        /// <summary>
        /// 移除所有成员
        /// </summary>
        /// <param name="groupid">拍卖会所在群号</param>
        /// <returns>状态值
        /// 0：正常移除
        /// 1：拍卖会不存在
        /// -1：删除时发生错误
        /// </returns>
        public int EmptyMember(long groupid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            var data = dbClient.Queryable<AuctionMemberInfo>().Where(i => i.Aid == groupid);
            if (data.Any())
            {
                if (dbClient.Deleteable<AuctionMemberInfo>().Where(i => i.Aid == groupid).ExecuteCommandHasChange())
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// 移除一名成员
        /// </summary>
        /// <param name="qqid">成员QQ号</param>
        /// <param name="groupid">成员所在群号</param>
        /// <returns>状态值
        /// 0：正常移除
        /// 1：该成员并不在拍卖会内
        /// -1：数据库出错
        /// </returns>
        public int LeaveAuction(long qqid, long groupid)
        {
            int retCode;
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            if (dbClient.Queryable<AuctionMemberInfo>().Where(i => i.Qid == qqid && i.Aid == groupid).Any())
            {
                retCode = dbClient.Deleteable<AuctionMemberInfo>().Where(i => i.Qid == qqid && i.Aid == groupid)
                                  .ExecuteCommandHasChange()
                    ? 0
                    : -1;
            }
            else
            {
                retCode = 1;
            }

            return retCode;
        }

        /// <summary>
        /// 移除小组,组员组号归为初始
        /// </summary>
        /// <param name="groupid">目标小组</param>
        /// <param name="aid">小组所在群号</param>
        /// <returns>状态值
        /// 1：正常移除
        /// 0：该小组并不在拍卖会内
        /// -1：数据库出错
        /// </returns>
        public int RemoveGroup(int groupid, long aid)
        {
            int retCode;
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            if (dbClient.Queryable<AuctionGroupInfo>().Where(i => i.Aid == aid && i.Gid == groupid).Any())
            {
                retCode = dbClient.Deleteable<AuctionGroupInfo>().Where(i => i.Aid == aid && i.Gid == groupid)
                    .ExecuteCommandHasChange()
                    ? 1
                    : -1;
            }
            else
            {
                retCode = 0;
            }

            if (retCode != 1)
            {
                return retCode;
            }

            int retCode2 = 0;
            List<AuctionMemberInfo> auctionMembers = ShowGroupMembers(aid, groupid);

            auctionMembers.ForEach(async member =>
            {
                var data = new AuctionMemberInfo()
                {
                    Qid = member.Qid,
                    Aid = aid,
                    Name = member.Name,
                    Gid = -1
                };
                retCode2 = await dbClient.Updateable(data)
                    .Where(i => i.Qid == data.Qid && i.Aid == aid)
                    .ExecuteCommandHasChangeAsync()
                    ? 1
                    : -1;
            });


            return retCode2;
        }

        /// <summary>
        /// 查询拍卖会所有成员
        /// </summary>
        /// <param name="groupid">QQ群号</param>
        /// <returns>按组号排序的成员列表</returns>
        public List<AuctionMemberInfo> ShowMembers(long groupid)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            return dbClient.Queryable<AuctionMemberInfo>()
                .Where(i => i.Aid == groupid)
                .OrderBy(i => i.Gid)
                .ToList();
        }

        /// <summary>
        /// 查询拍卖会某个小组的所有成员
        /// </summary>
        /// <param name="aid">QQ群号</param>
        /// <param name="groupId">小组号</param>
        /// <returns>按组号排序的成员列表</returns>
        public List<AuctionMemberInfo> ShowGroupMembers(long aid,int groupId)
        {
            using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
            return dbClient.Queryable<AuctionMemberInfo>()
                .Where(i => i.Aid == aid && i.Gid == groupId)
                .OrderBy(i => i.Gid)
                .ToList();
        }


        /// <summary>
        /// 添加一名成员
        /// </summary>
        /// <param name="qid">成员QQ号</param>
        /// <param name="aid">成员所在群号</param>
        /// <param name="groupid">成员所在组号</param>
        /// <returns>状态值
        /// 0：正常添加
        /// 1：该成员已存在，更新信息
        /// -1：数据库出错/API错误
        /// </returns>
        public async Task<int> JoinToAuction(long qid, long aid, long groupid = -1)
        {
            try
            {
                int retCode;
                //从API获取成员信息
                (APIStatusType apiStatus, GroupMemberInfo member) =
                    await GuildEventArgs.SourceGroup.GetGroupMemberInfo(qid);
                if (apiStatus != APIStatusType.OK) return -1;
                //读取数据库
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                if (await dbClient.Queryable<AuctionMemberInfo>().Where(i => i.Qid == qid && i.Aid == aid).AnyAsync())
                {
                    var data = new AuctionMemberInfo()
                    {
                        Qid = qid,
                        Aid = aid,
                        Name = string.IsNullOrEmpty(member.Card) ? member.Nick : member.Card,
                        Gid = groupid
                    };
                    retCode = await dbClient.Updateable(data)
                                            .Where(i => i.Qid == qid && i.Aid == aid)
                                            .ExecuteCommandHasChangeAsync()
                        ? 1
                        : -1;
                }
                else
                {
                    var memberStatus = new AuctionMemberInfo
                    {
                        Level = LevelType.Normal,
                        Aid = aid,
                        Gid = groupid,
                        Qid = qid,
                        Name = string.IsNullOrEmpty(member.Card) ? member.Nick : member.Card
                    };
                    //成员信息
                    retCode = await dbClient.Insertable(memberStatus).ExecuteCommandAsync() > 0 ? 0 : -1;
                }
                return retCode;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database", ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }



        /// <summary>
        /// 初次创建拍卖会
        /// </summary>
        /// <param name="gName">拍卖会名称</param>
        /// <param name="aId">拍卖会所在群号</param>
        /// <returns>状态值
        /// 0：正常创建
        /// 1：该群公会已存在，更新信息
        /// -1:数据库出错
        /// </returns>
        public int CreateAuction(string gName, long aId)
        {
            try
            {
                int retCode;
                AuctionLot defaultLot = GetDefaultLot(aId);
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //更新信息时不需要更新公会战信息
                if (dbClient.Queryable<AuctionInfo>().Where(i => i.Aid == aId).Any())
                {
                    var data = new AuctionInfo()
                    {
                        AuctionName = gName,
                        Aid = aId
                    };
                    retCode = dbClient.Updateable(data)
                                      .UpdateColumns(auctionInfo =>
                                                         new { auctionInfo.AuctionName, auctionInfo.Aid })
                                      .Where(i => i.Aid == aId)
                                      .ExecuteCommandHasChange()
                        ? 1
                        : -1;
                }
                else
                {
                    //会战进度表
                    var auctionStatusData = new AuctionInfo
                    {
                        Aid = aId,
                        AuctionName = gName,
                        Rid = defaultLot.Rid,
                        CurrStartBid = defaultLot.StartBid,
                        CurrRangeBid = defaultLot.RangeBid,
                        CurrBid = -1,
                        CurrBidGroup = -1,
                        InAuction = false
                    };
                    retCode = dbClient.Insertable(auctionStatusData).ExecuteCommand() > 0 ? 0 : -1;
                }

                return retCode;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database", ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 创建拍卖会小组
        /// </summary>
        /// <param name="gName">拍卖小组名称</param>
        /// <param name="aId">拍卖会所在群号</param>
        /// <returns>状态值
        /// 组号：正常创建
        /// 0：该小组已存在，更新信息
        /// -1:数据库出错
        /// </returns>
        public int CreateAuctionGroup(string gName, long aId)
        {
            try
            {
                int retCode;
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                //小组已存在则更新
                if (dbClient.Queryable<AuctionGroupInfo>().Where(i => i.Aid == aId && i.GroupName == gName).Any())
                {
                    var data = new AuctionGroupInfo()
                    {
                        GroupName = gName,
                        Aid = aId,
                    };
                    retCode = dbClient.Updateable(data)
                                      .UpdateColumns(groupInfo =>
                                                         new { groupInfo.GroupName, groupInfo.Aid })
                                      .Where(i => i.Aid == aId)
                                      .ExecuteCommandHasChange()
                        ? 0
                        : -1;
                }
                else
                {
                    //会战进度表
                    var auctionStatusData = new AuctionGroupInfo
                    {
                        GroupName = gName,
                        Aid = aId,
                        CurrValue = 0,
                        CurrAmount = 0
                    };
                    retCode = dbClient.Insertable(auctionStatusData).ExecuteReturnIdentity();
                    if (retCode < 0) retCode = -1;
                }

                return retCode;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database", ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 取消拍卖会
        /// </summary>
        /// <param name="aid">拍卖会群的群号</param>
        /// <returns>数据库是否成功运行</returns>
        public bool DeleteAuction(long aid)
        {
            try
            {
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                bool deletGuildInfo = dbClient.Deleteable<AuctionInfo>().Where(auction => auction.Aid == aid)
                                              .ExecuteCommandHasChange();
                bool deletMemberInfo = true;
                if (dbClient.Queryable<AuctionMemberInfo>().Where(member => member.Aid == aid).Count() > 0)
                {
                    deletMemberInfo = dbClient.Deleteable<AuctionMemberInfo>().Where(member => member.Aid == aid)
                                              .ExecuteCommandHasChange();
                }

                return deletMemberInfo && deletGuildInfo;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database", ConsoleLog.ErrorLogBuilder(e));
                return false;
            }
        }

        /// <summary>
        /// 添加一名成员作为小组组长
        /// </summary>
        /// <param name="qid">成员QQ号</param>
        /// <param name="aid">成员所在群号</param>
        /// <param name="groupid">成员所在组号</param>
        /// <returns>状态值
        /// 0：正常添加
        /// 1：该成员已存在，更新信息
        /// -1：数据库出错/API错误
        /// </returns>
        public async Task<int> LeaderJoinToGroup(long qid, long aid, long groupid)
        {
            try
            {
                int retCode;
                //从API获取成员信息
                (APIStatusType apiStatus, GroupMemberInfo member) =
                    await GuildEventArgs.SourceGroup.GetGroupMemberInfo(qid);
                if (apiStatus != APIStatusType.OK) return -1;
                //读取数据库
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                if (await dbClient.Queryable<AuctionMemberInfo>().Where(i => i.Qid == qid && i.Aid == aid).AnyAsync())
                {
                    var data = new AuctionMemberInfo()
                    {
                        Level = LevelType.Leader,
                        Qid = qid,
                        Aid = aid,
                        Name = string.IsNullOrEmpty(member.Card) ? member.Nick : member.Card,
                        Gid = groupid
                    };
                    retCode = await dbClient.Updateable(data)
                                            .Where(i => i.Qid == qid && i.Aid == aid)
                                            .ExecuteCommandHasChangeAsync()
                        ? 1
                        : -1;
                }
                else
                {
                    var memberStatus = new AuctionMemberInfo
                    {
                        Level = LevelType.Leader,
                        Aid = aid,
                        Gid = groupid,
                        Qid = qid,
                        Name = string.IsNullOrEmpty(member.Card) ? member.Nick : member.Card
                    };
                    //成员信息
                    retCode = await dbClient.Insertable(memberStatus).ExecuteCommandAsync() > 0 ? 0 : -1;
                }
                return retCode;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database", ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }

        /// <summary>
        /// 添加一名成员到小组
        /// </summary>
        /// <param name="qid">成员QQ号</param>
        /// <param name="aid">成员所在群号</param>
        /// <param name="groupId">成员所在组号</param>
        /// <returns>状态值
        /// 0：该成员已加入其他小组或未加入拍卖会
        /// 1：该成员已存在，更新信息
        /// -1：数据库出错/API错误
        /// </returns>
        public async Task<int> JoinToAuctionGroup(long qid, long aid, int groupId)
        {
            try
            {
                int retCode;
                //从API获取成员信息
                (APIStatusType apiStatus, GroupMemberInfo member) =
                    await GuildEventArgs.SourceGroup.GetGroupMemberInfo(qid);
                if (apiStatus != APIStatusType.OK) return -1;
                //读取数据库
                using SqlSugarClient dbClient = SugarUtils.CreateSqlSugarClient(DBPath);
                if (await dbClient.Queryable<AuctionGroupInfo>().Where(i => i.Aid == aid && i.Gid == groupId).AnyAsync())
                {
                    if (await dbClient.Queryable<AuctionMemberInfo>().Where(i => i.Aid == aid && i.Qid == qid && i.Gid == -1)
                        .AnyAsync())
                    {
                        var data = new AuctionMemberInfo()
                        {
                            Qid = qid,
                            Aid = aid,
                            Name = string.IsNullOrEmpty(member.Card) ? member.Nick : member.Card,
                            Gid = groupId
                        };
                        retCode = await dbClient.Updateable(data)
                                                .Where(i => i.Qid == qid && i.Aid == aid)
                                                .ExecuteCommandHasChangeAsync()
                            ? 1
                            : -1;
                    }
                    else
                    {
                        retCode = 0;
                    }
                }
                else
                {
                    ConsoleLog.Error("Database", "当前成员未在拍卖会却试图加入小组");
                    retCode = 0;
                }
                return retCode;
            }
            catch (Exception e)
            {
                ConsoleLog.Error("Database", ConsoleLog.ErrorLogBuilder(e));
                return -1;
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 自动加载默认拍卖品
        /// </summary>
        private AuctionLot GetDefaultLot(long aid)
        {
            return new AuctionLot
            {
                Aid = aid,
                Rid = -1,
                Value = -1,
                StartBid = -1,
                RangeBid = -1
            };
        }
        #endregion
    }
}
