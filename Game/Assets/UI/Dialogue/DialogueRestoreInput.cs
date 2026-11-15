// 역할: 숨긴 대화창을 화면 어디서나 복원하는 투명 UI 입력 영역이다. 버튼이나 문구는 표시하지 않는다.
using UnityEngine;
using UnityEngine.EventSystems;

namespace DdukDduk.Dialogue
{
    public sealed class DialogueRestoreInput : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private DialogueUIController dialogueUI;

        // 마우스 왼쪽 클릭 또는 터치가 끝났을 때 대화창만 복원한다. 뒤쪽 UI로 같은 클릭을 전달하지 않는다.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                dialogueUI.RestoreFromPointer();
            }
        }
    }
}
