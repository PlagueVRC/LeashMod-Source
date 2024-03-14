#if !Free
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.Playables;

namespace LeashMod.External_Libraries
{
    internal static class ParametersLib
    {
        public static readonly string[] DefaultParameterNames =
        {
            "Viseme",
            "GestureLeft",
            "GestureLeftWeight",
            "GestureRight",
            "GestureRightWeight",
            "TrackingType",
            "VRMode",
            "MuteSelf",
            "Grounded",
            "AngularY",
            "Upright",
            "AFK",
            "Seated",
            "InStation",
            "VelocityX",
            "VelocityY",
            "VelocityZ",
            "IsLocal",
            "AvatarVersion",
            "VRCEmote",
            "VRCFaceBlendH",
            "VRCFaceBlendV"
        };

        public static List<AvatarParameter> FilterDefaultParameters(IEnumerable<AvatarParameter> src)
        {
            return src.Where(param => !DefaultParameterNames.Contains(param.field_Private_String_0)).ToList();
        }

        public static IEnumerable<AvatarParameter> GetAllAvatarParameters(this VRCAvatarManager manager)
        {
            var parameters = manager.field_Private_AvatarPlayableController_0?
                .field_Private_Dictionary_2_Int32_AvatarParameter_0;

            if (parameters == null)
            {
                yield break;
            }

            foreach (var param in parameters)
            {
                yield return param.value;
            }
        }

        public static List<AvatarParameter> GetAvatarParameters(this VRCAvatarManager manager)
        {
            return FilterDefaultParameters(manager.GetAllAvatarParameters());
        }

        public static bool HasCustomExpressions(this VRCAvatarManager manager)
        {
            return manager &&
                   manager.field_Private_AvatarPlayableController_0 != null &&
                   manager.prop_VRCAvatarDescriptor_0 != null &&
                   manager.prop_VRCAvatarDescriptor_0.customExpressions &&
                   /* Fuck you */
                   manager.prop_VRCAvatarDescriptor_0.expressionParameters != null &&
                   manager.prop_VRCAvatarDescriptor_0.expressionsMenu != null &&
                   /* This isn't funny */
                   manager.prop_VRCAvatarDescriptor_0.expressionsMenu.controls != null &&
                   manager.prop_VRCAvatarDescriptor_0.expressionsMenu.controls.Count > 0;
        }

        public static IEnumerable<Renderer> GetAvatarRenderers(this VRCAvatarManager manager)
        {
            return manager.field_Private_ArrayOf_Renderer_0;
        }

        public static float GetValue(this AvatarParameter parameter)
        {
            if (parameter == null)
            {
                return 0f;
            }

            switch (parameter.field_Public_ParameterType_0)
            {
                case AvatarParameter.ParameterType.Bool:
                    return parameter.prop_Boolean_1 ? 1f : 0f;
                case AvatarParameter.ParameterType.Int:
                    return parameter.prop_Int32_1;
                case AvatarParameter.ParameterType.Float:
                    return parameter.prop_Single_1;
                default:
                    return 0f;
            }
        }
    }
}
#endif