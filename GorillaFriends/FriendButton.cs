using System;
using UnityEngine;
using UnityEngine.UI;

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
        private bool initialized = false;
        private float nextUpdate = 0.0f;
        private static float nextTouch = 0.0f;

        private void Start()
        {
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
        }
        private void Update()
        {
            if (nextUpdate > Time.time || parentLine.playerVRRig == null || parentLine.linePlayer == null) return;
            nextUpdate = Time.time + 0.5f;

            /* First Initialization? */
            if (!initialized)
            {
                initialized = true;
                if (Main.IsVerified(parentLine.linePlayer.UserId))
                {
                    parentLine.playerName.color = Main.m_clrVerified;
                    parentLine.playerVRRig.playerText.color = Main.m_clrVerified;
                }

                if (parentLine.linePlayer.IsLocal) gameObject.SetActive(false);
                else
                {
                    if (Main.IsFriend(parentLine.linePlayer.UserId))
                    {
                        if (!Main.IsInFriendList(parentLine.linePlayer.UserId)) Main.m_listCurrentSessionFriends.Add(parentLine.linePlayer.UserId);
                        parentLine.playerName.color = Main.m_clrFriend;
                        parentLine.playerVRRig.playerText.color = Main.m_clrFriend;
                        isOn = true;
                        UpdateColor();
                    }
                    else
                    {
                        var hasPlayedBefore = Main.HasPlayedWithUsRecently(parentLine.linePlayer.UserId);
                        Main.Log(parentLine.linePlayer.NickName + " has been played: " + hasPlayedBefore.ToString());
                        if(hasPlayedBefore == Main.RecentlyPlayed.Before)
                        {
                            PlayerPrefs.SetString(parentLine.linePlayer.UserId + "_played", (((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds() + 1).ToString());
                            parentLine.playerName.color = Main.m_clrPlayedRecently;
                            parentLine.playerVRRig.playerText.color = Main.m_clrPlayedRecently;
                        }
                        else
                        {
                            PlayerPrefs.SetString(parentLine.linePlayer.UserId + "_played", ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString());
                        }
                    }
                }
                return;
            }

            if (parentLine.linePlayer != null && !parentLine.linePlayer.IsLocal && isOn != Main.IsInFriendList(parentLine.linePlayer.UserId))
            {
                isOn = !isOn;
                UpdateColor();
                if (!isOn)
                {
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
                }
                else
                {
                    parentLine.playerName.color = Main.m_clrFriend;
                    parentLine.playerVRRig.playerText.color = Main.m_clrFriend;
                }
            }
        }
        private void OnTriggerEnter(Collider collider)
        {
            if (nextTouch > Time.time) return;
            nextTouch = Time.time + 0.25f;
            GorillaTriggerColliderHandIndicator component = collider.GetComponent<GorillaTriggerColliderHandIndicator>();
            if (component == null) return;

            isOn = !isOn;
            UpdateColor();
            GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength, GorillaTagger.Instance.tapHapticDuration);

            if (isOn)
            {
                Main.m_listCurrentSessionFriends.Add(parentLine.linePlayer.UserId);
                PlayerPrefs.SetInt(parentLine.linePlayer.UserId + "_friend", 1);
                parentLine.playerName.color = Main.m_clrFriend;
                parentLine.playerVRRig.playerText.color = Main.m_clrFriend;
                return;
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