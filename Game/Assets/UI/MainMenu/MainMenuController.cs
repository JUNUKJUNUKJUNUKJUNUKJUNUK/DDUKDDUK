using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DdukDduk.MainMenu
{
    // 실제 저장 기능과 연결하기 전 Inspector에서 슬롯의 표시 상태를 지정한다.
    [Serializable]
    public class SaveSlotPreview
    {
        public bool hasSaveData;
        public string summary = "임시 저장 기록";
    }

    // 시작 화면의 표시와 선택 피드백만 담당하며 게임 진행이나 저장 파일을 변경하지 않는다.
    public class MainMenuController : MonoBehaviour
    {
        [Header("임시 슬롯 — 저장 있음 여부를 Inspector에서 설정")]
        [SerializeField] private SaveSlotPreview[] slots =
        {
            new SaveSlotPreview(), new SaveSlotPreview(), new SaveSlotPreview()
        };

        [Header("화면 연결")]
        [SerializeField] private UnityEngine.UI.Button continueButton;
        [SerializeField] private GameObject slotPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private UnityEngine.UI.Button[] slotButtons;
        [SerializeField] private TMP_Text[] slotLabels;
        [SerializeField] private TMP_Text feedback;

        public IReadOnlyList<SaveSlotPreview> Slots => slots;

        private void Start()
        {
            ClosePanels();
            RefreshSlots();
        }

        // 하나라도 임시 저장 기록이 있으면 이어 하기를 활성화하고 각 슬롯의 표시를 갱신한다.
        [ContextMenu("슬롯 표시 새로고침")]
        public void RefreshSlots()
        {
            bool hasAnySave = false;
            for (int index = 0; index < slots.Length; index++)
            {
                bool hasSave = slots[index] != null && slots[index].hasSaveData;
                hasAnySave |= hasSave;
                slotButtons[index].interactable = hasSave;
                slotLabels[index].text = "슬롯 " + (index + 1) + "\n"
                    + (hasSave ? slots[index].summary : "저장된 기록이 없습니다.");
            }
            continueButton.interactable = hasAnySave;
            if (!hasAnySave)
                slotPanel.SetActive(false);
        }

        // 새 게임 생성이나 씬 이동 없이 선택했다는 피드백만 표시한다.
        public void SelectNewGame()
        {
            feedback.text = "새로 하기를 선택했습니다.";
        }

        // 저장 기록이 있는 경우에만 슬롯 선택 패널을 연다.
        public void OpenSlots()
        {
            RefreshSlots();
            if (!continueButton.interactable)
                return;
            settingsPanel.SetActive(false);
            slotPanel.SetActive(true);
            feedback.text = "";
        }

        // 선택한 슬롯의 피드백만 표시하며 실제 저장 데이터를 불러오지 않는다.
        public void SelectSlot(int index)
        {
            if (index < 0 || index >= slots.Length || slots[index] == null || !slots[index].hasSaveData)
                return;
            feedback.text = "슬롯 " + (index + 1) + "번을 선택했습니다.";
        }

        // 실제 설정 항목이 없는 빈 패널을 연다.
        public void OpenSettings()
        {
            slotPanel.SetActive(false);
            settingsPanel.SetActive(true);
            feedback.text = "";
        }

        public void ClosePanels()
        {
            slotPanel.SetActive(false);
            settingsPanel.SetActive(false);
        }

        // 빌드에서는 앱을 종료하고 Editor에서는 현재 Play Mode만 종료한다.
        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
