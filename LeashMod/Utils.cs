using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UIExpansionKit.API;
using UnityEngine;
using VRC;
using VRC.UI.Elements.Menus;

namespace LeashMod
{
    internal class Utils
    {
        internal static List<Player> GetAllPlayers()
        {
            return PlayerManager.field_Private_Static_PlayerManager_0?.field_Private_List_1_Player_0?.ToArray()?.ToList();
        }

        internal static Player GetCurrentlySelectedPlayer()
        {
            if (GameObject.Find("UserInterface")?.GetComponentInChildren<SelectedUserMenuQM>() == null)
            {
                return null;
            }

            return GetPlayerFromIDInLobby(GameObject.Find("UserInterface")?.GetComponentInChildren<SelectedUserMenuQM>()?.field_Private_IUser_0?.prop_String_0);
        }

        internal static Player GetPlayerFromIDInLobby(string id)
        {
            var all_player = GetAllPlayers();

            foreach (var player in all_player)
            {
                if (player != null && player.prop_APIUser_0 != null)
                {
                    if (player.prop_APIUser_0.id == id)
                    {
                        return player;
                    }
                }
            }

            return null;
        }

        internal static string CreateSHA256(string rawData)
        {
            // Create a SHA256
            using (var sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                var bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string
                var builder = new StringBuilder();
                for (var i = 0; i < bytes.Length; i++)
                {
                    var t = bytes[i];
                    builder.Append(t.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        internal static void ChillOkayPopup(string Title, string Content, PopupType type, string OkayText = "Okay", Action OkayAction = null)
        {
            ICustomShowableLayoutedMenu Popup = null;

            if (type == PopupType.FullScreen)
            {
                Popup = ExpansionKitApi.CreateCustomFullMenuPopup(LayoutDescription.WideSlimList);
            }
            else
            {
                Popup = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);
            }

            Popup.AddSimpleButton(Title, delegate() { });
            Popup.AddLabel(Content);
            Popup.AddSpacer();
            Popup.AddSpacer();
            Popup.AddSpacer();
            Popup.AddSpacer();
            Popup.AddSpacer();
            Popup.AddSimpleButton(OkayText, () =>
            {
                Popup.Hide();
                OkayAction?.Invoke();
            });

            Popup.Show();
        }

        public static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
        {
            return new Vector3(current.x + (target.x - current.x) * maxDistanceDelta, current.y + (target.y - current.y) * maxDistanceDelta, current.z + (target.z - current.z) * maxDistanceDelta);
        }

        public static Vector3 MoveTowardsInPerspective(Vector3 InPerspectiveOf, Vector3 PosToMove, Vector3 ToThis, float Speed)
        {
            //May Need -
            var NewPos = Vector3.MoveTowards(PosToMove, ToThis - (InPerspectiveOf - PosToMove), Speed);

            return NewPos;
        }

        public static Vector3 Distance(Vector3 current, Vector3 target, bool Normalize = false)
        {
            if (Normalize)
            {
                return (target - current).normalized;
            }

            return target - current;
        }

        public static int RangeConv(float input, float MinPossibleInput, float MaxPossibleInput, float MinConv, float MaxConv)
        {
            return Convert.ToInt32((input - MinPossibleInput) * (MaxConv - MinConv) / (MaxPossibleInput - MinPossibleInput) + MinConv);
        }

        public static float RangeConvF(float input, float MinPossibleInput, float MaxPossibleInput, float MinConv, float MaxConv)
        {
            return (input - MinPossibleInput) * (MaxConv - MinConv) / (MaxPossibleInput - MinPossibleInput) + MinConv;
        }

        internal enum PopupType
        {
            FullScreen,
            QuickMenu
        }
    }

    internal static class UtilsExt
    {
        public static readonly Vector4 ms_pointVector = new Vector4(0f, 0f, 0f, 1f);

        // VRChat related
        public static VRC.Player GetLocalPlayer() => VRC.Player.prop_Player_0;

        public static bool IsFriend(VRC.Player p_player)
        {
            bool l_result = false;
            if (p_player.field_Private_APIUser_0 != null)
                l_result = p_player.field_Private_APIUser_0.isFriend;
            return l_result;
        }

        public static Il2CppSystem.Collections.Generic.List<VRC.Player> GetPlayers() => VRC.PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0;

        public static System.Collections.Generic.List<VRC.Player> GetFriendsInInstance()
        {
            System.Collections.Generic.List<VRC.Player> l_result = new System.Collections.Generic.List<VRC.Player>();
            var l_remotePlayers = GetPlayers();
            if (l_remotePlayers != null)
            {
                foreach (VRC.Player l_remotePlayer in l_remotePlayers)
                {
                    if ((l_remotePlayer != null) && IsFriend(l_remotePlayer))
                        l_result.Add(l_remotePlayer);
                }
            }
            return l_result;
        }

        public static VRCTrackingManager GetVRCTrackingManager() => VRCTrackingManager.field_Private_Static_VRCTrackingManager_0;
        public static VRCTrackingSteam GetVRCTrackingSteam() => GetVRCTrackingManager().field_Private_List_1_VRCTracking_0.ToArray()[0].TryCast<VRCTrackingSteam>();
        public static Transform GetTrackingLeftController() => GetVRCTrackingSteam().field_Private_SteamVR_ControllerManager_0.field_Public_GameObject_0.transform;
        public static Transform GetTrackingRightController() => GetVRCTrackingSteam().field_Private_SteamVR_ControllerManager_0.field_Public_GameObject_1.transform;

        // RootMotion.FinalIK.IKSolverVR extensions
        public static void SetLegIKWeight(this RootMotion.FinalIK.IKSolverVR p_solver, HumanBodyBones p_leg, float p_weight)
        {
            var l_leg = (p_leg == HumanBodyBones.LeftFoot) ? p_solver.leftLeg : p_solver.rightLeg;
            if (l_leg != null)
            {
                l_leg.positionWeight = p_weight;
                l_leg.rotationWeight = p_weight;
            }
        }

        // Math extensions
        public static Matrix4x4 GetMatrix(this Transform p_transform, bool p_pos = true, bool p_rot = true, bool p_scl = false)
        {
            return Matrix4x4.TRS(p_pos ? p_transform.position : Vector3.zero, p_rot ? p_transform.rotation : Quaternion.identity, p_scl ? p_transform.localScale : Vector3.one);
        }
        public static Matrix4x4 AsMatrix(this Quaternion p_quat)
        {
            return Matrix4x4.Rotate(p_quat);
        }

        public static T GetAddComponent<T>(this GameObject obj) where T : Behaviour
        {
            T comp;

            try
            {
                comp = obj.GetComponent<T>();

                if (comp == null)
                {
                    comp = obj.AddComponent<T>();
                }
            }
            catch
            {
                comp = obj.AddComponent<T>();
            }

            return comp;
        }

        public static T GetAddComponent<T>(this Transform obj) where T : Behaviour
        {
            T comp;

            try
            {
                comp = obj.gameObject.GetComponent<T>();

                if (comp == null)
                {
                    comp = obj.gameObject.AddComponent<T>();
                }
            }
            catch
            {
                comp = obj.gameObject.AddComponent<T>();
            }

            return comp;
        }
    }
}