using Sora.EventArgs.SoraEvent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntiRain.DatabaseUtils;
using AntiRain.DatabaseUtils.Helpers.AuctionDB;
using AntiRain.Resource.TypeEnum;
using Sora.Entities;
using AntiRain.Resource.TypeEnum.CommandType;
using AntiRain.Tool;
using Sora.Entities.CQCodes;
using Sora.Entities.Info;
using Sora.Enumeration.ApiEnum;
using Sora.Enumeration.EventParamsType;
using Sora.Tool;

namespace AntiRain.ChatModule.AuctionModule
{
    internal class AuctionGroupManager
    {
        #region 属性
        private GroupMessageEventArgs GBEventArgs { get; set; }
        private Group SourceGroup { get; set; }
        private User SenderQQ { get; set; }
        private AuctionCommand CommandType { get; set; }
        private AuctionGroupMgrDBHelper AuctionGroupDB { get; set; }
        private string[] CommandArgs { get; set; }

        #endregion

        #region 构造函数
        public AuctionGroupManager(GroupMessageEventArgs GBattleEventArgs, AuctionCommand commandType)
        {
            this.GBEventArgs = GBattleEventArgs;
            this.SourceGroup = GBEventArgs.SourceGroup;
            this.SenderQQ = GBEventArgs.Sender;
            this.CommandType = commandType;
            this.AuctionGroupDB = new AuctionGroupMgrDBHelper(GBEventArgs);
            this.CommandArgs = GBEventArgs.Message.RawText.Trim().Split(' ');
        }
        #endregion

        #region 指令分发
        public async void GuildBattleResponse() //指令分发
        {
            if (GBEventArgs == null) throw new ArgumentNullException(nameof(GBEventArgs));

            ConsoleLog.Info($"拍卖会[群:{SourceGroup.Id}]", $"开始处理指令{CommandType}");

            switch (CommandType)
            {
                //创建拍卖会
                case AuctionCommand.CreateAuction:
                    //检查执行者权限和参数
                    if (!await IsAdmin()) return;
                    AuctionCreate();
                    break;

                //删除拍卖会
                case AuctionCommand.DeleteAuction:
                    //检查执行者权限和参数
                    if (!await IsAdmin() || !await ZeroArgsCheck()) return;
                    AuctionDelete();
                    break;

                //查看成员
                case AuctionCommand.ListMember:
                    if (!await IsAdmin() || !await ZeroArgsCheck()) return;
                    MemberList();
                    break;

                //为拍卖会添加成员
                case AuctionCommand.JoinAuction:
                    if (!await IsAdmin()) return;
                    AuctionJoin();
                    break;

                //创建小组
                case AuctionCommand.CreateGroup:
                    if (!await IsAdmin()) return;
                    GroupCreate();
                    break;

                //加入小组
                case AuctionCommand.JoinGroup:
                    if (!await MemberCheck()) return;
                    GroupJoin();
                    break;

                default:
                    ConsoleLog.Warning($"会战[群:{SourceGroup.Id}]", $"接到未知指令{CommandType}");
                    break;
            }
        }
        #endregion

        #region 指令

        /// <summary>
        /// 创建拍卖会
        /// 此时可有一个参数，为拍卖会名
        /// </summary>
        private async void AuctionCreate()
        {
            //获取本群信息
            (APIStatusType apiStatus2, GroupInfo groupInfo) = await SourceGroup.GetGroupInfo();
            string guildName = apiStatus2 == APIStatusType.OK ? groupInfo.GroupName : "unknown_group_name";
            //检查是否已存在拍卖会
            if (AuctionGroupDB.AuctionExists() == -1)
            {
                await SourceGroup.SendGroupMessage(CQCode.CQAt(GBEventArgs.Sender.Id),
                    "\r\nERROR",
                    "\r\n数据库错误");
            }

            //检查参数数量,合法时创建拍卖会
            int result = -2;
            switch (BotUtils.CheckForLength(CommandArgs, 1))
            {
                case LenType.Legitimate://参数中有拍卖会名
                    guildName = CommandArgs[1];
                    result = AuctionGroupDB.CreateAuction(guildName, SourceGroup.Id);
                    break;
                case LenType.Illegal: //参数中没有拍卖会名
                    result = AuctionGroupDB.CreateAuction(guildName, SourceGroup.Id);
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(GBEventArgs.Sender.Id),
                        $"怎么名字都不起啊，给你整成[{guildName}]了");
                    break;
                case LenType.Extra:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(GBEventArgs.Sender.Id),
                        "输入额外信息，请重新输入合法参数");
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(GBEventArgs.Sender.Id),
                        "（名字里不能带空格嗷）");
                    return;
            }
            //输出信息
            switch (result)
            {
                case -1:
                    await SourceGroup.SendGroupMessage($"拍卖会[{guildName}]创建失败：数据库错误。");
                    break;
                case 0:
                    await SourceGroup.SendGroupMessage($"拍卖会[{guildName}]已经创建。");
                    break;
                case 1:
                    await SourceGroup.SendGroupMessage($"拍卖会[{guildName}]已经存在，更新了拍卖会的信息。");
                    break;

            }
        }

        /// <summary>
        /// 删除拍卖会
        /// </summary>
        private async void AuctionDelete()
        {
            if (!await CheckAuctionExist()) return;
            //搜索拍卖会并删除
            string guildName = AuctionGroupDB.GetAuctionName(GBEventArgs.SourceGroup.Id);
            await SourceGroup.SendGroupMessage(AuctionGroupDB.DeleteAuction(GBEventArgs.SourceGroup.Id)
                ? $" 拍卖会[{guildName}]已被删除。"
                : $" 拍卖会[{guildName}]删除失败，数据库错误。");
        }

        /// <summary>
        /// 列出成员
        /// </summary>
        private async void MemberList()
        {
            if (!await CheckAuctionExist()) return;
            List<AuctionMemberInfo> memberList = AuctionGroupDB.ShowMembers(SourceGroup.Id);
            //列出所有成员并对齐
            if (memberList.Any())
            {
                StringBuilder stringBuilder = new StringBuilder();
                double maxLenghtOfQQ = 0; //最长的QQ号长度，用于Pad对齐
                double maxLenghtOfNick = 0; //最长的昵称长度，用于Pad对齐
                int maxLenghtOfQQint = 0; //最长的QQ号长度，用于Pad对齐
                int maxLenghtOfNickint = 0; //最长的昵称长度，用于Pad对齐

                memberList.ForEach(async member =>
                {
                    //await SourceGroup.GetGroupMemberInfo(member.Qid);
                    (APIStatusType status, GroupMemberInfo groupMemberInfo) =
                        await SourceGroup.GetGroupMemberInfo(member.Qid);
                    if (status != APIStatusType.OK)
                    {
                        ConsoleLog.Error("API Error", $"API ret error {status}");
                        return;
                    }

                    #region 为QQ号，昵称长度检查更新
                    if (BotUtils.GetQQStrLength(member.Qid.ToString()) > maxLenghtOfQQ)
                    {
                        maxLenghtOfQQ = BotUtils.GetQQStrLength(member.Qid.ToString());
                    }

                    if (BotUtils.GetQQStrLength(groupMemberInfo.Nick) > maxLenghtOfNick)
                    {
                        maxLenghtOfNick = BotUtils.GetQQStrLength(groupMemberInfo.Nick);
                    }

                    if (member.Qid.ToString().Length > maxLenghtOfQQint)
                    {
                        maxLenghtOfQQint = member.Qid.ToString().Length;
                    }

                    if (groupMemberInfo.Nick.Length > maxLenghtOfNickint)
                    {
                        maxLenghtOfNickint = groupMemberInfo.Nick.Length;
                    }
                    #endregion


                });
                maxLenghtOfQQ+=2;

                #region 为每个成员生成字符串
                memberList.ForEach(async member =>
                {
                    (APIStatusType status, GroupMemberInfo aMember) = await GBEventArgs.SoraApi.GetGroupMemberInfo(member.Aid, member.Qid);
                    if (status != APIStatusType.OK)
                    {
                        ConsoleLog.Error("API Error", $"API ret error {status}");
                        return;
                    }

                    stringBuilder.Append("\n" + BotUtils.PadRightQQ(member.Qid.ToString(), maxLenghtOfQQ) +
                                         "  |   " +
                                         aMember.Nick);
                });
                #endregion

                string listHeader = "\n\t" + AuctionGroupDB.GetAuctionInfo(SourceGroup.Id);
                listHeader += "\n\t公会成员列表";
                listHeader += "\n".PadRight(maxLenghtOfNickint + maxLenghtOfQQint + 6, '=');
                listHeader += "\n" + BotUtils.PadRightQQ("QQ号", maxLenghtOfQQ) + "  |   昵称";
                await SourceGroup.SendGroupMessage(CQCode.CQAt(GBEventArgs.Sender.Id), listHeader, stringBuilder.ToString());
            }
            else
            {
                await SourceGroup.SendGroupMessage(CQCode.CQAt(GBEventArgs.Sender.Id), " 公会暂无成员噢~");
            }
        }

        /// <summary>
        /// 为拍卖会添加成员
        /// </summary>
        private async void AuctionJoin()
        {
            if (!await CheckAuctionExist())
            {
                ConsoleLog.Error("拍卖会数据库", "删除成员时发现该群未登记为拍卖会");
                await SourceGroup.SendGroupMessage("登记登记先登记，还没登记拍卖会就开始加人了？");
                return;
            }

            //检查参数数量
            if (BotUtils.CheckForLength(CommandArgs, 0) != LenType.Legitimate)
            {
                ConsoleLog.Error("拍卖添加成员", "添加成员时有额外参数");
                await SourceGroup.SendGroupMessage("参数好像有点问题诶，添加成员功能不需要参数嗷");
                return;
            }

            //一键入会指令
            switch (AuctionGroupDB.EmptyMember(SourceGroup.Id))
            {
                case -1:
                    ConsoleLog.Error("拍卖会成员数据库", "删除全部成员时出错");
                    await SourceGroup.SendGroupMessage("啊哦，出了一点小问题，请联系我的维护人员嗷。");
                    return;
                case 0:
                    ConsoleLog.Info("拍卖会成员数据库", "删除全部成员成功");
                    break;
                case 1:
                    ConsoleLog.Info("拍卖会成员数据库", "没有可删除的成员");
                    break;
            }

            (APIStatusType apiStatus ,List<GroupMemberInfo> memberList) = await SourceGroup.GetGroupMemberList();
            //若成功获取到群成员列表，删除原有拍卖会成员并添加所有群成员
            if (apiStatus == APIStatusType.OK)
            {
                string messageReturn = AuctionGroupDB.GetAuctionName(SourceGroup.Id);
                messageReturn += "\t\n" + "成员添加完成";
                foreach(GroupMemberInfo groupMember in memberList)
                {
                    int result = await AuctionGroupDB.JoinToAuction(groupMember.UserId, SourceGroup.Id);

                    if (result == 0)
                    {
                        messageReturn += "\t\n" + "正常添加：";
                        messageReturn += "\t\n" + groupMember.UserId + $"[{(string.IsNullOrEmpty(groupMember.Card)? groupMember.Nick : groupMember.Card)}]";
                    }

                    if (result == -1)
                    {
                        messageReturn += "\t\n" + "添加时出错：";
                        messageReturn += "\t\n" + groupMember.UserId + $"[{(string.IsNullOrEmpty(groupMember.Card) ? groupMember.Nick : groupMember.Card)}]";
                    }

                    if (result == 1)
                    {
                        messageReturn += "\t\n" + "已存在拍卖会成员：";
                        messageReturn += "\t\n" + groupMember.UserId + $"[{(string.IsNullOrEmpty(groupMember.Card) ? groupMember.Nick : groupMember.Card)}]";
                    }
                }
                ConsoleLog.Info("拍卖会成员数据库", "添加成员完成");
                await SourceGroup.SendGroupMessage(messageReturn);
            }
            else
            {
                ConsoleLog.Error("拍卖会成员","获取拍卖会成员失败");
                await SourceGroup.SendGroupMessage("啊哦，我好像拿不到群成员列表诶");
            }
            
        }

        /// <summary>
        /// 创建小组
        /// </summary>
        private async void GroupCreate()
        {
            if (!await CheckAuctionExist()) return;
            
            //检查参数数量
            if (BotUtils.CheckForLength(CommandArgs, 2) != LenType.Legitimate)
            {
                ConsoleLog.Error("创建小组","参数不合法");
                await SourceGroup.SendGroupMessage("该命令为 [创建小组][小组名][@组长]","请输入正确数量的参数");
                return;
            }
            string name = CommandArgs[1];
            long leaderId = await GetUidInMsg();
            #region 检查组长参数
            
            if (leaderId == -1 || leaderId == 0)
            {
                ConsoleLog.Error("创建小组", "组长不合法");
                await SourceGroup.SendGroupMessage("需要且只要at一人组长");
                return;
            }
            
            #endregion

            #region 检查组长是否在其他组
            AuctionMemberInfo CallerInfo = AuctionGroupDB.GetMemberInfo(SenderQQ.Id);
            if (CallerInfo.Gid != -1)
            {
                ConsoleLog.Error("创建小组失败", "发起人已在其他小组中");
                await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),"创建小组失败，", "对方已在其他小组中");
                return;
            }
            #endregion

            //创建小组
            int result = AuctionGroupDB.CreateAuctionGroup(name, SourceGroup.Id);
            switch (result)
            {
                case -1:
                    ConsoleLog.Error("创建小组失败", "数据库错误");
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), "创建小组失败，", "请联系我的维护者");
                    break;
                case 0:
                    ConsoleLog.Info("创建小组", "小组已存在，更名");
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), $"小组更名为{name}，", "你已经成为小组组长");
                    break;
                default:
                    ConsoleLog.Info("创建小组", "创建小组成功");
                    await AuctionGroupDB.LeaderJoinToAuction(leaderId, SourceGroup.Id, result);
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                       "\t\n" + $"创建小组{name}成功，",
                                                       "\t\n"+ $"{leaderId}成为小组组长",
                                                       "\t\n"+ $"小组组号为{result}");
                    break;
            }
        }

        /// <summary>
        /// 加入小组
        /// </summary>
        private async void GroupJoin()
        {
            if (!await CheckAuctionExist()) return;

            //检查参数数量
            if (BotUtils.CheckForLength(CommandArgs, 1) != LenType.Legitimate)
            {
                ConsoleLog.Error("加入小组", "参数不合法");
                await SourceGroup.SendGroupMessage("该命令为 [创建小组][组号]", "请输入正确数量的参数");
                return;
            }

            string groupStr = CommandArgs[1];
            
            if (int.TryParse(groupStr, out int groupId))
            {
                int result = await AuctionGroupDB.JoinToAuctionGroup(SenderQQ.Id, SourceGroup.Id, groupId);
                switch (result)
                {
                    case -1:
                        ConsoleLog.Error("加入小组失败", "数据库错误");
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), "加入小组失败，", "请联系我的维护者");
                        break;
                    case 0:
                        ConsoleLog.Info("加入小组失败", "已经加入其他小组或未加入拍卖会");
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), "加入小组失败", "你已经加入其他小组或未加入拍卖会");
                        break;
                    case 1:
                        ConsoleLog.Info("加入小组", "加入小组成功");
                        await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                            "\t\n" + $"加入小组{groupId}成功，");
                        break;
                }
            }
        }

        #endregion

        #region 私有方法


        /// <summary>
        /// 检查发送消息的成员权限等级是否为管理员及以上
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 成员为管理员或群主</para>
        /// <para><see langword="false"/> 成员不是管理员</para>
        /// </returns>
        private async ValueTask<bool> IsAdmin(bool shwoWarning = true)
        {
            GroupSenderInfo senderInfo = GBEventArgs.SenderInfo;

            bool isAdmin = senderInfo.Role == MemberRoleType.Admin ||
                           senderInfo.Role == MemberRoleType.Owner;
            //非管理员执行的警告信息
            if (!isAdmin)
            {
                //执行者为普通群员时拒绝执行指令
                if (shwoWarning) await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                                                        "此指令只允许管理者执行");
                ConsoleLog.Warning($"会战[群:{SourceGroup.Id}]", $"群成员{senderInfo.Nick}正在尝试执行指令{CommandType}");
            }
            return isAdmin;
        }

        /// <summary>
        /// 零参数指令的参数检查
        /// 同时检查成员是否存在
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 指令合法</para>
        /// <para><see langword="false"/> 有多余参数</para>
        /// </returns>
        private async ValueTask<bool> ZeroArgsCheck()
        {
            //检查参数
            switch (BotUtils.CheckForLength(CommandArgs, 0))
            {
                case LenType.Extra:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                             "\r\n听不见！重来！（有多余参数）");
                    return false;
                case LenType.Legitimate:
                    return true;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error", "LenType");
                    return false;
            }
        }

        /// <summary>
        /// 检查成员
        /// </summary>
        /// <returns>
        /// <para><see langword="true"/> 存在成员</para>
        /// <para><see langword="false"/> 不存在或有错误</para>
        /// </returns>
        private async ValueTask<bool> MemberCheck()
        {
            //检查成员
            switch (AuctionGroupDB.CheckMemberExists(SenderQQ.Id))
            {
                case 1:
                    return true;
                case 0:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id), "不是这个拍卖会的成员");
                    return false;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return false;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(SenderQQ.Id),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error", "LenType");
                    return false;
            }
        }

        /// <summary>
        /// 根据UID来检查成员
        /// </summary>
        /// <param name="uid">成员UID</param>
        /// <returns>
        /// <para><see langword="true"/> 存在成员</para>
        /// <para><see langword="false"/> 不存在或有错误</para>
        /// </returns>
        private async ValueTask<bool> MemberCheck(long uid)
        {
            //检查成员
            switch (AuctionGroupDB.CheckMemberExists(uid))
            {
                case 1:
                    return true;
                case 0:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(uid), "不是这个公会的成员");
                    return false;
                case -1:
                    DBMsgUtils.DatabaseFailedTips(GBEventArgs);
                    return false;
                default:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(uid),
                                             "发生未知错误，请联系机器人管理员");
                    ConsoleLog.Error("Unknown error", "LenType");
                    return false;
            }
        }

        /// <summary>
        /// 从消息的CQ码中获取用户ID（单CQ码）
        /// <para><see langword="long number"/> 返回单CQ码的用户ID</para>
        /// <para><see langword="0"/> 消息中有多个用户At</para>
        /// <para><see langword="-1"/> 消息中无At或At成员不在用户列表</para>
        /// </summary>
        private async ValueTask<long> GetUidInMsg()
        {
            List<long> AtUserList = GBEventArgs.Message.GetAllAtList();
            if (AtUserList.Count == 0) return -1;
            if (AtUserList.Count > 1) return 0;
            //检查成员
            if (await MemberCheck(AtUserList.First()))
            {
                return AtUserList.First();
            }
            return -1;
        }

        const string ROUND_CODE = "ABCDEFGHIJKLNMOPQRSTUVWXYZ";
        private string GetBossCode(int round, int order)
        {
            return round > 26 ? $"{round} - {order}" : $"{ROUND_CODE[round - 1]}{order}";
        }

        private async Task<bool> CheckAuctionExist()
        {
            switch (AuctionGroupDB.AuctionExists())
            {
                case 0:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(GBEventArgs.Sender.Id),
                        "这群还没登记成拍卖会，你干啥呢？");
                    return false;
                case -1:
                    await SourceGroup.SendGroupMessage(CQCode.CQAt(GBEventArgs.Sender.Id),
                        "\r\nERROR",
                        "\r\n数据库错误");
                    return false;
            }

            return true;
        }
        #endregion
    }
}
