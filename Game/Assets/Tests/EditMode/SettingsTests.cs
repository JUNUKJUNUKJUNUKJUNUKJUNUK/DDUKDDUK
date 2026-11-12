// 역할: 환경 설정 데이터와 저장 처리를 EditMode에서 검증한다.
// 구성: 기본값·손상 데이터, 저장과 복원, 음량 보정 및 데시벨 변환.

using System;
using NUnit.Framework;
using UnityEngine;

public class SettingsTests
{
    private string key;

    // 실제 사용자 설정과 겹치지 않는 키를 테스트마다 새로 만든다.
    [SetUp]
    public void SetUp()
    {
        key = "DDUKDDUK.Tests.Settings." + Guid.NewGuid().ToString("N");
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    [Test]
    public void MissingOrBrokenSettings_UseDefaults()
    {
        AssertDefaults(SettingsService.Read(key));

        PlayerPrefs.SetString(key, "broken json");
        AssertDefaults(SettingsService.Read(key));
    }

    [Test]
    public void SaveAndReload_PreserveThreeVolumesAndScreenMode()
    {
        SettingsService.Write(
            key,
            new SettingsData { masterVolume = .25f, bgmVolume = .5f, sfxVolume = .75f, fullscreen = true });
        var saved = SettingsService.Read(key);

        Assert.That(saved.masterVolume, Is.EqualTo(.25f));
        Assert.That(saved.bgmVolume, Is.EqualTo(.5f));
        Assert.That(saved.sfxVolume, Is.EqualTo(.75f));
        Assert.That(saved.fullscreen, Is.True);
    }

    [Test]
    public void InvalidVolumes_AreClampedOrReset()
    {
        var data = new SettingsData
        {
            masterVolume = -1,
            bgmVolume = 2,
            sfxVolume = float.NaN
        };

        data.Validate();

        Assert.That(data.masterVolume, Is.Zero);
        Assert.That(data.bgmVolume, Is.EqualTo(1));
        Assert.That(data.sfxVolume, Is.EqualTo(1));
    }

    [Test]
    public void AudioGain_ZeroIsSilentAndHalfIsMinusSixDecibels()
    {
        Assert.That(SettingsService.VolumeToDecibels(0), Is.EqualTo(-80));
        Assert.That(SettingsService.VolumeToDecibels(.5f), Is.EqualTo(-6.0206f).Within(.001f));
        Assert.That(SettingsService.VolumeToDecibels(1), Is.Zero);
    }

    private static void AssertDefaults(SettingsData data)
    {
        Assert.That(data.masterVolume, Is.EqualTo(1));
        Assert.That(data.bgmVolume, Is.EqualTo(1));
        Assert.That(data.sfxVolume, Is.EqualTo(1));
        Assert.That(data.fullscreen, Is.False);
    }
}
