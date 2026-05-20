using UnityEngine;

namespace BtlSys.UI
{
    public class SelectionCursorUI : MonoBehaviour
    {
        public static SelectionCursorUI Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            gameObject.SetActive(false);
        }

        public void SetTarget(Transform target)
        {
            if (target == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            transform.position = target.position + Vector3.up * 2f; // Assuming 3D world or 2D sprite
        }
    }
}
