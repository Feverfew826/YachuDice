using System;

using UnityEngine;

namespace YachuDice.Utilities
{
    public class UiSfxManager : MonoBehaviour
    {
        [Serializable]
        private struct RoleSource
        {
            public SfxRole role;
            public AudioSource source;
        }

        [SerializeField] private RoleSource[] _roleSources;

        // 클릭 직후 이 시간(초) 동안은 move음을 내지 않는다. 창이 닫히며 버튼이 커서 아래로 드러나
        // 부수적으로 발생하는 hover(이동)음이 방금 난 클릭음(예: 뒤로가기)을 가리는 것을 막기 위함.
        [SerializeField] private float _moveSuppressAfterClickSeconds = 0.1f;
        private float _lastClickTime = float.NegativeInfinity;

        private static UiSfxManager _instance;
        private static UiSfxManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var prefab = Resources.Load<UiSfxManager>("SFX/UiSfxManager");
                    if (prefab == null)
                    {
                        Debug.LogError("Resources/SFX/UiSfxManager 프리팹을 찾을 수 없습니다.");
                        return null;
                    }

                    _instance = Instantiate(prefab);
                    DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;
            }
        }

        public static void PlayClick(SfxRole role)
        {
            if (Instance != null)
                Instance.PlayClickInternal(role);
        }

        public static void PlayMove()
        {
            if (Instance != null)
                Instance.PlayMoveInternal();
        }

        private void PlayClickInternal(SfxRole role)
        {
            _lastClickTime = Time.unscaledTime;

            Play(role);
        }

        private void PlayMoveInternal()
        {
            if (Time.unscaledTime - _lastClickTime < _moveSuppressAfterClickSeconds)
                return;

            Play(SfxRole.Move);
        }

        private void Play(SfxRole role)
        {
            foreach (var entry in _roleSources)
            {
                if (entry.role == role)
                {
                    // 클립·pitch·volume 등은 각 AudioSource에 설정된 값을 그대로 사용한다.
                    if (entry.source != null && entry.source.clip != null)
                        entry.source.PlayOneShot(entry.source.clip);

                    return;
                }
            }
        }
    }
}
