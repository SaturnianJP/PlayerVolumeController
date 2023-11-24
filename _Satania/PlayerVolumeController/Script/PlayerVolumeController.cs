
using satania.behaviour;
using VRC.SDK3.Data;
using VRC.SDKBase;
using UnityEngine;

namespace satania.player.volume
{
    /// <summary>
    /// ローカル動作
    /// </summary>
    public class PlayerVolumeController : SataniaNonSyncBehaviour
    {
        private void DebugLog(string msg = "", string color = "yellow", string title = nameof(PlayerVolumeController))
        {
            Debug.Log($"[<color={color}>{title}</color>]{msg}");
        }

        #region Serialize Variables
        /// <summary>
        /// in Decibels, Range 0-24 Add boost to the Player's voice in decibels. Default is 15.
        /// </summary>
        [Header("マイクのゲイン値 | 範囲[0-24] デフォルト[15]")]
        [SerializeField] float VoiceGain = 15.0f;
        
        /// <summary>
        /// in Meters, Range 0 - 1,000,000 The near radius, in meters, where volume begins to fall off. It is strongly recommended to leave the Near value at zero for realism and effective spatialization for user voices.
        /// </summary>
        [Header("マイク音量が下がり始める距離 | 範囲[0 - 1000000] デフォルト[0]")]
        [SerializeField] float VoiceDistanceNear = 0.0f;
        
        /// <summary>
        /// in Meters, Range is 0 - 1,000,000 This sets the end of the range for hearing the user's voice. Default is 25 meters. You can lower this to make another player's voice not travel as far, all the way to 0 to effectively 'mute' the player.
        /// </summary>
        [Header("マイクの音が届く最大距離 | 範囲[0 - 1000000] デフォルト[25]")]
        [SerializeField] float VoiceDistanceFar = 25.0f;
        
        /// <summary>
        /// in Meters, Range is 0 -1,000 Default is 0. A player's voice is normally simulated to be a point source, however changing this value allows the source to appear to come from a larger area. This should be used carefully, and is mainly for distant audio sources that need to sound "large" as you move past them. Keep this at zero unless you know what you're doing. The value for Volumetric Radius should always be lower than Voice Distance Far.
        ///
        ///If you want a user's voice to sound like it is close no matter how far it is, increase the Voice Distance Near range to a large value.
        /// </summary>
        [Header("マイクの音源の大きさ | 範囲[0 - 1000] デフォルト[0]")]
        [SerializeField] float VoiceVolumetricRadius = 0.0f;
        
        /// <summary>
        /// On/Off When a voice is some distance off, it is passed through a low-pass filter to help with understanding noisy worlds. You can disable this if you want to skip this filter. For example, if you intend for a player to use their voice channel to play a high-quality DJ mix, turning this filter off is advisable.
        /// </summary>
        [Header("マイクのローパスフィルター | デフォルト[ON]")]
        [SerializeField] bool VoiceLowPass = true;

        /// <summary>
        /// in Decibels, Range 0-10 Set the Maximum Gain allowed on Avatar Audio. Default is 10.
        /// </summary>
        [Header("アバター音量のゲイン値 | 範囲[0 - 10] デフォルト[10]")]
        [SerializeField] float AvatarAudioGain = 10.0f;

        /// <summary>
        /// in Meters, Range is not limited This sets the maximum end of the range for hearing the avatar's audio. Default is 40 meters. You can lower this to make another player's avatar not travel as far, all the way to 0 to effectively 'mute' the player. Note that this is compared to the audio source's maxDistance, and the smaller value is used.
        /// </summary>
        [Header("アバターの音声が聞こえる最大距離 | 範囲[0 - 40] デフォルト[40]")]
        [SerializeField] float AvatarAudioFarRadius = 40.0f;

        /// <summary>
        /// in Meters, Range is not limited This sets the maximum start of the range for hearing the avatar's audio. Default is 40 meters. You can lower this to make another player's avatar not travel as far, all the way to 0 to effectively 'mute' the player. Note that this is compared to the audio source's minDistance, and the smaller value is used.
        /// </summary>
        [Header("アバターの音声が聞こえ始める距離 | 範囲[0 - 40] デフォルト[40]")]
        [SerializeField] float AvatarAudioNearRadius = 40.0f;

        /// <summary>
        /// in Meters, Range is not limited An avatar's audio source is normally simulated to be a point source, however changing this value allows the source to appear to come from a larger area. This should be used carefully, and is mainly for distant audio sources that need to sound "large" as you move past them. Keep this at zero unless you know what you're doing. The value for Volumetric Radius should always be lower than Avatar AudioFarRadius. Default is 40 meters.
        /// </summary>
        [Header("アバターの音声が聞こえる中心点の大きさ | 範囲[0 - 40] デフォルト[40]")]
        [SerializeField] float AvatarAudioVolmetricRadius = 40.0f;

        /// <summary>
        /// On/Off If this is on, then Spatialization is enabled for the source, and the spatialBlend is set to 1. Default is Off.
        /// </summary>
        [Header("アバターオーディオの空間化 | デフォルト[OFF]")]
        [SerializeField] bool AvatarAudioForceSpatial = false;

        /// <summary>
        /// On/Off This sets whether the audio source should use a pre-configured custom curve. Default is Off.
        /// </summary>
        [Header("オーディオソースのカスタムカーブの有効化 | デフォルト[ON]")]
        [SerializeField] bool AvatarAudioCustomCurve = true;
        #endregion

        #region Player Voice Dictionaries
        private DataDictionary _VoiceGains = new DataDictionary();
        private DataDictionary _VoiceDistanceNears = new DataDictionary();
        private DataDictionary _VoiceDistanceFars = new DataDictionary();
        private DataDictionary _VoiceVolumetricRadius = new DataDictionary();
        private DataDictionary _VoiceLowpasses = new DataDictionary();
        #endregion

        #region Avatar Audio Dictionaries
        private DataDictionary _AvatarAudioGain = new DataDictionary();
        private DataDictionary _AvatarAudioFarRadius = new DataDictionary();
        private DataDictionary _AvatarAudioNearRadius = new DataDictionary();
        private DataDictionary _AvatarAudioVolumetricRadius = new DataDictionary();
        private DataDictionary _AvatarAudioForceSpatial = new DataDictionary();
        private DataDictionary _AvatarAudioCustomCurve = new DataDictionary();
        #endregion

        #region VRCPlayerApi Func
        VRCPlayerApi[] GetAllPlayers()
        {
            VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(players);
            return players;
        }
        #endregion

        #region Dictionary Func
        private int GetIndexFromKey(DataDictionary dic, DataToken key)
        {
            int ret = -1;
            var keys = dic.GetKeys();
            ret = keys.IndexOf(key);

            return ret;
        }
        private void AddDictionary(DataDictionary dic, int key, DataToken value)
        {
            int index = GetIndexFromKey(dic, key);
            if (index == -1)
                dic.Add(key, value);
        }
        private void RemoveDictionary(DataDictionary dic, int key)
        {
            int index = GetIndexFromKey(dic, key);
            if (index != -1)
                dic.Remove(key);
        }
        private void AddDictionaries(int _id)
        {
            SetDictionary(_VoiceGains, _id, VoiceGain);
            SetDictionary(_VoiceDistanceNears, _id, VoiceDistanceNear);
            SetDictionary(_VoiceDistanceFars, _id, VoiceDistanceFar);
            SetDictionary(_VoiceVolumetricRadius, _id, VoiceVolumetricRadius);
            SetDictionary(_VoiceLowpasses, _id, VoiceLowPass);

            SetDictionary(_AvatarAudioGain, _id, AvatarAudioGain);
            SetDictionary(_AvatarAudioFarRadius, _id, AvatarAudioFarRadius);
            SetDictionary(_AvatarAudioNearRadius, _id, AvatarAudioNearRadius);
            SetDictionary(_AvatarAudioVolumetricRadius, _id, AvatarAudioVolmetricRadius);
            SetDictionary(_AvatarAudioForceSpatial, _id, AvatarAudioForceSpatial);
            SetDictionary(_AvatarAudioCustomCurve, _id, AvatarAudioCustomCurve);
        }
        private void RemoveDictionaries(VRCPlayerApi player)
        {
            int _id = player.playerId;

            RemoveDictionary(_VoiceGains, _id);
            RemoveDictionary(_VoiceDistanceNears, _id);
            RemoveDictionary(_VoiceDistanceFars, _id);
            RemoveDictionary(_VoiceVolumetricRadius, _id);
            RemoveDictionary(_VoiceLowpasses, _id);

            RemoveDictionary(_AvatarAudioGain, _id);
            RemoveDictionary(_AvatarAudioFarRadius, _id);
            RemoveDictionary(_AvatarAudioNearRadius, _id);
            RemoveDictionary(_AvatarAudioVolumetricRadius, _id);
            RemoveDictionary(_AvatarAudioForceSpatial, _id);
            RemoveDictionary(_AvatarAudioCustomCurve, _id);
        }
        private void SetDictionary(DataDictionary dic, int key, DataToken value)
        {
            int index = GetIndexFromKey(dic, key);
            if (index == -1)
                AddDictionary(dic, key, value);
            else
                dic[key] = value;
        }
        private void InitializeDictionary(VRCPlayerApi player)
        {
            if (!player.isLocal)
                return;

            var players = GetAllPlayers();
            foreach (var plr in players)
            {
                if (plr == null)
                    continue;

                if (!plr.IsValid())
                    continue;

                AddDictionaries(plr.playerId);
            }
        }
        #endregion

        #region GetValue Func
        public float GetVoiceGain(int playerid)
        {
            float ret = VoiceGain;
            if (_VoiceGains.TryGetValue(playerid, out DataToken value))
                ret = value.Float;

            return ret;
        }

        public float GetVoiceDistanceNear(int playerid)
        {
            float ret = VoiceDistanceNear;
            if (_VoiceDistanceNears.TryGetValue(playerid, out DataToken value))
                ret = value.Float;

            return ret;
        }

        public float GetVoiceDistanceFar(int playerid)
        {
            float ret = VoiceDistanceFar;
            if (_VoiceDistanceFars.TryGetValue(playerid, out DataToken value))
                ret = value.Float;

            return ret;
        }

        public float GetVoiceVolumetricRadius(int playerid)
        {
            float ret = VoiceVolumetricRadius;
            if (_VoiceVolumetricRadius.TryGetValue(playerid, out DataToken value))
                ret = value.Float;

            return ret;
        }

        public bool GetVoiceLowpass(int playerid)
        {
            bool ret = VoiceLowPass;
            if (_VoiceLowpasses.TryGetValue(playerid, out DataToken value))
                ret = value.Boolean;

            return ret;
        }

        public float GetAvatarAudioGain(int playerid)
        {
            float ret = AvatarAudioGain;
            if (_AvatarAudioGain.TryGetValue(playerid, out DataToken value))
                ret = value.Float;

            return ret;
        }

        public float GetAvatarAudioFarRadius(int playerid)
        {
            float ret = AvatarAudioFarRadius;
            if (_AvatarAudioFarRadius.TryGetValue(playerid, out DataToken value))
                ret = value.Float;

            return ret;
        }

        public float GetAvatarAudioNearRadius(int playerid)
        {
            float ret = AvatarAudioNearRadius;
            if (_AvatarAudioNearRadius.TryGetValue(playerid, out DataToken value))
                ret = value.Float;

            return ret;
        }

        public float GetAvatarAudioVolumetricRadius(int playerid)
        {
            float ret = AvatarAudioVolmetricRadius;
            if (_AvatarAudioVolumetricRadius.TryGetValue(playerid, out DataToken value))
                ret = value.Float;

            return ret;
        }

        public bool GetAvatarAudioForceSpatial(int playerid)
        {
            bool ret = AvatarAudioForceSpatial;
            if (_AvatarAudioForceSpatial.TryGetValue(playerid, out DataToken value))
                ret = value.Boolean;

            return ret;
        }

        public bool GetAvatarAudioCustomCurve(int playerid)
        {
            bool ret = AvatarAudioCustomCurve;
            if (_AvatarAudioCustomCurve.TryGetValue(playerid, out DataToken value))
                ret = value.Boolean;

            return ret;
        }
        #endregion

        #region Player Voice Func
        public void SetVoiceGain(int playerid, float gain) 
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetDictionary(_VoiceGains, playerid, gain);
                player.SetVoiceGain(gain);
            }
        }
        public void SetVoiceDistanceNear(int playerid, float near)
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetDictionary(_VoiceDistanceNears, playerid, near);
                player.SetVoiceDistanceNear(near);
            }
        }
        public void SetVoiceDistanceFar(int playerid, float far) 
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetDictionary(_VoiceDistanceFars, playerid, far);
                player.SetVoiceDistanceFar(far);
            }
        }
        public void SetVoiceVolumetricRadius(int playerid, float radius) 
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetDictionary(_VoiceVolumetricRadius, playerid, radius);
                player.SetVoiceVolumetricRadius(radius);
            }
        }
        public void SetVoiceLowpass(int playerid, bool lowpass) 
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetDictionary(_VoiceLowpasses, playerid, lowpass);
                player.SetVoiceLowpass(lowpass);
            }
        }
        #endregion

        #region Avatar Audio Func
        public void SetAvatarAudioGain(int playerid, float gain)
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetDictionary(_AvatarAudioGain, playerid, gain);
                player.SetAvatarAudioGain(gain);
            }
        }
        public void SetAvatarAudioFarRadius(int playerid, float radius)
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetDictionary(_AvatarAudioFarRadius, playerid, radius);
                player.SetAvatarAudioFarRadius(radius);
            }

        }
        public void SetAvatarAudioNearRadius(int playerid, float radius)
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetDictionary(_AvatarAudioNearRadius, playerid, radius);
                player.SetAvatarAudioNearRadius(radius);
            }
        }
        public void SetAvatarAudioVolumetricRadius(int playerid, float radius) 
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetDictionary(_AvatarAudioVolumetricRadius, playerid, radius);
                player.SetAvatarAudioVolumetricRadius(radius);
            }
        }
        public void SetAvatarAudioForceSpatial(int playerid, bool spatial) 
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetDictionary(_AvatarAudioForceSpatial, playerid, spatial);
                player.SetAvatarAudioForceSpatial(spatial);
            }
        }
        public void SetAvatarAudioCustomCurve(int playerid, bool useCustomCurve)
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetDictionary(_AvatarAudioCustomCurve, playerid, useCustomCurve);
                player.SetAvatarAudioCustomCurve(useCustomCurve);
            }
        }
        #endregion

        #region Reset Func
        public void ResetPlayerVoice(int playerid)
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetVoiceGain(playerid, VoiceGain);
                SetVoiceDistanceNear(playerid, VoiceDistanceNear);
                SetVoiceDistanceFar(playerid, VoiceDistanceFar);
                SetVoiceVolumetricRadius(playerid, VoiceVolumetricRadius);
                SetVoiceLowpass(playerid, VoiceLowPass);
            }
        }

        public void ResetAvatarAudio(int playerid)
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerid);

            if (player != null)
            {
                SetAvatarAudioGain(playerid, AvatarAudioGain);
                SetAvatarAudioFarRadius(playerid, AvatarAudioFarRadius);
                SetAvatarAudioNearRadius(playerid, AvatarAudioNearRadius);
                SetAvatarAudioVolumetricRadius(playerid, AvatarAudioVolmetricRadius);
                SetAvatarAudioForceSpatial(playerid, AvatarAudioForceSpatial);
                SetAvatarAudioCustomCurve(playerid, AvatarAudioCustomCurve);
            }
        }
        #endregion

        #region VRChat Func
        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player.isLocal)
                InitializeDictionary(player);
            else
                AddDictionaries(player.playerId);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            //自身が抜けたときはスルー
            if (!player.isLocal)
                RemoveDictionaries(player);
        }
        #endregion
    }
}
