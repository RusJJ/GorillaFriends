using BepInEx;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GorillaFriends
{
    [BepInPlugin(ModConstants.ModConstants.modGUID, ModConstants.ModConstants.modName, ModConstants.ModConstants.modVersion)]
    public class Main : BaseUnityPlugin
    {
        internal static bool m_bScoreboardTweakerMode = false;
        internal static Main m_hInstance = null;
        internal static GameObject m_pScoreboardFriendBtn = null;
        internal static FriendButton m_pFriendButtonController = null;
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
        void OnScoreboardTweakerStart()
        {
            m_bScoreboardTweakerMode = true;
        }
        void OnScoreboardTweakerProcessedPre(GameObject scoreboardLinePrefab)
        {
            foreach (Transform t in scoreboardLinePrefab.transform)
            {
                if (t.name == "Mute Button")
                {
                    Main.m_pScoreboardFriendBtn = GameObject.Instantiate(t.gameObject);
                    if (Main.m_pScoreboardFriendBtn != null) // Who knows...
                    {
                        Main.m_pScoreboardFriendBtn.transform.GetChild(0).localScale = new Vector3(0.032f, 0.032f, 1.0f);
                        Main.m_pScoreboardFriendBtn.transform.GetChild(0).name = "Friend Text";
                        Main.m_pScoreboardFriendBtn.transform.parent = scoreboardLinePrefab.transform;
                        Main.m_pScoreboardFriendBtn.transform.name = "FriendButton";
                        Main.m_pScoreboardFriendBtn.transform.localPosition = new Vector3(18.0f, 0.0f, 0.0f);

                        var controller = Main.m_pScoreboardFriendBtn.GetComponent<GorillaPlayerLineButton>();
                        if (controller != null)
                        {
                            Main.m_pFriendButtonController = Main.m_pScoreboardFriendBtn.AddComponent<FriendButton>();
                            Main.m_pFriendButtonController.parentLine = controller.parentLine;
                            Main.m_pFriendButtonController.offText = "ADD\nFRIEND";
                            Main.m_pFriendButtonController.onText = "FRIEND!";
                            Main.m_pFriendButtonController.myText = controller.myText;
                            Main.m_pFriendButtonController.myText.text = Main.m_pFriendButtonController.offText;
                            Main.m_pFriendButtonController.offMaterial = controller.offMaterial;
                            Main.m_pFriendButtonController.onMaterial = new Material(controller.offMaterial);
                            Main.m_pFriendButtonController.onMaterial.color = Main.m_clrFriendColor;

                            GameObject.Destroy(controller);
                        }

                        Main.m_pScoreboardFriendBtn.transform.localPosition = new Vector3(-74.0f, 0.0f, 0.0f); // Should be -77, but i want more space between Mute and Friend button
                        Main.m_pScoreboardFriendBtn.transform.localScale = new Vector3(60.0f, t.localScale.y, 0.25f * t.localScale.z);
                        Main.m_pScoreboardFriendBtn.transform.GetChild(0).GetComponent<Text>().color = Color.clear;
                        GameObject.Destroy(Main.m_pScoreboardFriendBtn.transform.GetComponent<MeshRenderer>());
                    }
                    return;
                }
            }
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


    [HarmonyPatch(typeof(GorillaScoreBoard))]
    [HarmonyPatch("Awake", MethodType.Normal)]
    internal class GorillaScoreBoardAwake
    {
        private static void Prefix(GorillaScoreBoard __instance)
        {
            if (Main.m_bScoreboardTweakerMode || Main.m_pScoreboardFriendBtn != null) return;
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
                        var controller = Main.m_pScoreboardFriendBtn.GetComponent<GorillaPlayerLineButton>();
                        if (controller != null)
                        {
                            Main.m_pFriendButtonController = Main.m_pScoreboardFriendBtn.AddComponent<FriendButton>();
                            Main.m_pFriendButtonController.parentLine = controller.parentLine;
                            Main.m_pFriendButtonController.offText = "ADD\nFRIEND";
                            Main.m_pFriendButtonController.onText = "FRIEND!";
                            Main.m_pFriendButtonController.myText = controller.myText;
                            Main.m_pFriendButtonController.myText.text = Main.m_pFriendButtonController.offText;
                            Main.m_pFriendButtonController.offMaterial = controller.offMaterial;
                            Main.m_pFriendButtonController.onMaterial = new Material(controller.offMaterial);
                            Main.m_pFriendButtonController.onMaterial.color = Main.m_clrFriendColor;

                            GameObject.Destroy(controller);
                        }
                    }
                    return;
                }
            }
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
}