using UnityEngine;

namespace BtlSys.UI
{
    public class BillboardUI : MonoBehaviour
    {
        private Camera _mainCamera;

        void Start()
        {
            _mainCamera = Camera.main;
        }

        void LateUpdate()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            // Makes the UI always face the camera perfectly
            // We want the negative Z (the front of the canvas) to face the camera
            // So we make the positive Z face away from the camera
            transform.rotation = Quaternion.LookRotation(transform.position - _mainCamera.transform.position, _mainCamera.transform.up);
        }
    }
}
