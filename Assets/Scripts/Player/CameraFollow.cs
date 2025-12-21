using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform followTarget;
    [SerializeField] Transform orientation;

    private void LateUpdate()
    {
        transform.position = followTarget.position;
        transform.rotation = orientation.rotation;
    }
}
