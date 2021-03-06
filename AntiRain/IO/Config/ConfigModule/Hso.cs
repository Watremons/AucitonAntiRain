using AntiRain.Resource.TypeEnum;

namespace AntiRain.IO.Config.ConfigModule
{
    internal class Hso
    {
        /// <summary>
        /// 色图源类型
        /// </summary>
        public SetuSourceType Source { set; get; }
        /// <summary>
        /// Pximy代理
        /// </summary>
        public string PximyProxy { set; get; }
        /// <summary>
        /// 是否启用本地缓存
        /// </summary>
        public bool UseCache { set; get; }
        /// <summary>
        /// 是否使用装逼大图
        /// </summary>
        public bool CardImage { set; get; }
        /// <summary>
        /// 色图文件夹大小限制
        /// </summary>
        public long SizeLimit { set; get; }
        /// <summary>
        /// LoliconToken
        /// </summary>
        public string LoliconToken { set; get; }
        /// <summary>
        /// YukariToken
        /// </summary>
        public string YukariToken { set; get; }
    }
}
