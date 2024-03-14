using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using DiscordRPC;
using DiscordRPC.Unity;
using Leash.IL2CPPAssetBundleAPI;
using LeashMod.Loader.Types;
using MelonLoader;
using PlagueButtonAPI;
using PlagueButtonAPI.Controls;
using PlagueButtonAPI.Controls.Grouping;
using PlagueButtonAPI.Misc;
using PlagueButtonAPI.Pages;
using TMPro;
using UIExpansionKit.API;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using VRC.Core;
using VRC.Dynamics;
using Button = DiscordRPC.Button;
using Label = PlagueButtonAPI.Controls.Label;
using MessageBox = User32.MessageBox;
using Object = UnityEngine.Object;
#if !Free
using LeashMod.External_Libraries;
#endif
#if !Free
using System.Collections;
using System.Linq;
using Libraries;
#endif
#if !Free
using UnityEngine.XR;
using VRC;
#endif
#if !Free
using Slider = PlagueButtonAPI.Controls.Slider;
using VRC.Playables;
#endif
#if !Free
using Environment = System.Environment;
using VRC.SDKBase;
#endif

[assembly: MelonOptionalDependencies(new []{ "TouchTriggers" })]

namespace LeashMod
{
    public class LeashMod : LM
    {
        internal static DiscordRpcClient PresenceClient;

        private static RichPresence PresenceData;

        private static PresenceState CurrentPresenceState = PresenceState.Idle;
        private IL2CPPAssetBundle Bundle = new IL2CPPAssetBundle();

        //private GameObject GagIcon;

        private bool HasInit = false;
        private Sprite LeashModLogo;

        private static RichPresence CurrentPresence => RefreshPresence();

        //private Restraint LeftHandIsBoundTo;
        //private Restraint RightHandIsBoundTo;

        private bool TouchTriggersIsPresent;

        public override void OnApplicationStart()
        {
            InitPresence();

            ClassInjector.RegisterTypeInIl2Cpp<ObjectListener>();
            //ClassInjector.RegisterTypeInIl2Cpp<Restraint>();

            if (Bundle.LoadBundle("LeashMod.Resources.LeashMod.assetbundle"))
            {
                LeashModLogo = Bundle.Load<Sprite>("LeashMod");
            }

            #if !Free

            JsonConfig.LoadConfig(ref Config, Environment.CurrentDirectory + "\\LeashMod.json");

            MelonCoroutines.Start(Loop());

            Hooks.OnAvatarInstantiated += HooksOnOnAvatarInstantiated;

            TouchTriggersIsPresent = MelonHandler.Mods.Any(o => o.Info.Name == "TouchTriggers");

            if (TouchTriggersIsPresent)
            {
                InitTriggers();
            }
            #endif
        }

        private void InitTriggers()
        {
            TouchTriggers.ScriptableReceivers.OnCollisionEnter += OnCollisionEnter;
            TouchTriggers.ScriptableReceivers.OnCollisionExit += OnCollisionExit;
        }

        private void HooksOnOnAvatarInstantiated(VRCAvatarManager manager, ApiAvatar arg2, GameObject obj)
        {
            var AllTransforms = obj.GetComponentsInChildren<Transform>(true);

            if (manager.field_Private_VRCPlayer_0 == VRCPlayer.field_Internal_Static_VRCPlayer_0)
            {
                //RestraintL = manager.field_Private_Animator_0.GetBoneTransform(HumanBodyBones.LeftHand).gameObject.AddComponent<Restraint>();

                //RestraintR = manager.field_Private_Animator_0.GetBoneTransform(HumanBodyBones.RightHand).gameObject.AddComponent<Restraint>();

                //RestraintL.restraintType = Restraint.RestraintType.LeftHand;
                //RestraintR.restraintType = Restraint.RestraintType.RightHand;

                LeashFromObject =
                    AllTransforms.FirstOrDefault(o => o != null && o.name == "Plague_LeashFromMe")?.transform
                    ??
                    obj.transform.Find("ForwardDirection/Avatar")?.GetComponent<Animator>()?.GetBoneTransform(HumanBodyBones.Neck);
            }
            else if (obj.transform.root.GetComponent<Player>().field_Private_APIUser_0.id == Config.MasterID)
            {
                MasterObj = obj;

                var FemaleCheck = IsAvatarFemale(manager);

                Config.MasterTag = FemaleCheck.Item1 ? "Mistress" : "Master";

                Log($"{Config.MasterTag ?? ""}'s Avatar Is {Enum.GetName(typeof(VRC_AvatarDescriptor.AnimationSet), FemaleCheck.Item2)}!");
                JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");

                LeashToObject =
                    AllTransforms.FirstOrDefault(o => o != null && o.name == "Plague_LeashToMe")?.transform
                    ??
                    obj.transform.Find("ForwardDirection/Avatar")?.GetComponent<Animator>()?.GetBoneTransform(HumanBodyBones.RightHand);

                LeashLockParam = null;
                LeashImmersionParam = null;
                LeashLengthParam = null;

                if (manager.HasCustomExpressions() && manager.GetAvatarParameters() is var Params && Params != null && Params.Any() && Config.IsLocked)
                {
                    LeashLockParam = Params.FirstOrDefault(o => o.field_Private_String_0 == "LeashMod_LockToggle" && o.field_Public_ParameterType_0 == AvatarParameter.ParameterType.Bool);
                    LeashImmersionParam = Params.FirstOrDefault(o => o.field_Private_String_0 == "LeashMod_ImmersionToggle" && o.field_Public_ParameterType_0 == AvatarParameter.ParameterType.Bool);

                    LeashLengthParam = Params.FirstOrDefault(o => o.field_Private_String_0 == "LeashMod_LeashLength" && o.field_Public_ParameterType_0 == AvatarParameter.ParameterType.Float);

                    if (LeashLockParam != null && LeashImmersionParam != null && LeashLengthParam != null)
                    {
                        Log($"{Config.MasterTag ?? ""}'s Avatar Has {Config.MasterTag ?? ""}-Control Support, Controls Will Be Hidden.");
                    }
                    else
                    {
                        Log($"{Config.MasterTag ?? ""}'s Avatar Does Not {Config.MasterTag ?? ""}-Control Support, Controls Will Be Shown.");
                    }
                }

                LeashLock?.SetActive(LeashLockParam == null);
                LeashLengthSlider?.SetActive(LeashLengthParam == null);

                var Handler = obj.transform.root.GetComponent<ObjectHandler>();

                if (Handler == null)
                {
                    Handler = obj.transform.root.gameObject.AddComponent<ObjectHandler>();

                    Handler.OnDestroyed += _ =>
                    {
                        Log($"{Config.MasterTag ?? ""} Has Left!");

                        LeashLockParam = null;
                        LeashImmersionParam = null;
                        LeashLengthParam = null;

                        LeashLengthSlider?.SetActive(true);
                    };
                }
            }
        }

        private bool UnlockPending;
        private bool IsGagged;

        private void OnCollisionEnter(ContactReceiver arg1, ContactSender arg2)
        {
            if (arg1?.shape?.component != null && arg2?.shape?.component != null && arg1.transform.root.GetComponent<Player>() == Player.prop_Player_0 && arg2.transform.root.GetComponent<Player>() == Master) // From Master To Us.
            {
                if (arg1.name == "LeashModUnlockReceiver" && arg2.name == "LeashModUnlockSender")
                {
                    // Key To Collar
                    UnlockPending = true;
                }
                else if (arg1.name.Contains("LeashModGagZone"))
                {
                    IsGagged = true;
                }
            }
        }

        private void OnCollisionExit(ContactReceiver arg1, ContactSender arg2)
        {
            if (arg1?.shape?.component != null && arg2?.shape?.component != null && arg1.transform.root.GetComponent<Player>() == Player.prop_Player_0 && arg2.transform.root.GetComponent<Player>() == Master) // From Master To Us.
            {
                if (arg1.name.Contains("LeashModGagZone"))
                {
                    IsGagged = false;
                }
            }
        }

        public override void OnApplicationQuit()
        {
            PresenceClient.ClearPresence();
            PresenceClient.Dispose();
        }

        private static RichPresence RefreshPresence()
        {
            if (PresenceData == null)
            {
                PresenceData = new RichPresence
                {
                    Details = "Created By Plague/Meru",

                    Assets = new Assets
                    {
                        LargeImageKey = "logo",
                        LargeImageText = "LeashMod By Plague/Meru" + Environment.NewLine + "https://LeashMod.com",
                        SmallImageKey = "meru",
                        SmallImageText = "Play Type: " + (XRDevice.isPresent ? "VR" : "Desktop")
                    },

                    Buttons = new[]
                    {
                        new Button { Label = "Discord", Url = "https://discord.gg/snvPDskTPG" }
                    },

                    Timestamps = Timestamps.Now
                };
            }

            switch (CurrentPresenceState)
            {
                case PresenceState.Idle:
                    PresenceData.State = "";
                    break;
                case PresenceState.Loading:
                    PresenceData.State = "Loading A World..";
                    break;
                case PresenceState.InWorld:
                    PresenceData.State = "In A " + RoomManager.field_Internal_Static_ApiWorldInstance_0?.type.ToString()?.Replace("Only", " Only")?.Replace("Plus", "+")?.Replace("OfGuests", "+") + " World";
                    break;
            }

            return PresenceData;
        }

        private static void InitPresence()
        {
            ConfigManager.LocalConfig.Cast<LocalConfig>().SetValue("disableRichPresence", new Il2CppSystem.Boolean { m_value = true }.BoxIl2CppObject());

            using (var webClient = new WebClient())
            {
                if (!Directory.Exists(Environment.CurrentDirectory + "\\LeashMod"))
                {
                    Directory.CreateDirectory(Environment.CurrentDirectory + "\\LeashMod");
                }

                webClient.DownloadFile("http://LeashMod.com/RPC/discord-rpc.dll", Environment.CurrentDirectory + "\\LeashMod\\discord-rpc.dll");
            }

            if (PresenceClient == null)
            {
                PresenceClient = new DiscordRpcClient("948880375873171516", client: new UnityNamedPipe());
            }

            if (!PresenceClient.IsInitialized)
            {
                PresenceClient?.Initialize();
            }

            MelonCoroutines.Start(RunMe());

            IEnumerator RunMe()
            {
                while (PresenceClient.CurrentUser == null)
                {
                    yield return new WaitForEndOfFrame();
                }

                yield return new WaitForSeconds(2f);

                PresenceClient?.SetPresence(CurrentPresence);

                yield break;
            }
        }

        private enum PresenceState
        {
            Idle,
            Loading,
            InWorld
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "ui" && !HasInit)
            {
                HasInit = true;

                ButtonAPI.OnInit += () =>
                {
                    //GagIcon = Object.Instantiate(GameObject.Find("UserInterface").transform.Find("UnscaledUI/HudContent/Hud/VoiceDotParent/VoiceDot")?.gameObject, GameObject.Find("UserInterface").transform.Find("/UnscaledUI/HudContent/Hud/VoiceDotParent"), true);

                    //if (GagIcon?.GetComponent<Image>() != null)
                    //{
                    //    GagIcon.GetComponent<Image>().enabled = true;
                    //    GagIcon.transform.localPosition = new Vector3(-0.0011f, 125f, 0f);
                    //    GagIcon.GetComponent<Image>().sprite = GaggedLogo;
                    //    GagIcon.GetComponent<Image>().color = Color.white;

                    //    GagIcon.SetActive(false);
                    //}
                    //else
                    //{
                    //    MelonLogger.Error("GagIcon == null");
                    //}

                    #if !Free
                    var MasterButtonLeft = new WingSingleButton(WingSingleButton.Wing.Left, "Set As Master/Mistress", "Sets The Currently Selected Player As Your Master/Mistress.", () =>
                    {
                        var PlayerSelected = Utils.GetCurrentlySelectedPlayer();

                        if (PlayerSelected == null)
                        {
                            return;
                        }

                        if (Config.IsLeashed)
                        {
                            ButtonAPI.GetQuickMenuInstance().ShowAlert("You Are Already Leashed!");
                            return;
                        }

                        if (PlayerSelected.field_Private_APIUser_0.id == APIUser.CurrentUser.id)
                        {
                            ButtonAPI.GetQuickMenuInstance().ShowAlert("No. :)");
                            return;
                        }

                        Master = PlayerSelected;

                        Config.MasterDisplayName = PlayerSelected.field_Private_APIUser_0.displayName;
                        Config.MasterID = PlayerSelected.field_Private_APIUser_0.id;
                        JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");

                        ButtonAPI.GetQuickMenuInstance().ShowAlert("Your Master Has Been Set!");

                        HooksOnOnAvatarInstantiated(Master._vrcplayer.prop_VRCAvatarManager_0, null, Master._vrcplayer.prop_VRCAvatarManager_0.prop_GameObject_0);
                    }, false);

                    MasterButtonLeft.gameObject.GetAddComponent<ObjectHandler>().OnUpdateEachSecond = (obj, enabled) =>
                    {
                        var PlayerSelected = Utils.GetCurrentlySelectedPlayer();

                        if (PlayerSelected != null)
                        {
                            MasterButtonLeft.SetText("Set As " + (IsAvatarFemale(PlayerSelected._vrcplayer.prop_VRCAvatarManager_0).Item1 ? "Mistress" : "Master"));
                            MasterButtonLeft.SetInteractable(true);
                        }
                        else
                        {
                            MasterButtonLeft.SetText("Set As Master/Mistress");
                            MasterButtonLeft.SetInteractable(false);
                        }
                    };

                    var MasterButtonRight = new WingSingleButton(WingSingleButton.Wing.Right, "Set As Master/Mistress", "Sets The Currently Selected Player As Your Master/Mistress.", () =>
                    {
                        var PlayerSelected = Utils.GetCurrentlySelectedPlayer();

                        if (PlayerSelected == null)
                        {
                            return;
                        }

                        if (Config.IsLeashed)
                        {
                            ButtonAPI.GetQuickMenuInstance().ShowAlert("You Are Already Leashed!");
                            return;
                        }

                        if (PlayerSelected.field_Private_APIUser_0.id == APIUser.CurrentUser.id)
                        {
                            ButtonAPI.GetQuickMenuInstance().ShowAlert("No. :)");
                            return;
                        }

                        Master = PlayerSelected;

                        Config.MasterDisplayName = PlayerSelected.field_Private_APIUser_0.displayName;
                        Config.MasterID = PlayerSelected.field_Private_APIUser_0.id;
                        JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");

                        ButtonAPI.GetQuickMenuInstance().ShowAlert("Your Master Has Been Set!");

                        HooksOnOnAvatarInstantiated(Master._vrcplayer.prop_VRCAvatarManager_0, null, Master._vrcplayer.prop_VRCAvatarManager_0.prop_GameObject_0);
                    }, false);

                    MasterButtonRight.gameObject.GetAddComponent<ObjectHandler>().OnUpdateEachSecond = (obj, enabled) =>
                    {
                        var PlayerSelected = Utils.GetCurrentlySelectedPlayer();

                        if (PlayerSelected != null)
                        {
                            MasterButtonRight.SetText("Set As " + (IsAvatarFemale(PlayerSelected._vrcplayer.prop_VRCAvatarManager_0).Item1 ? "Mistress" : "Master"));
                            MasterButtonRight.SetInteractable(true);
                        }
                        else
                        {
                            MasterButtonRight.SetText("Set As Master/Mistress");
                            MasterButtonRight.SetInteractable(false);
                        }
                    };
                    #endif

                    var Page = MenuPage.CreatePage(WingSingleButton.Wing.Left, LeashModLogo, "LeashMod_1", "Leash Mod", expandButton: true, expandButtonSprite: LeashModLogo).Item1;

                    new WingSingleButton(WingSingleButton.Wing.Right, "Leash Mod", "Created By Plague", Page.OpenMenu, true, LeashModLogo); // For Both Sides

                    #if !Free
                    var InfoGroup = Page.AddButtonGroup("Info");

                    InfoGroup.AddLabel("Authed:\ntrue", "Authed:\ntrue");

                    var IDLabel = InfoGroup.AddLabel($"{Config.MasterTag ?? ""} ID: {Config.MasterID ?? ""}", $"{Config.MasterID ?? ""}");

                    IDLabel.LabelButton.gameObject.GetAddComponent<ObjectHandler>().OnUpdateEachSecond += (obj, IsEnabled) =>
                    {
                        if (IsEnabled)
                        {
                            IDLabel.LabelButton.SetText($"{Config.MasterTag ?? ""} ID: {Config.MasterID ?? ""}");
                            IDLabel.LabelButton.SetTooltip($"{Config.MasterID ?? ""}");
                        }
                    };

                    var NameLabel = InfoGroup.AddLabel($"{Config.MasterTag ?? ""} Name: {Config.MasterDisplayName ?? ""}", $"{Config.MasterDisplayName ?? ""}");

                    NameLabel.LabelButton.gameObject.GetAddComponent<ObjectHandler>().OnUpdateEachSecond += (obj, IsEnabled) =>
                    {
                        if (IsEnabled)
                        {
                            NameLabel.LabelButton.SetText($"{Config.MasterTag ?? ""} Name: {Config.MasterDisplayName ?? ""}");
                            NameLabel.LabelButton.SetTooltip($"{Config.MasterDisplayName ?? ""}");
                        }
                    };

                    var PresentLabel = InfoGroup.AddLabel($"{Config.MasterTag ?? ""} Present: false", "Not Present");

                    PresentLabel.LabelButton.gameObject.GetAddComponent<ObjectHandler>().OnUpdateEachSecond += (obj, IsEnabled) =>
                    {
                        if (IsEnabled)
                        {
                            PresentLabel.LabelButton.SetText($"{Config.MasterTag ?? ""} Present: {(Master != null ? "true" : "false")}");
                            PresentLabel.LabelButton.SetTooltip($"{(Master != null ? "Present" : "Not Present")}");
                        }
                    };
                    #endif
                    var OptionsGroup = Page.AddButtonGroup("Leash Options");
                    #if !Free
                    Leash = OptionsGroup.AddToggleButton("Leash", "Enable Leash", "Disable Leash", val =>
                    {
                        if (string.IsNullOrWhiteSpace(Config.MasterID))
                        {
                            ButtonAPI.GetQuickMenuInstance().ShowAlert($"You Don't Currently Have A {Config.MasterTag ?? ""} Set! This Will Not Do Anything Until You Set One!");
                            Leash.SetToggleState(false);
                            return;
                        }

                        if (Master == null && val)
                        {
                            ButtonAPI.GetQuickMenuInstance().ShowAlert($"Your {Config.MasterTag ?? ""} Is Not Currently In Your Lobby, So Nothing Will Happen Yet.");
                        }

                        Config.IsLeashed = val;
                        JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");
                    });
                    Leash.SetToggleState(Config.IsLeashed);

                    LeashLock = OptionsGroup.AddToggleButton("Lock Leash", "Lock Leash", "Unlock Leash", val =>
                    {
                        if (string.IsNullOrWhiteSpace(Config.MasterID))
                        {
                            ButtonAPI.GetQuickMenuInstance().ShowAlert($"You Don't Currently Have A {Config.MasterTag ?? ""} Set! This Will Not Do Anything Until You Set One!");
                            LeashLock.SetToggleState(false);
                            return;
                        }

                        if (!Config.IsLeashed)
                        {
                            ButtonAPI.GetQuickMenuInstance().ShowAlert("You're Not Currently Leashed!");
                            LeashLock.SetToggleState(false);
                            return;
                        }

                        if (Master == null && val)
                        {
                            ButtonAPI.GetQuickMenuInstance().ShowAlert($"Your {Config.MasterTag ?? ""} Is Not Currently In Your Lobby, So Nothing Will Happen Yet.");
                        }

                        if (val)
                        {
                            ButtonAPI.GetQuickMenuInstance().ShowConfirmDialog("Alert", $"Are You Sure You Wish To Lock Your Leash? You Cannot Undo This Without Your {Config.MasterTag ?? ""} Having A Supported Avatar Or Closing Your Game & Deleting LeashMod's Config. This Also Lets Them Take Full Control Of You If They Have A Supported Avatar.", () =>
                            {
                                Config.IsLocked = true;
                                LockControls(true);
                                JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");

                                LeashLengthSlider?.SetActive(LeashLengthParam == null);

                                HooksOnOnAvatarInstantiated(Master._vrcplayer.field_Private_VRCAvatarManager_0, Master.prop_ApiAvatar_0, Master.transform.Find("ForwardDirection/Avatar").gameObject);
                            }, () => { LeashLock.SetToggleState(false); });
                        }
                        //else
                        //{
                        //    BuiltinUiUtils.ShowInputPopup($"{Config.MasterTag ?? ""} Code", "", InputField.InputType.Standard, false, "Confirm", (text, keys, comp) =>
                        //    {
                        //        var Code = Utils.CreateSHA256(Config.MasterID + " - " + RoomManager.field_Internal_Static_ApiWorldInstance_0.nonce + " - " + DateTime.UtcNow.Hour + " - " + DateTime.UtcNow.Day + "\\" + DateTime.UtcNow.Month).Substring(0, 8);

                        //        if (text == Code)
                        //        {
                        //            Config.IsLocked = false;
                        //            LockControls(false);
                        //            JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");
                        //        }

                        //        LeashLock.SetToggleState(Config.IsLocked);
                        //    }, () => { LeashLock.SetToggleState(Config.IsLocked); }, $"{Config.MasterTag ?? ""} Code => Ask Your {Config.MasterTag ?? ""} For This!");
                        //}
                    });
                    LeashLock.SetToggleState(Config.IsLocked);

                    ImmersionButton = OptionsGroup.AddToggleButton("Immersion", $"Enable Only Turning On The Leash When Your {Config.MasterTag ?? ""}'s Avatar Has Their Handle Visible", $"Disable Only Turning On The Leash When Your {Config.MasterTag ?? ""}'s Avatar Has Their Handle Visible", val =>
                    {
                        Config.OnlyEnableWhenLeashIsVisible = val;
                        JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");
                    });
                    ImmersionButton.SetToggleState(Config.OnlyEnableWhenLeashIsVisible);

                    if (MelonHandler.Mods.Any(o => o.Info.Name == "TouchTriggers"))
                    {
                        var GagButton = OptionsGroup.AddToggleButton("TouchTriggers Gag", "Enable Gag; Where If A DynamicBoneCollider Touches Your Lower Face, You Will Be Near-Muted.", "Disable Gag.", val =>
                        {
                            Config.GagEnabled = val;

                            JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");

                            if (!Config.GagEnabled)
                            {
                                GagState = false;
                                USpeaker.field_Internal_Static_Single_1 = 1f;
                                //GagIcon.SetActive(false);
                            }
                        });
                        GagButton.SetToggleState(Config.GagEnabled);
                    }

                    //var RestrainButton = OptionsGroup.AddToggleButton("Restrain Hands Test", "Test.", "Test.", val =>
                    //{
                    //    Config.RestrainHandsTest = val;

                    //    JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");
                    //});
                    //RestrainButton.SetToggleState(Config.RestrainHandsTest);

                    //new SimpleSingleButton(OptionsGroup, "Bind Left Hand To..", "", () =>
                    //{
                    //    var page = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

                    //    page.AddSimpleButton("Close", () =>
                    //    {
                    //        page.Hide();
                    //    });

                    //    foreach (var obj in Object.FindObjectsOfType<GameObject>().Where(o => o.name.Contains("YouTube")))
                    //    {
                    //        page.AddSimpleButton((obj.transform.parent != null ? obj.transform.parent.name + "/" : "") + obj.name, () =>
                    //        {
                    //            if (LeftHandIsBoundTo != null)
                    //            {
                    //                Object.Destroy(LeftHandIsBoundTo);
                    //            }

                    //            LeftHandIsBoundTo = obj.AddComponent<Restraint>();

                    //            LeftHandIsBoundTo.BindTo(RestraintL);
                    //            RestraintL.BindTo(LeftHandIsBoundTo);

                    //            page.Hide();
                    //        });
                    //    }

                    //    page.Show();
                    //});

                    //new SimpleSingleButton(OptionsGroup, "Bind Right Hand To..", "", () =>
                    //{
                    //    var page = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.WideSlimList);

                    //    page.AddSimpleButton("Close", () =>
                    //    {
                    //        page.Hide();
                    //    });

                    //    foreach (var obj in Object.FindObjectsOfType<GameObject>().Where(o => o.name.Contains("YouTube")))
                    //    {
                    //        page.AddSimpleButton((obj.transform.parent != null ? obj.transform.parent.name + "/" : "") + obj.name, () =>
                    //        {
                    //            if (RightHandIsBoundTo != null)
                    //            {
                    //                Object.Destroy(RightHandIsBoundTo);
                    //            }

                    //            RightHandIsBoundTo = obj.AddComponent<Restraint>();

                    //            RightHandIsBoundTo.BindTo(RestraintR);
                    //            RestraintR.BindTo(RightHandIsBoundTo);

                    //            page.Hide();
                    //        });
                    //    }

                    //    page.Show();
                    //});

                    #endif
                    //new SimpleSingleButton(OptionsGroup, "Show Master Code", "Shows Your Master Code To Let Your Pet Free!", () =>
                    //{
                    //    var Code = Utils.CreateSHA256(APIUser.CurrentUser.id + " - " + RoomManager.field_Internal_Static_ApiWorldInstance_0.nonce + " - " + DateTime.UtcNow.Hour + " - " + DateTime.UtcNow.Day + "\\" + DateTime.UtcNow.Month).Substring(0, 8);

                    //    Clipboard.SetText(Code, TextDataFormat.Text);

                    //    ButtonAPI.GetQuickMenuInstance().ShowOKDialog("Info", $"Your Master Code Is:\n{Code}\n\nIt Has Been Copied To Your Clipboard.");
                    //});
                    #if !Free
                    LeashLengthSlider = Page.AddSlider("Leash Length", $"Adjusts How Far You Can Go From Your {Config.MasterTag ?? ""}", val =>
                    {
                        if (Math.Abs(Config.LeashLength - val) > 0.001f)
                        {
                            Config.LeashLength = val;
                            LogWarning($"Leash Length Changed! - {Config.LeashLength}");
                            JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");
                        }
                    }, 0f, 15f, Config.LeashLength, false, false);

                    LeashSpeedSlider = Page.AddSlider("Leash Speed", $"Adjusts How Fast You Move Towards Your {Config.MasterTag ?? ""}", val =>
                    {
                        if (Math.Abs(Config.VelocityMultiplier - val) > 0.0001f)
                        {
                            Config.VelocityMultiplier = val;
                            JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");
                        }
                    }, 0.05f, 0.2f, Config.VelocityMultiplier, false, false);
                    #endif

                    #if !Free
                    LockControls(Config.IsLocked);
                    #endif
                };
            }

            MelonCoroutines.Start(RunMe());

            IEnumerator RunMe()
            {
                if (buildIndex == -1)
                {
                    while (RoomManager.field_Internal_Static_ApiWorldInstance_0 == null)
                    {
                        yield return new WaitForEndOfFrame();
                    }

                    yield return new WaitForSeconds(7f); // VRC Presence Buffer

                    LockControls(Config.IsLocked);
                }

                CurrentPresenceState = buildIndex == 0 ? PresenceState.Idle : buildIndex == -1 ? PresenceState.InWorld : PresenceState.Loading;
                PresenceClient?.SetPresence(CurrentPresence);

                yield break;
            }

            #if !Free
            if (buildIndex != -1) // Isn't A World
            {
                Log($"Scene Change: {buildIndex}");

                LeashLengthSlider?.SetActive(true);
            }
        }

        [Obfuscation(ApplyToMembers = true, Exclude = true, StripAfterObfuscation = true)]
        public class Configuration
        {
            public bool GagEnabled;

            public bool IsLeashed;
            public bool IsLocked;
            public float LeashLength = 0.9f;
            public string MasterDisplayName;
            public string MasterID;

            public string MasterTag = "Master";

            public bool OnlyEnableWhenLeashIsVisible = true;
            //public bool RestrainHandsTest;

            public float VelocityMultiplier = 0.10f;
        }

        public Configuration Config = new Configuration();

        private Player Master; // Temp Obj
        private Transform LeashFromObject;
        private Transform LeashToObject;

        private AvatarParameter LeashLockParam = null;
        private AvatarParameter LeashImmersionParam = null;
        private AvatarParameter LeashLengthParam = null;
        #endif

        #if !Free
        private static (bool, VRC_AvatarDescriptor.AnimationSet?) IsAvatarFemale(VRCAvatarManager manager)
        {
            var AllTransforms = manager?.prop_GameObject_0?.GetComponentsInChildren<Transform>(true);

            var AnimSet = (manager?.field_Private_VRC_AvatarDescriptor_0?.Animations ?? manager?.field_Private_VRC_AvatarDescriptor_0?.Animations) ?? manager?.field_Private_VRCAvatarDescriptor_0?.Animations;

            if (AnimSet == null)
            {
                Log("AnimSet Is Null!");
            }

            return (AnimSet == VRC_AvatarDescriptor.AnimationSet.Female || (AllTransforms != null && AllTransforms.Any(o => o != null && (o.name.ToLower().Contains("breast") || o.name.ToLower().Contains("boob")))), AnimSet);
        }

        //private Restraint RestraintL;
        //private Restraint RestraintR;

        private GameObject MasterObj;

        private bool GagState;

        private float OnUpdateRoutine = 0f;
        private float OnUpdateRoutine2 = 0f;

        public override void OnUpdate()
        {
            if (Time.time > OnUpdateRoutine2)
            {
                OnUpdateRoutine2 = Time.time + 0.1f;

                if (Master?._vrcplayer != null && Config.IsLocked && LeashLockParam != null && (LeashLockParam.prop_Boolean_1 || UnlockPending))
                {
                    UnlockPending = false;

                    Log($"{Config.MasterTag ?? ""} Set You Free!");

                    Config.IsLocked = false;
                    LockControls(false);
                    JsonConfig.SaveConfig(Config, Environment.CurrentDirectory + "\\LeashMod.json");

                    LeashLock?.SetToggleState(false);

                    LeashLengthSlider?.SetActive(true);
                }

                // If the gag is enabled, the gagval was found AND the state is different to the previous known state
                if (Config.GagEnabled && GagState != IsGagged)
                {
                    // cache the new state
                    GagState = IsGagged;

                    // vary the logged text based on the state
                    Log(GagState ? "You Were Gagged!" : "You Were UnGagged!");

                    // vary the mic gain based on the state
                    USpeaker.field_Internal_Static_Single_1 = GagState ? 0.01f : 1f;

                    // vary the gag icon visibility based on the state
                    //GagIcon.SetActive(GagState);
                }
            }

            if (Time.time > OnUpdateRoutine && Master?._vrcplayer != null && Config.IsLocked)
            {
                OnUpdateRoutine = Time.time + 1f;

                if (LeashImmersionParam != null && LeashImmersionParam.prop_Boolean_1 != Config.OnlyEnableWhenLeashIsVisible)
                {
                    Log($"{Config.MasterTag ?? ""} Set Your Immersion {(LeashImmersionParam.prop_Boolean_1 ? "On" : "Off")}");

                    ImmersionButton?.SetToggleState(LeashImmersionParam.prop_Boolean_1, true);
                }

                if (LeashLengthParam != null)
                {
                    var Val = Utils.RangeConvF(LeashLengthParam.prop_Single_1, 0f, 1f, 0f, 15f);

                    if (Math.Abs(Val - Config.LeashLength) > 0.001f)
                    {
                        Log($"{Config.MasterTag ?? ""} Set Your Leash Length To: {LeashLengthParam.prop_Single_1} => {Val}");

                        Config.LeashLength = Val;

                        try
                        {
                            LeashLengthSlider?.sliderSlider?.Set(Val);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private void LockControls(bool state)
        {
            try
            {
                Leash?.gameObject.SetActive(!state);
                LeashLock?.gameObject.SetActive(!state);
                ImmersionButton?.gameObject.SetActive(!state);
                LeashLengthSlider?.gameObject.SetActive(!state);

                if (state)
                {
                    LeashSpeedSlider.SetValue(0.10f, true);
                }

                LeashSpeedSlider.sliderSlider.minValue = state ? 0.10f : 0.05f;
            }
            catch (Exception ex)
            {
                LogError("Error in LockControls: " + ex);
            }
        }
        #endif

        #if !Free
        private ToggleButton Leash = null;
        private ToggleButton LeashLock = null;
        private ToggleButton ImmersionButton = null;
        private Slider LeashLengthSlider = null;
        private Slider LeashSpeedSlider = null;
        #endif

        #if !Free
        internal IEnumerator Loop()
        {
            while (true)
            {
                //If There's No Master Player Object & They've Spawned, Find It!
                if (MasterObj != null && (Master == null || LeashToObject == null || LeashFromObject == null))
                {
                    yield return new WaitForSeconds(1f);

                    try
                    {
                        Master = MasterObj?.transform?.root?.GetComponent<Player>();

                        if (Master != null)
                        {
                            LeashToObject =
                                Master.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(o => o != null && o.name == "Plague_LeashToMe")?.transform
                                ??
                                (MasterObj?.GetComponent<Animator>()?.GetBoneTransform(HumanBodyBones.RightHand) ?? Master.transform);

                            LeashFromObject =
                                VRCPlayer.field_Internal_Static_VRCPlayer_0.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(o => o != null && o.name == "Plague_LeashFromMe")?.transform
                                ??
                                (VRCPlayer.field_Internal_Static_VRCPlayer_0.transform.Find("ForwardDirection/Avatar")?.GetComponent<Animator>()?.GetBoneTransform(HumanBodyBones.Neck) ?? VRCPlayer.field_Internal_Static_VRCPlayer_0.transform);
                        }
                    }
                    catch
                    {
                    }
                }
                else
                {
                    yield return new WaitForEndOfFrame();

                    try
                    {
                        if (Config.IsLeashed
                            &&
                            RoomManager.field_Internal_Static_ApiWorld_0 != null &&
                            Master?._vrcplayer?.transform != null &&
                            VRCPlayer.field_Internal_Static_VRCPlayer_0 != null &&
                            VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Private_VRCPlayerApi_0 != null
                            &&
                            (!Config.OnlyEnableWhenLeashIsVisible || (LeashToObject.name == "Plague_LeashToMe" && LeashToObject.gameObject.active)))
                        {
                            var DistanceToMaster =
                                Vector3.Distance(
                                    LeashFromObject.position,
                                    LeashToObject.position);

                            //VRCPlayer.field_Internal_Static_VRCPlayer_0.transform.rotation = Quaternion.RotateTowards(VRCPlayer.field_Internal_Static_VRCPlayer_0.transform.rotation, LeashToObject.rotation, (DistanceToMaster - Config.LeashLength) * Config.VelocityMultiplier);

                            //Distance Math
                            //if (DistanceToMaster >= (Config.LeashLength * 3f))
                            //{
                            //    VRCPlayer.field_Internal_Static_VRCPlayer_0.transform.position = LeashToObject.position;
                            //}
                            //else
                            if (DistanceToMaster >= Config.LeashLength)
                            {
                                VRCPlayer.field_Internal_Static_VRCPlayer_0.transform.position = Utils.MoveTowardsInPerspective(LeashFromObject.position, VRCPlayer.field_Internal_Static_VRCPlayer_0.transform.position, LeashToObject.position, (DistanceToMaster - Config.LeashLength) * Config.VelocityMultiplier);
                            }

                            //Is In VR
                            if (XRDevice.isPresent)
                            {
                                //Vibration Math
                                if ((Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f) &&
                                    DistanceToMaster > Config.LeashLength)
                                {
                                    VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Private_VRCPlayerApi_0
                                        .PlayHapticEventInHand(VRC_Pickup.PickupHand.Left, 0.5f,
                                            0.025f * (DistanceToMaster - Config.LeashLength), 0.5f);

                                    VRCPlayer.field_Internal_Static_VRCPlayer_0.field_Private_VRCPlayerApi_0
                                        .PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.5f,
                                            0.025f * (DistanceToMaster - Config.LeashLength), 0.5f);
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        #region Logging

        public static void Log(string text, ConsoleColor color = ConsoleColor.Magenta)
        {
            MelonLogger.Msg(color, "[LeashMod] " + text);
        }

        public static void LogWarning(string text)
        {
            MelonLogger.Warning("[LeashMod] " + text);
        }

        public static void LogError(string text)
        {
            MelonLogger.Error("[LeashMod] " + text);
        }

        #endregion

        #endif
    }

    /// <summary>
    ///   A Component For Hooking To Generic Events Such As A Object Becoming Enabled, Disabled, Destroyed And For Events Such
    ///   As Update.
    /// </summary>
    public class ObjectListener : MonoBehaviour
    {
        private bool IsEnabled;
        public Action<GameObject> OnDestroyed = null;
        public Action<GameObject> OnDisabled = null;

        public Action<GameObject> OnEnabled = null;
        public Action<GameObject, bool> OnUpdate = null;
        public Action<GameObject, bool> OnUpdateEachSecond = null;
        private float UpdateDelay = 0f;

        public ObjectListener(IntPtr instance) : base(instance)
        {
        }

        private void OnEnable()
        {
            OnEnabled?.Invoke(gameObject);
            IsEnabled = true;
        }

        private void OnDisable()
        {
            OnDisabled?.Invoke(gameObject);
            IsEnabled = false;
        }

        private void OnDestroy()
        {
            OnDestroyed?.Invoke(gameObject);
        }

        private void Update()
        {
            OnUpdate?.Invoke(gameObject, IsEnabled);

            if (UpdateDelay < Time.time)
            {
                UpdateDelay = Time.time + 1f;

                OnUpdateEachSecond?.Invoke(gameObject, IsEnabled);
            }
        }
    }
}