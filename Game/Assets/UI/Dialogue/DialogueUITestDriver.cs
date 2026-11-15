// 역할: 독립 테스트 씬에서 임시 대사를 공급한다. 실제 스토리 진행이나 공통 프리팹에는 포함하지 않는다.
using TMPro;
using UnityEngine;

namespace DdukDduk.Dialogue
{
    public sealed class DialogueUITestDriver : MonoBehaviour
    {
        [SerializeField] private DialogueUIController dialogueUI;
        [SerializeField] private CanvasGroup guideGroup;
        [SerializeField] private TMP_Text testStatus;
        [SerializeField, TextArea] private string[] sampleLines =
        {
            "대화 UI를 확인하는 첫 번째 임시 문구입니다.\n자동 넘김이나 빨리 감기를 켜 보세요.",
            "두 번째 대사입니다.\n저장이나 설정을 열면 수동 진행으로 돌아갑니다.",
            "세 번째 대사입니다.\n창을 숨긴 뒤 화면 아무 곳이나 클릭하면 돌아옵니다.",
            "마지막 대사입니다.\n테스트가 끝나면 다시 출력으로 처음부터 확인할 수 있습니다."
        };
        private int sampleIndex = -1;

        // 테스트 씬에서만 다음 대사 요청에 임시 문장을 공급한다.
        private void OnEnable()
        {
            dialogueUI.NextLineRequested += ShowNextSample;
        }

        // 테스트 공급자가 꺼지면 이벤트 연결과 자동 진행을 해제한다.
        private void OnDisable()
        {
            if (dialogueUI != null)
            {
                dialogueUI.NextLineRequested -= ShowNextSample;
                dialogueUI.StopPlayback();
            }
        }

        // 숨김 중에는 테스트 안내와 버튼도 보이지 않고 입력을 받지 않게 한다. 대사 공급 스크립트는 유지한다.
        private void LateUpdate()
        {
            bool visible = !dialogueUI.IsHidden;
            guideGroup.alpha = visible ? 1f : 0f;
            guideGroup.interactable = visible;
            guideGroup.blocksRaycasts = visible;
        }

        // 테스트 씬 진입 시 화자가 있는 임시 문장을 순차 출력한다.
        private void Start()
        {
            ShowSample();
        }

        // 자동 진행을 해제하고 첫 임시 대사부터 다시 시작한다. 테스트 대사만 읽은 상태로 지정한다.
        public void ShowSample()
        {
            dialogueUI.StopPlayback();
            sampleIndex = -1;
            ShowNextSample();
        }

        // 임시 배열의 다음 문장을 공급하며 마지막 요청에서는 대화를 종료해 반복 진행을 막는다.
        private void ShowNextSample()
        {
            sampleIndex++;
            if (sampleIndex >= sampleLines.Length)
            {
                dialogueUI.EndDialogue();
                testStatus.text = "테스트 대화 끝 / 다시 출력으로 재시작";
                return;
            }

            testStatus.text = "테스트 대사 " + (sampleIndex + 1) + " / " + sampleLines.Length;
            dialogueUI.ShowLine("화자 이름", sampleLines[sampleIndex], true);
        }

        // 출력 중에도 화자 없는 새 문장으로 교체해 이전 출력이 섞이지 않는지 확인한다.
        public void ShowNarration()
        {
            dialogueUI.StopPlayback();
            sampleIndex = sampleLines.Length;
            testStatus.text = "화자 없는 대사 테스트";
            dialogueUI.ShowLine("", "화자 이름이 없는 대사입니다.\n출력 중에 클릭하면 문장 전체를 표시합니다.");
        }

        // 화자와 본문이 비어 있을 때 즉시 출력 완료 상태가 되는지 확인한다.
        public void ShowEmpty()
        {
            dialogueUI.StopPlayback();
            sampleIndex = sampleLines.Length;
            testStatus.text = "빈 대사 테스트";
            dialogueUI.ShowLine(null, null);
        }
    }
}
