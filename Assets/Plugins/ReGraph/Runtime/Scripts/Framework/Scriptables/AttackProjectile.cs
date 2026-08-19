using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace Reshape.ReFramework
{
    [CreateAssetMenu(menuName = "Reshape/Attack Projectile", fileName = "AttackProjectile", order = 402)]
    [HideMonoScript]
    public class AttackProjectile : BaseScriptable
    {
        private enum Ballistic
        {
            StraightToTarget = 10,
            StraightFollowTarget = 20,
            CurveToTarget = 30
        }
        
        public enum HitOnAirFeedback
        {
            StaticOnAir = 10,
            KeepVelocityOnAir = 20,
            FallFromAir = 30
        }

        [Hint("showHints", "Define which ammo model prefab will be use.")]
        [SerializeField]
        private GameObject model;

        [Hint("showHints", "Define which flag / category / type the projectile belong to.")]
        [Layer]
        public int flag;
        
        [Hint("showHints", "Define the damage pack that use to calculate attack damage.")]
        [SerializeField, LabelText("Damage Pack")]
        private AttackDamagePack attackDamagePack;

        [Hint("showHints", "Define the path of the projectile travel.")]
        [SerializeField]
        private Ballistic travelMode = Ballistic.StraightToTarget;

        [Hint("showHints", "Define the range of projectile will stick on target when reach.")]
        [SerializeField]
        [BoxGroup("Hit"), LabelText("Stick On Target")]
        private bool hitTargetStick;

        [Hint("showHints", "Define the reaction when hit another projectile on air.")]
        [SerializeField]
        [BoxGroup("Hit"), LabelText("Hit Projectile React")]
        private HitOnAirFeedback hitOnAirReaction = HitOnAirFeedback.StaticOnAir;

        [Hint("showHints", "Define the range of projectile reach.\nProjectile will reach at a random spot within the range.")]
        [SerializeField]
        [HideIf("IsFollowTargetTravelMode")]
        [BoxGroup("Hit"), LabelText("Area Range")]
        private Vector3 hitAreaRange;
        
        [Hint("showHints", "Define the range size of hit area base on distance data")]
        [SerializeField]
        [HideIf("IsFollowTargetTravelMode")]
        [BoxGroup("Hit"), LabelText("Range Modifier")]
        private AnimationCurve hitAreaRangeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Hint("showHints", "Define the projectile use collider as hit box.")]
        [SerializeField]
        [BoxGroup("Hit"), LabelText("Collider Hit Box")]
        private bool hitColliderDetect;

        [Hint("showHints", "Define the layers for projectile hit box will detect.")]
        [SerializeField]
        [BoxGroup("Hit"), LabelText("Hit Layer"), Indent]
        [ShowIf("@hitColliderDetect == true")]
        private LayerMask hitColliderDetectLayer = Physics.AllLayers;

        [Hint("showHints", "Define the distance of projectile continue toward after arrive its destination.")]
        [SerializeField]
        [BoxGroup("Hit"), LabelText("Hit Distance"), Indent]
        [ShowIf("@hitColliderDetect == true")]
        private float hitColliderDetectDuration = 10;

        [Hint("showHints", "Define the projectile forecast target forward position.")]
        [SerializeField]
        [HideIf("IsFollowTargetTravelMode")]
        [BoxGroup("Hit"), LabelText("Forecast Forward")]
        private bool hitForecastForward;

        [Hint("showHints", "Define how accuracy the unit forecast target forward position.\n1 is 100% accuracy.")]
        [SerializeField]
        [HideIf("@IsFollowTargetTravelMode() || !hitForecastForward")]
        [BoxGroup("Hit"), LabelText("Accuracy"), Indent]
        private float hitForecastForwardAccuracy = 0.8f;

        [Hint("showHints", "Define the speed modifier of projectile travel.\nThis value will be sum with the value from stat.")]
        [SerializeField]
        [BoxGroup("Speed"), LabelText("Modifier")]
        private float travelSpeedMod = 1;

        [Hint("showHints", "Define the speed multiplier of projectile travel.\nThis value will be multiply with the value from stat.")]
        [SerializeField]
        [BoxGroup("Speed"), LabelText("Multiplier")]
        private float travelSpeedMtp = 1;

        [Hint("showHints", "Define the height of projectile travel curve.")]
        [SerializeField]
        [BoxGroup("Curve"), ShowIf("IsCurveTravelMode")]
        [LabelText("Height")]
        private float travelCurveHeight = 0.5f;
        
        [Hint("showHints", "Define the travel height of projectile base on distance data")]
        [SerializeField]
        [BoxGroup("Curve"), ShowIf("IsCurveTravelMode")]
        [LabelText("Height Modifier")]
        private AnimationCurve travelCurveHeightMod = AnimationCurve.Linear(0.5f, 0.5f, 1, 1);

        [Hint("showHints", "Define the width of projectile travel curve bezier points.")]
        [SerializeField]
        [BoxGroup("Curve"), ShowIf("IsCurveTravelMode")]
        [LabelText("Control Point")]
        private float travelCurveControlPoint = 0.5f;

        [Hint("showHints", "Define the projectile change its rotation while traveling in curve.")]
        [SerializeField]
        [BoxGroup("Curve"), ShowIf("IsCurveTravelMode")]
        [LabelText("Rotation Follow")]
        private bool travelCurveRotation;

        [Hint("showHints", "Define the projectile travel base on time rather than speed.\nThis value is the minimum time its travel.")]
        [SerializeField]
        [BoxGroup("Curve"), ShowIf("IsCurveTravelMode")]
        [LabelText("Base Time")]
        private float travelCurveBaseTime;

        [Hint("showHints", "Define the projectile curve travel speed.\nThis value will be multiplier of the travel speed.")]
        [SerializeField]
        [BoxGroup("Curve"), ShowIf("@IsCurveTravelMode() && travelCurveBaseTime > 0")]
        [LabelText("Speed Range")]
        private float travelCurveSpeedRange;

        public AttackDamagePack damagePack => attackDamagePack;
        public GameObject ammo => model;
        public float speedMod => travelSpeedMod;
        public float speedMtp => travelSpeedMtp;
        public Vector3 hitRange => hitAreaRange;
        public AnimationCurve hitRangeCurve => hitAreaRangeCurve;
        public bool stickTarget => hitTargetStick;
        public HitOnAirFeedback hitOnAirReact => hitOnAirReaction;
        public bool hitForecast => hitForecastForward;
        public float hitForecastAccuracy => hitForecastForwardAccuracy;
        public bool hitCollider => hitColliderDetect;
        public LayerMask hitColliderLayer => hitColliderDetectLayer;
        public float hitTravelDuration => hitColliderDetectDuration;
        public float curveHeight => travelCurveHeight;
        public AnimationCurve curveHeightCurve => travelCurveHeightMod;
        public float curveControlPoint => travelCurveControlPoint;
        public bool curveRotation => travelCurveRotation;
        public float curveBaseTime => travelCurveBaseTime;
        public float curveSpeedRange => travelCurveSpeedRange;

        public bool isStraightToTargetBallistic => travelMode == Ballistic.StraightToTarget;
        public bool isStraightFollowTargetBallistic => travelMode == Ballistic.StraightFollowTarget;
        public bool isCurveToTargetBallistic => travelMode == Ballistic.CurveToTarget;

#if UNITY_EDITOR
        private bool IsFollowTargetTravelMode ()
        {
            if (travelMode == Ballistic.StraightFollowTarget)
                return true;
            return false;
        }
        
        private bool IsCurveTravelMode ()
        {
            if (travelMode == Ballistic.CurveToTarget)
                return true;
            return false;
        }
#endif
    }
}