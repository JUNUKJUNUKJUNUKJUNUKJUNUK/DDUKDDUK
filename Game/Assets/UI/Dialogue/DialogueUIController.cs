// 역할: 대사 출력, 수동·자동·빨리 감기 진행과 패널 및 숨기기·복귀 입력을 관리한다.
// 스토리 진행과 저장 데이터 처리는 담당 시스템의 API가 공유된 뒤 연결한다.
using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace DdukDduk.Dialogue
{
    public enum DialoguePlaybackMode
    {
        Manual,
        Auto,
        Skip
    }

    public sealed class DialogueUIController : MonoBehaviour
    {
        [SerializeField] private GameObject dialogueContent;
        [SerializeField] private GameObject restoreInput;
        [SerializeField] private GameObject saveLoadPanel;
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private TMP_Text saveLoadTitle;
        [SerializeField] private TMP_Text slotFeedback;
        [SerializeField] private TMP_Text feedback;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text dialogueLabel;
        [SerializeField] private GameObject speakerPlate;
        [SerializeField] private GameObject nextHint;
        [SerializeField, Min(1f)] private float charactersPerSecond = 30f;
        [SerializeField, Min(.05f)] private float autoAdvanceDelay = 1.5f;
        [SerializeField, Min(.05f)] private float skipAdvanceDelay = .12f;
        [SerializeField] private TMP_Text autoLabel;
        [SerializeField] private TMP_Text skipLabel;

        private bool saving;
        private bool hasLine;
        private int characterCount;
        private float revealProgress;
        private Coroutine revealRoutine;
        private bool canSkipLine;
        private bool waitingForNextLine;
        private float nextAdvanceAt = float.PositiveInfinity;
        private int hiddenAtFrame = -1;
        private int restoredAtFrame = -1;

        // 스토리 연결 시 구독자가 다음 문장을 ShowLine으로 공급하거나 EndDialogue로 종료한다.
        public event Action NextLineRequested;

        public bool IsRevealing => hasLine && dialogueLabel.maxVisibleCharacters < characterCount;
        public bool IsHidden => !dialogueContent.activeSelf;
        public DialoguePlaybackMode PlaybackMode { get; private set; }

        // 대화 UI를 켜면 수동 모드로 돌아가고, 중단했던 문장이 있으면 남은 글자부터 출력을 재개한다.
        private void OnEnable()
        {
            StopPlayback();
            ClosePanels();
            ShowDialogue();
            feedback.text = "";
            nextHint.SetActive(hasLine && !IsRevealing);
            if (IsRevealing)
            {
                revealRoutine = StartCoroutine(RevealLine());
            }
        }

        // UI가 꺼지면 자동 진행과 코루틴을 정리한다. 문장과 출력 위치는 재활성화를 위해 유지한다.
        private void OnDisable()
        {
            StopPlayback();
            StopReveal();
        }

        // 활성 모드의 대기 시간이 끝나면 다음 문장을 한 번만 요청한다. 응답을 기다리는 동안 중복 요청하지 않는다.
        private void Update()
        {
            if (PlaybackMode == DialoguePlaybackMode.Manual)
            {
                return;
            }

            if (IsInputBlocked())
            {
                StopPlayback();
                return;
            }

            if (!hasLine || waitingForNextLine || IsRevealing || Time.unscaledTime < nextAdvanceAt)
            {
                return;
            }

            RequestNextLine();
        }

        // 전달받은 화자와 대사로 현재 문장을 교체한다. isRead는 호출자가 판정한 읽은 대사 여부이며 기본값은 false다.
        // null은 빈 문자열로 처리하고, 읽지 않은 새 대사를 받으면 빨리 감기를 해제한다.
        // 비활성 상태에서 호출하면 표시 데이터만 준비하고 OnEnable에서 출력을 시작한다.
        public void ShowLine(string speakerName, string dialogueText, bool isRead = false)
        {
            StopReveal();
            waitingForNextLine = false;
            canSkipLine = isRead;
            if (PlaybackMode == DialoguePlaybackMode.Skip && !canSkipLine)
            {
                StopPlayback();
            }

            speakerLabel.text = speakerName ?? "";
            speakerPlate.SetActive(!string.IsNullOrWhiteSpace(speakerName));
            dialogueLabel.text = dialogueText ?? "";
            dialogueLabel.maxVisibleCharacters = int.MaxValue;
            dialogueLabel.ForceMeshUpdate(true, true);
            characterCount = dialogueLabel.textInfo.characterCount;

            hasLine = true;
            revealProgress = 0f;
            dialogueLabel.maxVisibleCharacters = 0;
            nextHint.SetActive(characterCount == 0);
            feedback.text = "";
            ScheduleAdvance();
            if (PlaybackMode == DialoguePlaybackMode.Skip)
            {
                CompleteLine();
                return;
            }

            if (isActiveAndEnabled && IsRevealing)
            {
                revealRoutine = StartCoroutine(RevealLine());
            }
        }

        // 게임 시간 배율과 독립적인 경과 시간으로 보이는 TMP 문자 수를 늘린다.
        // 보조 패널이나 숨기기 상태에서는 경과 시간을 누적하지 않아 복귀 시 글자가 한꺼번에 나타나지 않는다.
        private IEnumerator RevealLine()
        {
            while (IsRevealing)
            {
                yield return null;
                if (IsInputBlocked())
                {
                    continue;
                }

                revealProgress += Time.unscaledDeltaTime * Mathf.Max(1f, charactersPerSecond);
                dialogueLabel.maxVisibleCharacters = Mathf.Min(characterCount, Mathf.FloorToInt(revealProgress));
            }

            revealRoutine = null;
            nextHint.SetActive(true);
            ScheduleAdvance();
        }

        // 수동 클릭은 자동 진행을 해제한 뒤 현재 문장을 완성하거나 다음 대사를 요청한다. 복귀 클릭은 무시한다.
        public void OnDialogueClicked()
        {
            if (!hasLine || IsInputBlocked())
            {
                return;
            }

            StopPlayback();
            if (IsRevealing)
            {
                CompleteLine();
                return;
            }

            RequestNextLine();
        }

        // 현재 출력 코루틴을 중단하고 문장 전체와 다음 대사 안내를 표시한다. 스토리 진행은 변경하지 않는다.
        public void CompleteLine()
        {
            if (!hasLine)
            {
                return;
            }

            StopReveal();
            dialogueLabel.maxVisibleCharacters = characterCount;
            revealProgress = characterCount;
            nextHint.SetActive(true);
            ScheduleAdvance();
        }

        // 패널·숨김 상태 및 복귀한 바로 그 프레임에는 대사 입력과 글자 출력을 막는다.
        private bool IsInputBlocked()
        {
            return !isActiveAndEnabled || !dialogueContent.activeInHierarchy
                || restoredAtFrame == Time.frameCount
                || saveLoadPanel.activeSelf || historyPanel.activeSelf || settingsPanel.activeSelf;
        }

        // 기존 출력 코루틴만 중단한다. 새 대사 교체·문장 완성·비활성화에서 공통으로 사용한다.
        private void StopReveal()
        {
            if (revealRoutine != null)
            {
                StopCoroutine(revealRoutine);
                revealRoutine = null;
            }
        }

        // 저장 슬롯 선택창을 연다. 실제 파일 쓰기는 슬롯 선택 후 연결할 예정이다.
        public void OpenSave()
        {
            OpenSaveLoad(true);
        }

        // 불러오기 슬롯 선택창을 연다. 현재는 실제 저장 파일을 읽지 않는다.
        public void OpenLoad()
        {
            OpenSaveLoad(false);
        }

        // 자동 진행을 해제하고 save에 따라 저장 또는 불러오기 슬롯 패널을 표시한다.
        private void OpenSaveLoad(bool save)
        {
            StopPlayback();
            ClosePanels();
            saving = save;
            saveLoadTitle.text = save ? "저장" : "불러오기";
            slotFeedback.text = "";
            saveLoadPanel.SetActive(true);
            feedback.text = "";
        }

        // slot은 사용자에게 표시되는 1~3번 저장 파일 번호다. 연결 전에는 안내만 표시한다.
        public void SelectSlot(int slot)
        {
            if (slot < 1 || slot > 3)
            {
                return;
            }

            // 연결 필요: 현재 대화 위치·선택 결과를 공통 저장 데이터로 수집하거나 복원한다.
            slotFeedback.text = "슬롯 " + slot + " / " + (saving ? "저장" : "불러오기") + " 연결 대기";
        }

        // 자동 진행을 해제하고 기록 패널을 연다. 실제 기록은 스토리 대사 수신 방식이 정해진 뒤 연결한다.
        public void OpenHistory()
        {
            StopPlayback();
            ClosePanels();
            historyPanel.SetActive(true);
            feedback.text = "";

            // 연결 필요: 이미 표시한 화자 이름과 대사 목록.
        }

        // 자동 진행을 해제하고 공통 설정을 표시한다. 음량과 화면 설정은 기존 컨트롤러가 처리한다.
        public void OpenSettings()
        {
            StopPlayback();
            ClosePanels();
            settingsPanel.SetActive(true);
            feedback.text = "";
        }

        // 모든 보조 패널을 닫아 서로 겹치거나 뒤쪽 입력을 가리는 상황을 해제한다.
        public void ClosePanels()
        {
            saveLoadPanel.SetActive(false);
            historyPanel.SetActive(false);
            settingsPanel.SetActive(false);
        }

        // 자동 진행을 해제하고 대화 UI를 숨긴다. 화면 전체의 투명 입력 영역만 켜 복귀 클릭을 받는다.
        public void HideDialogue()
        {
            StopPlayback();
            ClosePanels();
            hiddenAtFrame = Time.frameCount;
            dialogueContent.SetActive(false);
            restoreInput.SetActive(true);
        }

        // 대화 영역을 수동 모드로 복원하고 투명 입력 영역을 끈다. 숨기기 전에 켰던 모드는 재개하지 않는다.
        public void ShowDialogue()
        {
            StopPlayback();
            dialogueContent.SetActive(true);
            restoreInput.SetActive(false);
        }

        // 화면 전체 입력 영역에서 호출한다. 숨기기를 누른 프레임은 무시하고 복귀 클릭의 대사 진행을 차단한다.
        public void RestoreFromPointer()
        {
            if (!IsHidden || hiddenAtFrame == Time.frameCount)
            {
                return;
            }

            restoredAtFrame = Time.frameCount;
            ShowDialogue();
        }

        // 완료된 문장마다 다음 대사를 한 번 요청한다. 공급자가 없으면 자동 진행을 해제하고 연결 대기를 알린다.
        public void RequestNextLine()
        {
            if (!hasLine || IsRevealing || IsInputBlocked() || waitingForNextLine)
            {
                return;
            }

            if (NextLineRequested == null)
            {
                StopPlayback();
                feedback.text = "다음 대사 / 스토리 연결 대기";
                return;
            }

            waitingForNextLine = true;
            nextAdvanceAt = float.PositiveInfinity;
            nextHint.SetActive(false);

            // 연결 필요: 실제 스토리 공급자는 다음 문장과 읽은 여부를 ShowLine에 전달한다.
            // 선택지에 도달하면 StopPlayback을, 대화가 끝나면 EndDialogue를 호출한다.
            NextLineRequested.Invoke();
        }

        // 자동 넘김을 켜거나 다시 누르면 해제한다. 켜면 문장 출력 완료 후 설정한 시간만큼 기다린다.
        public void RequestAuto()
        {
            if (!hasLine || IsInputBlocked())
            {
                return;
            }

            SetPlaybackMode(PlaybackMode == DialoguePlaybackMode.Auto
                ? DialoguePlaybackMode.Manual : DialoguePlaybackMode.Auto);
        }

        // 읽은 문장에만 빨리 감기를 켜고 즉시 전체 표시한다. 같은 버튼을 다시 누르면 수동 모드로 돌아간다.
        public void RequestSkip()
        {
            if (!hasLine || IsInputBlocked())
            {
                return;
            }

            if (PlaybackMode == DialoguePlaybackMode.Skip)
            {
                StopPlayback();
                return;
            }

            if (!canSkipLine)
            {
                StopPlayback();
                feedback.text = "읽지 않은 대사는 빨리 감기를 할 수 없습니다.";
                return;
            }

            SetPlaybackMode(DialoguePlaybackMode.Skip);
            CompleteLine();
        }

        // 패널·숨기기·선택지 등 자동 진행을 멈춰야 할 때 사용한다. 현재 글자 출력 위치는 바꾸지 않는다.
        public void StopPlayback()
        {
            SetPlaybackMode(DialoguePlaybackMode.Manual);
        }

        // 스토리 공급자가 대화 종료를 알릴 때 호출한다. 마지막 문장을 남기고 추가 진행 요청을 차단한다.
        public void EndDialogue()
        {
            CompleteLine();
            StopPlayback();
            waitingForNextLine = false;
            hasLine = false;
            nextHint.SetActive(false);
        }

        // 단일 모드 값으로 자동·빨리 감기의 동시 활성화를 막고 버튼 문구와 대기 시간을 함께 갱신한다.
        private void SetPlaybackMode(DialoguePlaybackMode mode)
        {
            PlaybackMode = mode;
            autoLabel.text = mode == DialoguePlaybackMode.Auto ? "자동 넘김 켜짐" : "자동 넘김";
            skipLabel.text = mode == DialoguePlaybackMode.Skip ? "빨리 감기 켜짐" : "빨리 감기";
            ScheduleAdvance();
        }

        // 문장이 완료된 시점 또는 모드 전환 시점부터 새 대기 시간을 계산한다. 수동·출력 중에는 예약하지 않는다.
        private void ScheduleAdvance()
        {
            nextAdvanceAt = float.PositiveInfinity;
            if (!hasLine || IsRevealing || waitingForNextLine || PlaybackMode == DialoguePlaybackMode.Manual)
            {
                return;
            }

            float delay = PlaybackMode == DialoguePlaybackMode.Auto ? autoAdvanceDelay : skipAdvanceDelay;
            nextAdvanceAt = Time.unscaledTime + Mathf.Max(.05f, delay);
        }
    }
}
