using System.Threading.Tasks;
using AntiRain.ChatModule;
using AntiRain.ChatModule.AuctionModule;
using AntiRain.ChatModule.HsoModule;
using AntiRain.ChatModule.PcrGuildBattle;
using AntiRain.ChatModule.PcrUtils;
using AntiRain.IO.Config;
using AntiRain.IO.Config.ConfigModule;
using AntiRain.Resource;
using AntiRain.Resource.TypeEnum.CommandType;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;

namespace AntiRain.ServerInterface
{
    /// <summary>
    /// 群聊事件
    /// </summary>
    internal static class GroupMessageEvent
    {
        /// <summary>
        /// 群聊处理和事件触发分发
        /// </summary>
        public static ValueTask GroupMessageParse(object sender, GroupMessageEventArgs groupMessage)
        {
            //配置文件实例
            Config config = new Config(groupMessage.LoginUid);
            //读取配置文件
            if (!config.LoadUserConfig(out UserConfig userConfig))
            {
                groupMessage.SourceGroup.SendGroupMessage("读取配置文件(User)时发生错误\r\n请联系机器人管理员");
                ConsoleLog.Error("AntiRain会战管理", "无法读取用户配置文件");
                return ValueTask.CompletedTask;
            }
            //指令匹配
            //#开头的指令(会战) -> 关键词 -> 正则
            //会战管理
            if (Command.GetPCRGuildBattlecmdType(groupMessage.Message.RawText, out PCRGuildBattleCommand battleCommand))
            {
                ConsoleLog.Info("PCR会战管理",$"获取到指令[{battleCommand}]");
                //判断模块使能
                if (userConfig.ModuleSwitch.PCR_GuildManager)
                {
                    PcrGuildBattleChatHandle chatHandle = new PcrGuildBattleChatHandle(sender, groupMessage, battleCommand);
                    chatHandle.GetChat();
                }
                else
                {
                    ConsoleLog.Warning("AntiRain会战管理", "会战功能未开启");
                }
            }

            //指令匹配
            //*开头的指令(会战) -> 关键词 -> 正则
            //拍卖管理
            if (Command.GetAuctioncmdType(groupMessage.Message.RawText, out AuctionCommand auctionCommand))
            {
                ConsoleLog.Info("拍卖管理", $"获取到指令[{auctionCommand}]");
                //判断模块使能
                if (userConfig.ModuleSwitch.Auction)
                {
                    AuctionChatHandle chatHandle = new AuctionChatHandle(sender, groupMessage, auctionCommand);
                    chatHandle.GetChat();
                }
                else
                {
                    ConsoleLog.Warning("AntiRain拍卖管理", "拍卖功能未开启");
                }
            }


            //聊天关键词
            if (Command.GetKeywordType(groupMessage.Message.RawText, out KeywordCommand keywordCommand))
            {
                ConsoleLog.Info("关键词触发",$"触发关键词[{keywordCommand}]");
                switch (keywordCommand)
                {
                    case KeywordCommand.Hso:
                        if (userConfig.ModuleSwitch.Hso)
                        {
                            HsoHandle hso = new HsoHandle(sender, groupMessage);
                            hso.GetChat();
                        }
                        break;
                    //将其他的的全部交给娱乐模块处理
                    default:
                        if (userConfig.ModuleSwitch.HaveFun)
                        {
                            Surprise surprise = new Surprise(sender, groupMessage);
                            surprise.GetChat(keywordCommand);
                        }
                        break;
                }
            }

            //正则匹配
            if (Command.GetRegexType(groupMessage.Message.RawText, out RegexCommand regexCommand))
            {
                ConsoleLog.Info("正则触发",$"触发正则匹配[{regexCommand}]");
                switch (regexCommand)
                {
                    case RegexCommand.CheruDecode: case RegexCommand.CheruEncode:
                        //判断模块使能
                        if (userConfig.ModuleSwitch.Cheru)
                        {
                            CheruHandle cheru = new CheruHandle(sender, groupMessage);
                            cheru.GetChat(regexCommand);
                        }
                        break;
                    case RegexCommand.GetGuildRank:
                        //判断模块使能
                        if (userConfig.ModuleSwitch.PCR_GuildRank)
                        {
                            GuildRankHandle guildRank = new GuildRankHandle(sender, groupMessage);
                            guildRank.GetChat(regexCommand);
                        }
                        break;
                    //将其他的的全部交给娱乐模块处理
                    default:
                        if (userConfig.ModuleSwitch.HaveFun)
                        {
                            Surprise surprise = new Surprise(sender, groupMessage);
                            surprise.GetChat(regexCommand);
                        }
                        break;
                }
            }
            return ValueTask.CompletedTask;
        }
    }
}
