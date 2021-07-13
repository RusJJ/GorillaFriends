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
        public float touchTime = 0.0f;
        public Material offMaterial;
        public Material onMaterial;
        private MeshRenderer meshRenderer = null;
        private bool initialized = false;
        private float nextUpdate = 0.0f;

        private void Start()
        {
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                Main.Log("MeshRenderer is missing?");
            }
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
                    parentLine.playerName.color = Main.m_clrVerifiedColor;
                    parentLine.playerVRRig.playerText.color = Main.m_clrVerifiedColor;
                }

                if (parentLine.linePlayer.IsLocal) gameObject.SetActive(false);
                else
                {
                    if (Main.IsFriend(parentLine.linePlayer.UserId))
                    {
                        if (!Main.IsInFriendList(parentLine.linePlayer.UserId)) Main.m_listCurrentSessionFriends.Add(parentLine.linePlayer.UserId);
                        parentLine.playerName.color = Main.m_clrFriendColor;
                        parentLine.playerVRRig.playerText.color = Main.m_clrFriendColor;
                        isOn = true;
                        UpdateColor();
                    }
                }
                return;
            }

            if (!parentLine.linePlayer.IsLocal && isOn != Main.IsInFriendList(parentLine.linePlayer.UserId))
            {
                isOn = !isOn;
                UpdateColor();
                if (!isOn)
                {
                    if (Main.IsVerified(parentLine.linePlayer.UserId))
                    {
                        parentLine.playerName.color = Main.m_clrVerifiedColor;
                        parentLine.playerVRRig.playerText.color = Main.m_clrVerifiedColor;
                    }
                    else
                    {
                        parentLine.playerName.color = Color.white;
                        parentLine.playerVRRig.playerText.color = Color.white;
                    }
                }
                else
                {
                    parentLine.playerName.color = Main.m_clrFriendColor;
                    parentLine.playerVRRig.playerText.color = Main.m_clrFriendColor;
                }
            }
        }
        private void OnTriggerEnter(Collider collider)
        {
            if (touchTime > Time.time) return;
            touchTime = Time.time + 0.25f;
            GorillaTriggerColliderHandIndicator component = collider.GetComponent<GorillaTriggerColliderHandIndicator>();
            if (component == null) return;

            isOn = !isOn;
            UpdateColor();
            GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);

            if (isOn)
            {
                Main.m_listCurrentSessionFriends.Add(parentLine.linePlayer.UserId);
                PlayerPrefs.SetInt(parentLine.linePlayer.UserId + "_friend", 1);
                parentLine.playerName.color = Main.m_clrFriendColor;
                parentLine.playerVRRig.playerText.color = Main.m_clrFriendColor;
                return;
            }

            Main.m_listCurrentSessionFriends.Remove(parentLine.linePlayer.UserId);
            PlayerPrefs.SetInt(parentLine.linePlayer.UserId + "_friend", 0);
            if (Main.IsVerified(parentLine.linePlayer.UserId))
            {
                parentLine.playerName.color = Main.m_clrVerifiedColor;
                parentLine.playerVRRig.playerText.color = Main.m_clrVerifiedColor;
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