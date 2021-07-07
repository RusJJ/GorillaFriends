using BepInEx;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaFriends
{
    [BepInPlugin(ModConstants.ModConstants.modGUID, ModConstants.ModConstants.modName, ModConstants.ModConstants.modVersion)]
    public class Main : BaseUnityPlugin
    {
        internal static Main m_hInstance = null;
        internal static GameObject m_pScoreboardFriendBtn = null;
        internal static GorillaPlayerLineButton m_pScoreboardFriendController = null;
        internal static List<string> m_listVerifiedUserIds = new List<string>();
        internal static List<string> m_listCurrentSessionFriends = new List<string>();
        internal static void Log(string msg) => m_hInstance.Logger.LogMessage(msg);
        public static Color m_clrFriendColor { get; internal set; } = new Color(0.8f, 0.5f, 0.9f, 1.0f);
        public static Color m_clrVerifiedColor { get; internal set; } = new Color(0.5f, 1.0f, 0.5f, 1.0f);
        void Awake()
        {
            WebVerified.LoadListOfVerified();
            m_hInstance = this;
            HarmonyPatcher.Patch.Apply();
        }
        public static bool IsVerified(string userId)
        {
            foreach(string s in m_listVerifiedUserIds)
            {
                if (s == userId) return true;
            }
            return false;
        }
        public static bool IsFriend(string userId)
        {
            return (PlayerPrefs.GetInt(userId + "_friend", 0) != 0);
        }
        public static bool IsInFriendList(string userId)
        {
            foreach(string s in m_listCurrentSessionFriends)
            {
                if (s == userId) return true;
            }
            return false;
        }
    }

    public class ScoreboardLineFriendBtn : MonoBehaviour
    {
        public GorillaPlayerLineButton friendButton = null;
    }

    [HarmonyPatch(typeof(GorillaScoreBoard))]
    [HarmonyPatch("Awake", MethodType.Normal)]
    internal class GorillaScoreBoardAwake
    {
        private static void Prefix(GorillaScoreBoard __instance)
        {
            if (Main.m_pScoreboardFriendBtn != null) return;
            foreach(Transform t in __instance.scoreBoardLinePrefab.transform)
            {
                if(t.name == "Mute Button")
                {
                    Main.m_pScoreboardFriendBtn = GameObject.Instantiate(t.gameObject);
                    if (Main.m_pScoreboardFriendBtn != null) // Who knows...
                    {
                        t.localPosition = new Vector3(3.8f, 0.0f, 0.0f); // Move MuteButton a bit to left
                        Main.m_pScoreboardFriendBtn.transform.parent = __instance.scoreBoardLinePrefab.transform;
                        Main.m_pScoreboardFriendBtn.transform.name = "FriendButton";
                        Main.m_pScoreboardFriendBtn.transform.localPosition = new Vector3(17.5f, 0.0f, 0.0f);
                        Main.m_pScoreboardFriendController = Main.m_pScoreboardFriendBtn.GetComponent<GorillaPlayerLineButton>();
                        if (Main.m_pScoreboardFriendController != null)
                        {
                            __instance.scoreBoardLinePrefab.AddComponent<ScoreboardLineFriendBtn>().friendButton = Main.m_pScoreboardFriendController;
                            Main.m_pScoreboardFriendController.offText = "ADD FRIEND";
                            Main.m_pScoreboardFriendController.onText = "FRIEND!";
                            Main.m_pScoreboardFriendController.myText.text = Main.m_pScoreboardFriendController.offText;
                        }
                    }
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(GorillaPlayerScoreboardLine))]
    [HarmonyPatch("Update", MethodType.Normal)]
    internal class GorillaPlayerScoreboardLineUpdate
    {
        private static void Prefix(GorillaPlayerScoreboardLine __instance)
        {
            if (__instance.playerVRRig != null && __instance.linePlayer != null)
            {
                var tmp = __instance.GetComponent<ScoreboardLineFriendBtn>();
                if (tmp == null || tmp.friendButton == null) return;
                if (__instance.initialized == false)
                {
                    if (Main.IsVerified(__instance.linePlayer.UserId))
                    {
                        __instance.playerName.color = Main.m_clrVerifiedColor;
                        __instance.playerVRRig.playerText.color = Main.m_clrVerifiedColor;
                    }
                    if (__instance.linePlayer.IsLocal) tmp.friendButton.gameObject.SetActive(false);
                    else
                    {
                        if (Main.IsFriend(__instance.linePlayer.UserId))
                        {
                            Main.m_listCurrentSessionFriends.Add(__instance.linePlayer.UserId);
                            __instance.playerName.color = Main.m_clrFriendColor;
                            __instance.playerVRRig.playerText.color = Main.m_clrFriendColor;
                            tmp.friendButton.isOn = true;
                            tmp.friendButton.myText.text = tmp.friendButton.onText;
                            tmp.friendButton.UpdateColor();
                        }
                    }
                }
                else
                {
                    if (!__instance.linePlayer.IsLocal && tmp.friendButton.isOn != Main.IsInFriendList(__instance.linePlayer.UserId))
                    {
                        tmp.friendButton.isOn = !tmp.friendButton.isOn;
                        tmp.friendButton.UpdateColor();
                        if (!tmp.friendButton.isOn)
                        {
                            if (Main.IsVerified(__instance.linePlayer.UserId))
                            {
                                __instance.playerName.color = Main.m_clrVerifiedColor;
                                __instance.playerVRRig.playerText.color = Main.m_clrVerifiedColor;
                            }
                            else
                            {
                                __instance.playerName.color = Color.white;
                                __instance.playerVRRig.playerText.color = Color.white;
                            }
                            tmp.friendButton.myText.text = tmp.friendButton.offText;
                        }
                        else
                        {
                            __instance.playerName.color = Main.m_clrFriendColor;
                            __instance.playerVRRig.playerText.color = Main.m_clrFriendColor;
                            tmp.friendButton.myText.text = tmp.friendButton.onText;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(GorillaPlayerLineButton))]
    [HarmonyPatch("OnTriggerEnter", MethodType.Normal)]
    internal class GorillaPlayerLineButtonTriggerEnter
    {
        private static bool Prefix(GorillaPlayerLineButton __instance, Collider collider)
        {
            if (__instance.transform.name == Main.m_pScoreboardFriendBtn.transform.name)
            {
                //if (!__instance.enabled || __instance.touchTime + __instance.debounceTime >= Time.time) return false;

                GorillaTriggerColliderHandIndicator component = collider.GetComponent<GorillaTriggerColliderHandIndicator>();
                if(component != null) GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);

                __instance.isOn = !__instance.isOn;
                __instance.UpdateColor();
                if (__instance.isOn)
                {
                    Main.m_listCurrentSessionFriends.Add(__instance.parentLine.linePlayer.UserId);
                    PlayerPrefs.SetInt(__instance.parentLine.linePlayer.UserId + "_friend", 1);
                    __instance.parentLine.playerName.color = Main.m_clrFriendColor;
                    __instance.parentLine.playerVRRig.playerText.color = Main.m_clrFriendColor;
                    __instance.myText.text = __instance.onText;
                }
                else
                {
                    Main.m_listCurrentSessionFriends.Remove(__instance.parentLine.linePlayer.UserId);
                    PlayerPrefs.SetInt(__instance.parentLine.linePlayer.UserId + "_friend", 0);
                    __instance.myText.text = __instance.offText;
                    if (Main.IsVerified(__instance.parentLine.linePlayer.UserId))
                    {
                        __instance.parentLine.playerName.color = Main.m_clrVerifiedColor;
                        __instance.parentLine.playerVRRig.playerText.color = Main.m_clrVerifiedColor;
                    }
                    else
                    {
                        __instance.parentLine.playerName.color = Color.white;
                        __instance.parentLine.playerVRRig.playerText.color = Color.white;
                    }
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PhotonNetwork))]
    [HarmonyPatch("Disconnect", MethodType.Normal)]
    internal class OnRoomDisconnected
    {
        private static void Prefix()
        {
            Main.m_listCurrentSessionFriends.Clear();
        }
    }

    [HarmonyPatch(typeof(GorillaPlayerLineButton))]
    [HarmonyPatch("Update", MethodType.Normal)]
    internal class GorillaPlayerLineButtonUpdate
    {
        public static bool Prefix(GorillaPlayerLineButton __instance)
        {
            if (__instance == Main.m_pScoreboardFriendBtn) return false;
            return true;
        }
    }
}