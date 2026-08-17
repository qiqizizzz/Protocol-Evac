/*
 * ┌──────────────────────────────────┐
 * │  描    述: 音效播放器                      
 * │  类    名: SoundManager.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Common.Res;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utils.log;

namespace Framework.QTower.Common.Sound
{
    public class SoundManager
    {
        private const string SOUND_ROOT_NAME = "SoundManager";

        private readonly Dictionary<string, AudioClip> m_clipDic;

        private AudioSource m_bgmSource;
        private bool m_isStop;
        private float m_totalVolume;
        private float m_bgmVolume;
        private float m_voiceVolume;
        private float m_effectVolume;

        public bool IsStop
        {
            get => m_isStop;
            set
            {
                m_isStop = value;
                if (m_isStop)
                    m_bgmSource.Pause();
                else if (!m_bgmSource.isPlaying && m_bgmSource.clip != null)
                    m_bgmSource.Play();
            }
        }

        public float TotalVolume
        {
            get => m_totalVolume;
            set
            {
                m_totalVolume = Mathf.Clamp01(value);
                RefreshBgmVolume();
            }
        }

        public float BgmVolume
        {
            get => m_bgmVolume;
            set
            {
                m_bgmVolume = Mathf.Clamp01(value);
                RefreshBgmVolume();
            }
        }

        public float VoiceVolume
        {
            get => m_voiceVolume;
            set => m_voiceVolume = Mathf.Clamp01(value);
        }

        public float EffectVolume
        {
            get => m_effectVolume;
            set => m_effectVolume = Mathf.Clamp01(value);
        }

        // 创建音效管理器并初始化播放节点
        public SoundManager()
        {
            m_clipDic = new Dictionary<string, AudioClip>();
            m_bgmSource = CreateAudioSource();
            TotalVolume = 1f;
            BgmVolume = 1f;
            VoiceVolume = 1f;
            EffectVolume = 1f;
            IsStop = false;
        }

        // 播放 Addressables 中配置的背景音乐
        public void PlayBGM(string res)
        {
            if (m_isStop)
                return;

            PlayBgmAsync(res).Forget();
        }

        // 在指定世界坐标播放 Addressables 中配置的一次性音效
        public void PlayEffect(string res, Vector3 pos)
        {
            if (m_isStop)
                return;

            PlayEffectAsync(res, pos).Forget();
        }

        // 播放非空间化的一次性音效
        public void PlayEffect(string res)
        {
            PlayEffect(res, Vector3.zero);
        }

        // 异步加载并播放背景音乐
        private async UniTaskVoid PlayBgmAsync(string res)
        {
            AudioClip clip = await LoadClipAsync(res);
            if (clip == null || m_isStop)
                return;

            m_bgmSource.clip = clip;
            m_bgmSource.Play();
        }

        // 异步加载并播放一次性音效
        private async UniTaskVoid PlayEffectAsync(string res, Vector3 pos)
        {
            AudioClip clip = await LoadClipAsync(res);
            if (clip == null || m_isStop)
                return;

            float currentVolume = m_effectVolume * m_totalVolume;
            AudioSource.PlayClipAtPoint(clip, pos, currentVolume);
        }

        // 通过 Addressables 地址加载并缓存音频资源
        private async UniTask<AudioClip> LoadClipAsync(string res)
        {
            if (m_clipDic.TryGetValue(res, out AudioClip cachedClip))
                return cachedClip;

            AudioClip clip = await ResManager.LoadAssetAsync<AudioClip>(res);
            if (clip == null)
            {
                QLog.Error($"音频资源不存在：{res}");
                return null;
            }

            if (!m_clipDic.ContainsKey(res))
                m_clipDic.Add(res, clip);

            return clip;
        }

        // 创建跨场景背景音乐播放节点
        private AudioSource CreateAudioSource()
        {
            GameObject soundRoot = new GameObject(SOUND_ROOT_NAME);
            Object.DontDestroyOnLoad(soundRoot);
            AudioSource audioSource = soundRoot.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            return audioSource;
        }

        // 刷新背景音乐最终音量
        private void RefreshBgmVolume()
        {
            if (m_bgmSource == null)
                return;

            m_bgmSource.volume = m_bgmVolume * m_totalVolume;
        }
    }
}
