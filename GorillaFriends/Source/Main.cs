using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace GorillaFriends
{
    [BepInPlugin(ModConstants.ModConstants.modGUID, ModConstants.ModConstants.modName, ModConstants.ModConstants.modVersion)]
    public class Main : BaseUnityPlugin
    {
        public enum eRecentlyPlayed : byte
        {
            Never = 0,
            Before = 1,
            Now = 2,
        }
        internal static bool m_bScoreboardTweakerMode = false;
        internal static Main m_hInstance = null;
        internal static GameObject m_pScoreboardFriendBtn = null;
        internal static FriendButton m_pFriendButtonController = null;
        internal static List<string> m_listVerifiedUserIds = new List<string>();
        internal static List<string> m_listCurrentSessionFriends = new List<string>();
        internal static List<string> m_listCurrentSessionRecentlyChecked = new List<string>();
        internal static List<GorillaScoreBoard> m_listScoreboards = new List<GorillaScoreBoard>();
        internal static void Log(string msg) => m_hInstance.Logger.LogMessage(msg);
        public static Color m_clrFriend { get; internal set; } = new Color(0.8f, 0.5f, 0.9f, 1.0f);
        internal static string s_clrFriend;
        public static Color m_clrVerified { get; internal set; } = new Color(0.5f, 1.0f, 0.5f, 1.0f);
        internal static string s_clrVerified;
        public static Color m_clrPlayedRecently { get; internal set; } = new Color(1.0f, 0.67f, 0.67f, 1.0f);
        internal static string s_clrPlayedRecently;

        // These are little settings for us
        internal static byte moreTimeIfWeLagging = 5; // In case our game froze for a second or more
        internal static int howMuchSecondsIsRecently = 259200; // Just a time, equal to 3 days
        void Awake()
        {
            m_hInstance = this;
            WebVerified.LoadListOfVerified();
            HarmonyPatcher.Patch.Apply();

            var cfg = new ConfigFile(Path.Combine(Paths.ConfigPath, "GorillaFriends.cfg"), true);
            moreTimeIfWeLagging = cfg.Bind("Timings", "MoreTimeOnLag", (byte)5, "This is a little settings for us in case our game froze for a second or more").Value;
            howMuchSecondsIsRecently = cfg.Bind("Timings", "RecentlySeconds", 259200, "How much is \"recently\"?").Value;
            if (howMuchSecondsIsRecently < moreTimeIfWeLagging) howMuchSecondsIsRecently = moreTimeIfWeLagging;
            m_clrPlayedRecently = cfg.Bind("Colors", "RecentlyPlayedWith", m_clrPlayedRecently, "Color of \"Recently played with ...\"").Value;
            m_clrFriend = cfg.Bind("Colors", "Friend", m_clrFriend, "Color of FRIEND!").Value;

            byte[] clrizer = { (byte)(m_clrFriend.r * 255), (byte)(m_clrFriend.g * 255), (byte)(m_clrFriend.b * 255) };
            s_clrFriend = "\n <color=#" + ByteArrayToHexCode(clrizer) + ">";

            clrizer[0] = (byte)(m_clrVerified.r * 255); clrizer[1] = (byte)(m_clrVerified.g * 255); clrizer[2] = (byte)(m_clrVerified.b * 255);
            s_clrVerified = "\n <color=#" + ByteArrayToHexCode(clrizer) + ">";

            clrizer[0] = (byte)(m_clrPlayedRecently.r * 255); clrizer[1] = (byte)(m_clrPlayedRecently.g * 255); clrizer[2] = (byte)(m_clrPlayedRecently.b * 255);
            s_clrPlayedRecently = "\n <color=#" + ByteArrayToHexCode(clrizer) + ">";
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
                            Main.m_pFriendButtonController.onMaterial.color = Main.m_clrFriend;

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
        public static string ByteArrayToHexCode(byte[] arr)
        {
            StringBuilder hex = new StringBuilder(arr.Length * 2);
            foreach (byte b in arr)
                hex.AppendFormat("{0:X2}", b);
            return hex.ToString();
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
        public static bool NeedToCheckRecently(string userId)
        {
            foreach (string s in m_listCurrentSessionRecentlyChecked)
            {
                if (s == userId) return false;
            }
            return true;
        }
        public static eRecentlyPlayed HasPlayedWithUsRecently(string userId)
        {
            long time = long.Parse(PlayerPrefs.GetString(userId + "_played", "0"));
            long curTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
            if (time == 0) return eRecentlyPlayed.Never;
            if (time > curTime - moreTimeIfWeLagging && time <= curTime) return eRecentlyPlayed.Now;
            return ((time + howMuchSecondsIsRecently) > curTime) ? eRecentlyPlayed.Before : eRecentlyPlayed.Never;
        }
    }

    /* GT 1.1.69+ */
    [HarmonyPatch(typeof(GorillaScoreBoard))]
    [HarmonyPatch("RedrawPlayerLines", MethodType.Normal)]
    internal class GorillaScoreBoardRedrawPlayerLines
    {
        private static bool Prefix(GorillaScoreBoard __instance)
        {
            if (Main.m_bScoreboardTweakerMode) return true;

            //__instance.lines.Sort((Comparison<GorillaPlayerScoreboardLine>)((line1, line2) => line1.playerActorNumber.CompareTo(line2.playerActorNumber))); // leftover from GTag 1.1.0?
            __instance.boardText.text = __instance.GetBeginningString();
            __instance.buttonText.text = "";
            for (int i = 0; i < __instance.lines.Count; ++i)
            {
                try
                {
                    if (__instance.lines[i].gameObject.activeInHierarchy)
                    {
                        __instance.lines[i].gameObject.GetComponent<RectTransform>().localPosition = new Vector3(0f, (float)(__instance.startingYValue - __instance.lineHeight * i), 0f);
                        if (__instance.lines[i].linePlayer != null)
                        {
                            var usrid = __instance.lines[i].linePlayer.UserId;
                            var txtusr = __instance.lines[i].playerVRRig.playerText;

                            Text boardText = __instance.boardText;
                            if (Main.IsInFriendList(usrid))
                            {
                                boardText.text += Main.s_clrFriend + __instance.NormalizeName(true, __instance.lines[i].linePlayer.NickName) + "</color>";
                                txtusr.color = Main.m_clrFriend;
                            }
                            else if (Main.IsVerified(usrid))
                            {
                                boardText.text += Main.s_clrVerified + __instance.NormalizeName(true, __instance.lines[i].linePlayer.NickName) + "</color>";
                                txtusr.color = Main.m_clrVerified;
                                if (__instance.lines[i].linePlayer.IsLocal) GorillaTagger.Instance.offlineVRRig.playerText.color = Main.m_clrVerified;
                            }
                            else if (!Main.NeedToCheckRecently(usrid) && Main.HasPlayedWithUsRecently(usrid) == Main.eRecentlyPlayed.Before)
                            {
                                boardText.text += Main.s_clrPlayedRecently + __instance.NormalizeName(true, __instance.lines[i].linePlayer.NickName) + "</color>";
                                txtusr.color = Main.m_clrPlayedRecently;
                            }
                            else
                            {
                                boardText.text += "\n " + __instance.NormalizeName(true, __instance.lines[i].linePlayer.NickName);
                                txtusr.color = Color.white;
                            }
                            if (__instance.lines[i].linePlayer != PhotonNetwork.LocalPlayer)
                            {
                                if (__instance.lines[i].reportButton.isActiveAndEnabled)
                                {
                                    __instance.buttonText.text += "FRIEND       MUTE                      REPORT\n";
                                }
                                else
                                {
                                    __instance.buttonText.text += "FRIEND       MUTE      HATE SPEECH    TOXICITY      CHEATING      CANCEL\n";
                                }
                            }
                            else
                            {
                                __instance.buttonText.text += "\n";
                            }
                        }
                    }
                }
                catch
                {
                    // Error message supposed to be here?!
                }
            }
            return false;
        }
    }
    /* GT 1.1.69+ */

    // We are not supporting ScoreboardTweaks for now. Because it`s not updated.
    //[HarmonyPatch(typeof(GorillaScoreBoard))]
    //[HarmonyPatch("Awake", MethodType.Normal)]
    internal class GorillaScoreBoardAwake
    {
        private static void Prefix(GorillaScoreBoard __instance)
        {
            Main.m_listScoreboards.Add(__instance);
            __instance.boardText.supportRichText = true;

            var ppTmp = __instance.buttonText.transform.localPosition;
            var sd = __instance.buttonText.rectTransform.sizeDelta;
            __instance.buttonText.transform.localPosition = new Vector3(
                ppTmp.x - 3.0f,
                ppTmp.y,
                ppTmp.z
            );
            __instance.buttonText.rectTransform.sizeDelta = new Vector2(sd.x + 4.0f, sd.y);

            if (Main.m_bScoreboardTweakerMode || Main.m_pScoreboardFriendBtn != null) return;

            foreach (Transform t in __instance.scoreBoardLinePrefab.transform)
            {
                if (t.name == "Mute Button")
                {
                    Main.Log("Instanciating MuteBtn...");
                    Main.m_pScoreboardFriendBtn = GameObject.Instantiate(t.gameObject);
                    if (Main.m_pScoreboardFriendBtn != null) // Who knows...
                    {
                        Main.Log("Setting FriendBtn...");
                        t.localPosition = new Vector3(17.5f, 0.0f, 0.0f); // Move MuteButton a bit to right
                        Main.m_pScoreboardFriendBtn.transform.parent = __instance.scoreBoardLinePrefab.transform;
                        Main.m_pScoreboardFriendBtn.transform.name = "FriendButton";
                        Main.m_pScoreboardFriendBtn.transform.localPosition = new Vector3(3.8f, 0.0f, 0.0f);
                        var controller = Main.m_pScoreboardFriendBtn.GetComponent<GorillaPlayerLineButton>();
                        if (controller != null)
                        {
                            Main.Log("Replacing controller...");
                            Main.m_pFriendButtonController = Main.m_pScoreboardFriendBtn.AddComponent<FriendButton>();
                            Main.m_pFriendButtonController.parentLine = controller.parentLine;
                            Main.m_pFriendButtonController.offText = "ADD\nFRIEND";
                            Main.m_pFriendButtonController.onText = "FRIEND!";
                            Main.m_pFriendButtonController.myText = controller.myText;
                            Main.m_pFriendButtonController.myText.text = Main.m_pFriendButtonController.offText;
                            Main.m_pFriendButtonController.offMaterial = controller.offMaterial;
                            Main.m_pFriendButtonController.onMaterial = new Material(controller.offMaterial);
                            Main.m_pFriendButtonController.onMaterial.color = Main.m_clrFriend;

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
            try
            {
                if (!PhotonNetwork.InRoom) return;
                Main.m_listScoreboards.Clear();
                Main.m_listCurrentSessionFriends.Clear();
                Main.m_listCurrentSessionRecentlyChecked.Clear(); // Im too lazy to do a lil cleanup on our victims disconnect...
            }
            catch
            {
                // Who knows what's gonna happen, lol?
                // Should be safe but lets be honest -
                //   we dont wanna ruin someone's experience because of us!
            }
        }
    }
}