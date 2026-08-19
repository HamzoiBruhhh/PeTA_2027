using DG.Tweening;
using DG.Tweening.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    public abstract class EnvLight { }

    public class EnvLightTween : TweenData
    {
        public enum LightCommand
        {
            LightIntensity,
        }

        public LightCommand command;

        public float to;

        public override Tween GetTween (EnvLight light)
        {
            switch (command)
            {
                case LightCommand.LightIntensity:
                    return DOTween.To(() => RenderSettings.ambientIntensity, x => RenderSettings.ambientIntensity = x, to, duration);
                    ;
                default:
                    return null;
            }
        }

#if UNITY_EDITOR
        private bool HideColor ()
        {
            return !command.ToString().Contains("Color");
        }
#endif
    }
}