using UnityEngine;

namespace ArcSquad
{
    public sealed class ArcCameraRig : MonoBehaviour
    {
        public Transform Target;
        public float RotationSpeed=2.2f;
        float yaw=0f,pitch=15f;
        Vector3 velocity;
        public Vector3 FlatForward => Quaternion.Euler(0,yaw,0)*Vector3.forward;
        public Vector3 FlatRight => Quaternion.Euler(0,yaw,0)*Vector3.right;
        void LateUpdate()
        {
            if (!Target) return;
            if (Cursor.lockState==CursorLockMode.Locked)
            {
                yaw += Input.GetAxis("Mouse X")*RotationSpeed;
                pitch = Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*RotationSpeed,-12,55);
            }
            var direction=Quaternion.Euler(pitch,yaw,0)*Vector3.back;
            var desired=Target.position + Vector3.up*3 + direction*13f;
            transform.position=Vector3.SmoothDamp(transform.position,desired,ref velocity,.13f);
            transform.LookAt(Target.position+Vector3.up*1.5f);
        }
    }
}
