using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletsDestroy: MonoBehaviour
{
    void Update()
    {
        if (Input.GetKey(KeyCode.E))
        {
            Destroy(this.gameObject, 3f);
        }
    }
}
