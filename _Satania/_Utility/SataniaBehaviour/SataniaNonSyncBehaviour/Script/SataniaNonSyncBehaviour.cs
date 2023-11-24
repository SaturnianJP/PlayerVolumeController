
using UdonSharp;

namespace satania.behaviour
{
    /// <summary>
    /// 毎回SyncModeを指定するのがめんどいので作ったやつです。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SataniaNonSyncBehaviour : UdonSharpBehaviour { }
}

