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
            transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                             _mainCamera.transform.rotation * Vector3.up);
        }
    }
}
