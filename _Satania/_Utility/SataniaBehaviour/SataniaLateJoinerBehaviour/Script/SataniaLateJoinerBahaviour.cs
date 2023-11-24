
using UdonSharp;
using VRC.SDKBase;

namespace satania.behaviour
{
    /// <summary>
    /// LateJoinerへの同期に対応したUdonBehaviourです。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SataniaLateJoinerBahaviour : UdonSharpBehaviour
    {
        #region VRChat Func
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject))
            {
                RequestSerialization();
            }
        }
        
        /// <summary>
        /// オーナーでない場合はオーナーを取得した後に、RequestSerialization()を行います。
        /// </summary>
        public void RequestSerializeAndTakeOwner()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            
            RequestSerialization();
        }
        #endregion
    }
}
