using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntiRain.ChatModule.PcrGuildBattle;
using AntiRain.Resource.TypeEnum.CommandType;
using Sora.EventArgs.SoraEvent;
using Sora.Tool;

namespace AntiRain.ChatModule.AuctionModule
{
    internal class AuctionChatHandle
    {
        #region 属性
        public object Sender { private set; get; }
        public GroupMessageEventArgs AuctionEventArgs { private set; get; }
        public string AuctionStrCommand { private get; set; }
        private AuctionCommand CommandType { get; set; }
        #endregion

        #region 构造函数
        public AuctionChatHandle(object sender, GroupMessageEventArgs e, AuctionCommand commandType)
        {
            this.AuctionEventArgs = e;
            this.Sender = sender;
            this.CommandType = commandType;
        }
        #endregion

        #region 消息解析函数
        public void GetChat() //消息接收并判断是否响应
        {
            try
            {
                //获取第二个字符开始到空格为止的PCR命令
                AuctionStrCommand = AuctionEventArgs.Message.RawText.Substring(1).Split(' ')[0];
                //公会管理指令
                if ((int)CommandType > 1000 && (int)CommandType < 1100)
                {
                    ConsoleLog.Info("拍卖管理", $"获取到了拍卖管理指令{CommandType}");
                    AuctionEventArgs.SourceGroup.SendGroupMessage("哇哦");
                    AuctionGroupManager auctionGroupManager = new AuctionGroupManager(AuctionEventArgs,CommandType);
                    auctionGroupManager.GuildBattleResponse();
                }
                //出刀管理指令
                else if ((int)CommandType > 1100 && (int)CommandType < 1200)
                {
                    ConsoleLog.Info("拍卖管理", $"获取到了拍卖指令{CommandType}");
                    AuctionEventArgs.SourceGroup.SendGroupMessage("好耶");
                    //GuildBattleManager battleManager = new GuildBattleManager(PCRGuildEventArgs, CommandType);
                    //battleManager.GuildBattleResponse();
                }
            }
            catch (Exception e)
            {
                //命令无法被正确解析
                ConsoleLog.Error("拍卖管理", $"指令解析发生错误\n{e}");
            }
        }
        #endregion
    }
}
