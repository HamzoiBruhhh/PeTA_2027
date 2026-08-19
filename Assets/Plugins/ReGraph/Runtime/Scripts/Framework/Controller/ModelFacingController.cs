using Sirenix.OdinInspector;
using UnityEngine;

namespace Reshape.ReFramework
{
    [HideMonoScript]
    public class ModelFacingController : BaseBehaviour
    {
        public NumberList numberList;

        //-----------------------------------------------------------------
        //-- static methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- public methods
        //-----------------------------------------------------------------

        public void SetupAngles (NumberList numbers)
        {
            numberList = numbers;
        }

        //-----------------------------------------------------------------
        //-- protected methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- mono methods
        //-----------------------------------------------------------------

        protected void LateUpdate ()
        {
            var numberListCount = numberList.GetCount();
            if (transform && numberListCount > 0)
            {
                transform.localEulerAngles = Vector3.zero;
                var rot = transform.eulerAngles;
                var minIndex = 0;
                var smallest = float.MaxValue;
                for (var i = 0; i < numberListCount; i++)
                {
                    var different = Mathf.Abs(rot.y - numberList.GetByIndex(i));
                    if (different < smallest)
                    {
                        smallest = different;
                        minIndex = i;
                    }
                }

                rot.y = numberList.GetByIndex(minIndex);
                transform.eulerAngles = rot;
            }
        }

        //-----------------------------------------------------------------
        //-- BaseBehaviour methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- private methods
        //-----------------------------------------------------------------

        //-----------------------------------------------------------------
        //-- editor methods
        //-----------------------------------------------------------------
    }
}