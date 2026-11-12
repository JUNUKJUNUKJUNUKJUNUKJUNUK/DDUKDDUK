// 역할: 환경 설정의 저장값과 기본값을 정의한다.
// 구성: 세 음량과 화면 모드 데이터, 잘못된 음량 값의 범위 보정.

using System;
using UnityEngine;

[Serializable]
public sealed class SettingsData
{
    public float masterVolume = 1f;
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;

    public bool fullscreen;
    public void Validate()
    {
        masterVolume = ValidVolume(masterVolume);
        bgmVolume = ValidVolume(bgmVolume);
        sfxVolume = ValidVolume(sfxVolume);
    }

    private static float ValidVolume(float value)
    {
        // 비정상 수치는 기본 음량으로 복구하고, 정상 수치는 슬라이더 범위로 제한한다.
        return float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
    }
}
