using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace YachuDice.Utilities
{
    public class SfxButton : Button
    {
        [SerializeField] private SfxRole _role = SfxRole.Default;

        public override void OnPointerClick(PointerEventData eventData)
        {
            // base가 onClick을 호출하며 이 버튼을 비활성화/파괴할 수 있으므로(예: 뒤로가기), base 전에 재생한다.
            if (eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable())
                UiSfxManager.PlayClick(_role);

            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            // base가 onClick을 호출하며 이 버튼을 비활성화/파괴할 수 있으므로(예: 뒤로가기), base 전에 재생한다.
            if (IsActive() && IsInteractable())
                UiSfxManager.PlayClick(_role);

            base.OnSubmit(eventData);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (IsActive() && IsInteractable())
                UiSfxManager.PlayMove();

            base.OnPointerEnter(eventData);
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (IsActive() && IsInteractable())
                UiSfxManager.PlayMove();

            base.OnMove(eventData);
        }
    }
}