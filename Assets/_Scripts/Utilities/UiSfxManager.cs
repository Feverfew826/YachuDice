using System;

using UnityEngine;

namespace YachuDice.Utilities
{
    public class UiSfxManager : MonoBehaviour
    {
        [Serializable]
        private struct RoleClip
        {
            public SfxRole role;
            public AudioClip clip;
        }

        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private RoleClip[] _clickClips;
        [SerializeField] private AudioClip _moveClip;

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

            var clip = FindClip(role);
            if (clip != null)
                _audioSource.PlayOneShot(clip);
        }

        private void PlayMoveInternal()
        {
            if (Time.unscaledTime - _lastClickTime < _moveSuppressAfterClickSeconds)
                return;

            if (_moveClip != null)
                _audioSource.PlayOneShot(_moveClip);
        }

        private AudioClip FindClip(SfxRole role)
        {
            foreach (var entry in _clickClips)
            {
                if (entry.role == role)
                    return entry.clip;
            }

            return null;
        }
    }
}