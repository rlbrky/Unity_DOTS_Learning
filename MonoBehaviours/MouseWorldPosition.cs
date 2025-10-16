using System;
using UnityEngine;

public class MouseWorldPosition : MonoBehaviour
{
    public static MouseWorldPosition Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        
    }

    public Vector3 GetPosition()
    {
        Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        /* Does essentially the same thing as our code but using phyiscs, using this would be cleaner in non uniform terrains that have different heights etc.
         if (Physics.Raycast(mouseCameraRay, out RaycastHit hit))
        {
            return hit.point;
        } We use non physics one because our plane is flat.*/
        if (plane.Raycast(mouseCameraRay, out float distance))
        {
            return mouseCameraRay.GetPoint(distance);
        }
        else
        {
            return Vector3.zero;
        }
    }
}
