using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.UI;
using static GorillaFriends.Main;

namespace GorillaFriends
{
    /* Friend Button's Script */
    public class FriendButton : MonoBehaviour
    {
        public GorillaPlayerScoreboardLine parentLine = null;
        public bool isOn = false;
        public string offText = "";
        public string onText = "";
        public Text myText = null;
        public Material offMaterial;
        public Material onMaterial;
        private MeshRenderer meshRenderer = null;
        private int initialisedForLobby = -1;
        private float nextUpdate = 0.0f;

        private static float nextTouch = 0.0f;
        public static int lobbyId = 1;

        private void Start()
        {
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
        }
        private void Update()
        {
            Photon.Realtime.Player playa = parentLine.linePlayer;
            if (nextUpdate > Time.time || parentLine.playerVRRig == null || playa == null) return;
            nextUpdate = Time.time + 0.5f;

            InitializeWithLine();

            if (playa != null && !playa.IsLocal && isOn != Main.IsInFriendList(playa.UserId))
            {
                isOn = !isOn;
                UpdateColor();
                if (!isOn)
                {
                    if (Main.IsVerified(playa.UserId))
                    {
                        parentLine.playerName.color = Main.m_clrVerified;
                        parentLine.playerVRRig.playerText.color = Main.m_clrVerified;
                    }
                    else
                    {
                        parentLine.playerName.color = Color.white;
                        parentLine.playerVRRig.playerText.color = Color.white;
                    }
                }
                else
                {
                    parentLine.playerName.color = Main.m_clrFriend;
                    parentLine.playerVRRig.playerText.color = Main.m_clrFriend;
                }
            }
        }
        public void InitializeWithLine()
        {
            /* Initialising for this lobby */
            //if (initialisedForLobby != lobbyId)
            {
                initialisedForLobby = lobbyId;
                string userId = parentLine.linePlayer.UserId;
                if (Main.IsVerified(userId))
                {
                    parentLine.playerName.color = Main.m_clrVerified;
                    parentLine.playerVRRig.playerText.color = Main.m_clrVerified;
                    if (parentLine.linePlayer.IsLocal) GorillaTagger.Instance.offlineVRRig.playerText.color = Main.m_clrVerified;
                }
                else if (parentLine.linePlayer.IsLocal)
                {
                    parentLine.playerName.color = Color.white;
                    parentLine.playerVRRig.playerText.color = Color.white;
                    GorillaTagger.Instance.offlineVRRig.playerText.color = Color.white;
                }

                if (parentLine.linePlayer.IsLocal) gameObject.SetActive(false);
                else
                {
                    if (Main.IsFriend(userId))
                    {
                        if (!Main.IsInFriendList(userId)) Main.m_listCurrentSessionFriends.Add(userId);
                        parentLine.playerName.color = Main.m_clrFriend;
                        parentLine.playerVRRig.playerText.color = Main.m_clrFriend;

                        isOn = true;
                        UpdateColor();
                    }
                    else
                    {
                        eRecentlyPlayed hasPlayedBefore = Main.HasPlayedWithUsRecently(userId);
                        if (!Main.NeedToCheckRecently(userId)) Main.m_listCurrentSessionRecentlyChecked.Add(userId);

                        if (hasPlayedBefore == Main.eRecentlyPlayed.Before)
                        {
                            PlayerPrefs.SetString(userId + "_pd", (((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds() + Main.moreTimeIfWeLagging).ToString());
                            parentLine.playerName.color = Main.m_clrPlayedRecently;
                            parentLine.playerVRRig.playerText.color = Main.m_clrPlayedRecently;
                        }
                        else
                        {
                            PlayerPrefs.SetString(userId + "_pd", ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString());
                            parentLine.playerName.color = Color.white;
                            parentLine.playerVRRig.playerText.color = Color.white;
                        }
                    }
                }
                return;
            }
        }
        private void OnTriggerEnter(Collider collider)
        {
            if (nextTouch > Time.time) return;
            GorillaTriggerColliderHandIndicator component = collider.GetComponent<GorillaTriggerColliderHandIndicator>();
            if (component == null) return;

            nextTouch = Time.time + 0.25f;
            isOn = !isOn;
            UpdateColor();

            GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);
            GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(67, component.isLeftHand, 0.05f);
            if(PhotonNetwork.InRoom && GorillaTagger.Instance.myVRRig != null)
            {
                GorillaTagger.Instance.myVRRig.RPC("PlayHandTap", RpcTarget.Others, (object)67, (object)component.isLeftHand, (object)0.05f);
            }

            if (isOn)
            {           
                Main.m_listCurrentSessionFriends.Add(parentLine.linePlayer.UserId);
                PlayerPrefs.SetInt(parentLine.linePlayer.UserId + "_friend", 1);
                parentLine.playerName.color = Main.m_clrFriend;
                parentLine.playerVRRig.playerText.color = Main.m_clrFriend;
                goto ENDING; /* GT 1.1.0 */
                //return;
            }

            Main.m_listCurrentSessionFriends.Remove(parentLine.linePlayer.UserId);
            PlayerPrefs.DeleteKey(parentLine.linePlayer.UserId + "_friend");
            if (Main.IsVerified(parentLine.linePlayer.UserId))
            {
                parentLine.playerName.color = Main.m_clrVerified;
                parentLine.playerVRRig.playerText.color = Main.m_clrVerified;
            }
            else
            {
                parentLine.playerName.color = Color.white;
                parentLine.playerVRRig.playerText.color = Color.white;
            }

            /* GT 1.1.0 */
          ENDING:
            if(!Main.m_bScoreboardTweakerMode)
            {
                //Main.Log("Initiating Scoreboard Redraw...");
                foreach (var sb in Main.m_listScoreboards)
                {
                    //Main.Log("Redrawing...");
                    sb.RedrawPlayerLines();
                }
            }
            /* GT 1.1.0 */
        }
        public void UpdateColor()
        {
            if (isOn)
            {
                if (meshRenderer != null) meshRenderer.material = onMaterial;
                myText.text = onText;
            }
            else
            {
                if (meshRenderer != null) meshRenderer.material = offMaterial;
                myText.text = offText;
            }
        }
    }
}