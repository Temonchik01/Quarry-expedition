using UnityEngine;

namespace BtlSys.UI
{
    public class SelectionCursorUI : MonoBehaviour
    {
        private static SelectionCursorUI _instance;
        public static SelectionCursorUI Instance
        {
            get
            {
                if (_instance == null) _instance = Object.FindFirstObjectByType<SelectionCursorUI>(FindObjectsInactive.Include);
                return _instance;
            }
        }

        [SerializeField] private Vector3 _combatantOffset = new Vector3(-1f, 1.5f, 0f);
        [SerializeField] private Vector3 _uiOffset = new Vector3(2.2f, 0f, 0f);
        [SerializeField] private Sprite menuSprite;
        [SerializeField] private Sprite battleCursorSprite;
        [SerializeField] private Vector3 battleCursorScale = new Vector3(3f, 3f, 3f);
        private Vector3 menuCursorScale;
        private SpriteRenderer _spriteRenderer;
        private Camera _mainCamera;

        private void Awake()
        {
            _instance = this;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            menuCursorScale = transform.localScale;
            if (menuSprite == null && _spriteRenderer != null) menuSprite = _spriteRenderer.sprite;
            _mainCamera = Camera.main;
            gameObject.SetActive(false);
        }

        public void SetTarget(Transform target, bool isUI = false, bool isEnemy = false)
        {
            if (target == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (_spriteRenderer != null) _spriteRenderer.sprite = isEnemy ? battleCursorSprite : menuSprite;
            transform.localScale = isEnemy ? battleCursorScale : menuCursorScale;

            // Apply different offsets based on whether we are pointing at UI or a Combatant
            Vector3 offset = isUI ? _uiOffset : _combatantOffset;
            transform.position = target.position + offset;
        }

        private void LateUpdate()
        {
            // Billboard effect: Face the camera
            if (_mainCamera != null)
            {
                transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                                 _mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}

