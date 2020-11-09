using System.ComponentModel;

namespace AntiRain.Resource.TypeEnum.CommandType
{
    /// <summary>
    /// 机器人拍卖指令
    /// </summary>
    internal enum AuctionCommand
    {
        /// <summary>
        /// 创建一场拍卖会
        /// </summary>
        [Description("准备拍卖")]
        CreateAuction = 1001,

        /// <summary>
        /// 创建参与拍卖的小组
        /// </summary>
        [Description("创建小组")]
        CreateGroup = 1002,

        /// <summary>
        /// 查看成员
        /// </summary>
        [Description("查看成员")]
        ListMember = 1003,

        /// <summary>
        /// 查看成员（各个小组/全部小组）
        /// </summary>
        [Description("查看小组成员")]
        ListGroupMember = 1004,

        /// <summary>
        /// 清空拍卖会所有成员
        /// </summary>
        [Description("清空成员")]
        QuitAll = 1005,

        /// <summary>
        /// 删除小组
        /// </summary>
        [Description("删除小组")]
        DeleteGroup = 1006,

        /// <summary>
        /// 添加拍卖会藏品
        /// </summary>
        [Description("取消拍卖")]
        DeleteAuction = 1008,

        /// <summary>
        /// 添加成员
        /// </summary>
        [Description("添加成员")]
        JoinAuction = 1009,

        /// <summary>
        /// 添加小组成员
        /// </summary>
        [Description("加入小组")]
        JoinGroup = 1010,

        /// <summary>
        /// 拍卖开始指令
        /// </summary>
        [Description("开始拍卖")]
        AuctionStart = 1101,

        /// <summary>
        /// 拍卖结束命令
        /// </summary>
        [Description("结束拍卖")]
        AuctionEnd = 1102,

        /// <summary>
        /// 申请叫价命令
        /// </summary>
        [Description("申请出价")]
        RequestBid = 1103,

        /// <summary>
        /// 取消叫价申请
        /// </summary>
        [Description("取消申请")]
        UndoRequestBid = 1104,

        /// <summary>
        /// 叫价命令
        /// </summary>
        [Description("出价")]
        Bid = 1105,

        /// <summary>
        /// 撤销叫价命令
        /// </summary>
        [Description("撤销出价")]
        UndoBid = 1106,

        /// <summary>
        /// 删除叫价命令
        /// </summary>
        [Description("删除出价")]
        DeleteBid = 1107,

        /// <summary>
        /// 查看进度命令
        /// </summary>
        [Description("拍卖进度")]
        ShowProgress = 1108,

        /// <summary>
        /// 全购买表命令
        /// </summary>
        [Description("全拍卖记录")]
        ShowAllSellList = 1109,

        /// <summary>
        /// 修改进度
        /// </summary>
        [Description("修改进度")]
        ModifyProgress = 1110,

        /// <summary>
        /// 单组购买表查询
        /// </summary>
        [Description("组拍卖记录")]
        ShowSellList = 1111,

        /// <summary>
        /// 暂停拍卖进程
        /// </summary>
        [Description("暂停")]
        Pause = 1112
    }
}