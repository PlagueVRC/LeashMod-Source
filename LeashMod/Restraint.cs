using System;
using RootMotion.FinalIK;
using UnityEngine;

namespace LeashMod
{
    internal class Restraint : MonoBehaviour
    {
        public enum BodyTrackingMode
        {
            Generic = 0,
            FBT
        }

        public enum RestraintType
        {
            LeftHand,
            RightHand,
            LeftFoot,
            RightFoot,
            None
        }

        private Restraint BoundTo;

        public Matrix4x4 OffsetMatrix = Matrix4x4.identity;

        public VRCPlayer player;

        public RestraintType restraintType;
        public Vector3 TargetPos = Vector3.zero;

        public BodyTrackingMode TrackingMode = BodyTrackingMode.Generic;

        private Il2CppSystem.Collections.Generic.List<UnityEngine.Object> AntiGC = new Il2CppSystem.Collections.Generic.List<UnityEngine.Object>();

        public Restraint(IntPtr ptr) : base(ptr)
        {
            AntiGC.Add(this);
        }

        public IKSolverVR VR_Solver => player?.field_Private_VRC_AnimationController_0?.field_Private_VRIK_0?.solver;
        public FullBodyBipedIK FBT_IK => player?.field_Private_VRC_AnimationController_0?.field_Private_FullBodyBipedIK_0;

        public void BindTo(Restraint restraint)
        {
            TargetPos = Vector3.zero;

            BoundTo = restraint;

            OffsetMatrix = BoundTo.transform.GetMatrix().inverse * transform.GetMatrix();
        }

        //Init
        private void Start()
        {
            player = transform.root.GetComponent<VRCPlayer>();
        }

        private void Update()
        {
            //if (restraintType != RestraintType.None)
            //{
            //    // Recheck tracking mode
            //    TrackingMode = BodyTrackingMode.Generic;
            //    if (FBT_IK != null && FBT_IK.enabled)
            //    {
            //        TrackingMode = BodyTrackingMode.FBT;
            //    }

            //    var l_bonePoint = BoundTo?.transform;

            //    if (l_bonePoint != null)
            //    {
            //        switch (restraintType)
            //        {
            //            case RestraintType.LeftHand:
            //            case RestraintType.RightHand:
            //            case RestraintType.LeftFoot:
            //            case RestraintType.RightFoot:
            //                var l_resultMatrix = l_bonePoint.transform.GetMatrix() * OffsetMatrix;
            //                TargetPos = l_resultMatrix * UtilsExt.ms_pointVector;
            //                break;
            //        }
            //    }
            //}
            //else
            if (BoundTo != null)
            {
                TargetPos = BoundTo.transform.position;
            }
        }

        private void LateUpdate()
        {
            if (BoundTo != null && TargetPos != Vector3.zero)
            {
                switch (restraintType)
                {
                    case RestraintType.LeftHand:
                        switch (TrackingMode)
                        {
                            case BodyTrackingMode.Generic:
                                var l_ikArm = VR_Solver?.leftArm;

                                if (l_ikArm?.target != null)
                                {
                                    l_ikArm.positionWeight = 1f;
                                    l_ikArm.target.transform.position = TargetPos;
                                }

                                break;
                            case BodyTrackingMode.FBT:
                                if (FBT_IK.solver?.leftHandEffector?.target != null)
                                {
                                    FBT_IK.solver.leftHandEffector.position = TargetPos;
                                    FBT_IK.solver.leftHandEffector.target.position = TargetPos;
                                }

                                break;
                        }

                        break;
                    case RestraintType.RightHand:
                        switch (TrackingMode)
                        {
                            case BodyTrackingMode.Generic:
                                var l_ikArm = VR_Solver?.rightArm;

                                if (l_ikArm?.target != null)
                                {
                                    l_ikArm.positionWeight = 1f;
                                    l_ikArm.target.transform.position = TargetPos;
                                }

                                break;
                            case BodyTrackingMode.FBT:
                                if (FBT_IK.solver?.rightHandEffector?.target != null)
                                {
                                    FBT_IK.solver.rightHandEffector.position = TargetPos;
                                    FBT_IK.solver.rightHandEffector.target.position = TargetPos;
                                }

                                break;
                        }

                        break;
                    case RestraintType.LeftFoot:
                        switch (TrackingMode)
                        {
                            case BodyTrackingMode.Generic:
                                var l_ikLeg = VR_Solver?.leftLeg;

                                if (l_ikLeg?.target != null)
                                {
                                    l_ikLeg.positionWeight = 1f;
                                    l_ikLeg.target.transform.position = TargetPos;
                                }

                                break;
                            case BodyTrackingMode.FBT:
                                if (FBT_IK.solver?.leftFootEffector?.target != null)
                                {
                                    FBT_IK.solver.leftFootEffector.position = TargetPos;
                                    FBT_IK.solver.leftFootEffector.target.position = TargetPos;
                                }

                                break;
                        }

                        break;
                    case RestraintType.RightFoot:
                        switch (TrackingMode)
                        {
                            case BodyTrackingMode.Generic:
                                var l_ikLeg = VR_Solver?.rightLeg;

                                if (l_ikLeg?.target != null)
                                {
                                    l_ikLeg.positionWeight = 1f;
                                    l_ikLeg.target.transform.position = TargetPos;
                                }

                                break;
                            case BodyTrackingMode.FBT:
                                if (FBT_IK.solver?.rightFootEffector?.target != null)
                                {
                                    FBT_IK.solver.rightFootEffector.position = TargetPos;
                                    FBT_IK.solver.rightFootEffector.target.position = TargetPos;
                                }

                                break;
                        }

                        break;
                    case RestraintType.None:
                        transform.position = TargetPos;
                        break;
                }
            }
        }
    }
}