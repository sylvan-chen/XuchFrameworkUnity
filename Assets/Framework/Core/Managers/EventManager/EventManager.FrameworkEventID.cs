namespace XuchFramework.Core
{
    /// <summary>
    /// 框架事件 ID
    /// </summary>
    /// <remarks>
    /// 1~99 保留给框架使用,
    /// 100~65535 保留给协议使用,
    /// 100000 及以上给游戏使用
    /// </remarks>
    public static class FrameworkEventID
    {
        /// <summary> (channelName: string, ESocketError: int, errmsg: string) </summary>
        public const int NetworkConnected = 1;
    }
}