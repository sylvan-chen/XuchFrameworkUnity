using System;
using System.Collections.Generic;
using UnityEngine;
using DigiEden.Framework.Utils;

namespace DigiEden.Framework
{
    /// <summary>
    /// 框架事件 ID 枚举  100~ 65535 以内被协议使用了
    /// 1~99 给框架自身使用
    /// </summary>
    public enum FrameworkEventID
    {
        NetworkConnected = 1, //参数1: channelName,  参数2:ESocketError, 参数3: errmsg
    }
}