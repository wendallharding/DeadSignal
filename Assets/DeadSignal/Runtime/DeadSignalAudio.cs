using System;
using System.Collections.Generic;
using Reflex.Attributes;
using UnityEngine;

namespace DeadSignal
{
    public enum DeadSignalAudioCue
    {
        Fire,
        SignalImpact,
        SecurityImpact,
        SapperPulse,
        TowerOnline,
        Salvage,
        Shortcut,
        Extraction
    }

    internal interface IDeadSignalAudio
    {
        bool HasGeneratedClips { get; }
        int PlayedCueCount { get; }

        void Tick(bool isPowered, bool towerOnline, float signalRatio);
        void Play(DeadSignalAudioCue cue);
        void SetPaused(bool paused);
    }

    /// <summary>
    /// Builds the prototype's complete audio identity at runtime from original synthesized waveforms.
    /// </summary>
    public sealed class DeadSignalAudio : MonoBehaviour, IDeadSignalAudio
    {
        private const int SAMPLE_RATE = 22050;
        private const float AMBIENCE_SECONDS = 2f;
        private const float CUE_SECONDS = 0.22f;

        private readonly Dictionary<DeadSignalAudioCue, AudioClip> m_cues = new();

        private IComfortSettings m_comfortSettings;
        private AudioSource m_deadZoneLoop;
        private AudioSource m_poweredLoop;
        private AudioSource m_cueSource;
        private bool m_isPaused;

        public bool HasGeneratedClips => m_deadZoneLoop != null && m_deadZoneLoop.clip != null &&
                                         m_poweredLoop != null && m_poweredLoop.clip != null && m_cues.Count == 8;
        public int PlayedCueCount { get; private set; }
        public bool AudioEnabled => m_comfortSettings?.AudioEnabled ?? true;
        public float DeadZoneVolume => m_deadZoneLoop != null ? m_deadZoneLoop.volume : 0f;
        public float PoweredVolume => m_poweredLoop != null ? m_poweredLoop.volume : 0f;

        [Inject]
        private void _construct(IComfortSettings comfortSettings)
        {
            m_comfortSettings = comfortSettings;
            m_comfortSettings.AudioEnabledChanged += _handleAudioEnabledChanged;
        }

        private void Awake()
        {
            m_deadZoneLoop = _createSource("Dead Zone Machinery Loop", true);
            m_poweredLoop = _createSource("Powered Network Loop", true);
            m_cueSource = _createSource("Signal Cue Source", false);

            m_deadZoneLoop.clip = _createLoop("Dead Zone Machinery", 43f, 0.045f, 17);
            m_poweredLoop.clip = _createLoop("Powered Network", 91f, 0.035f, 29);
            _createCues();

            m_deadZoneLoop.Play();
            m_poweredLoop.Play();
            _applyAudioEnabled(AudioEnabled);
        }

        public void Tick(bool isPowered, bool towerOnline, float signalRatio)
        {
            if (m_deadZoneLoop == null || m_poweredLoop == null)
            {
                return;
            }

            var lowSignalTension = 1f - Mathf.Clamp01(signalRatio);
            var deadTarget = isPowered ? 0.035f : 0.14f + lowSignalTension * 0.05f;
            var poweredTarget = towerOnline && isPowered ? 0.12f : 0.018f;
            m_deadZoneLoop.volume = Mathf.MoveTowards(m_deadZoneLoop.volume, deadTarget, Time.unscaledDeltaTime * 0.35f);
            m_poweredLoop.volume = Mathf.MoveTowards(m_poweredLoop.volume, poweredTarget, Time.unscaledDeltaTime * 0.35f);
            m_deadZoneLoop.pitch = 0.92f + lowSignalTension * 0.12f;
        }

        public void Play(DeadSignalAudioCue cue)
        {
            if (!AudioEnabled || m_isPaused || m_cueSource == null || !m_cues.TryGetValue(cue, out var clip))
            {
                return;
            }

            m_cueSource.PlayOneShot(clip, _cueVolume(cue));
            PlayedCueCount++;
        }

        public void SetPaused(bool paused)
        {
            m_isPaused = paused;
            if (m_deadZoneLoop == null || m_poweredLoop == null)
            {
                return;
            }

            if (paused)
            {
                m_deadZoneLoop.Pause();
                m_poweredLoop.Pause();
                m_cueSource.Pause();
                return;
            }

            if (AudioEnabled)
            {
                m_deadZoneLoop.UnPause();
                m_poweredLoop.UnPause();
                m_cueSource.UnPause();
            }
        }

        private void OnDestroy()
        {
            if (m_comfortSettings != null)
            {
                m_comfortSettings.AudioEnabledChanged -= _handleAudioEnabledChanged;
            }

            if (m_deadZoneLoop != null)
            {
                Destroy(m_deadZoneLoop.clip);
            }

            if (m_poweredLoop != null)
            {
                Destroy(m_poweredLoop.clip);
            }

            foreach (var clip in m_cues.Values)
            {
                Destroy(clip);
            }
        }

        private AudioSource _createSource(string sourceName, bool loop)
        {
            var sourceRoot = new GameObject(sourceName);
            sourceRoot.transform.SetParent(transform, false);
            var source = sourceRoot.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = 0f;
            return source;
        }

        private AudioClip _createLoop(string clipName, float baseFrequency, float noiseAmount, int noiseSeed)
        {
            var sampleCount = Mathf.RoundToInt(SAMPLE_RATE * AMBIENCE_SECONDS);
            var samples = new float[sampleCount];
            uint noiseState = (uint)noiseSeed;
            for (var index = 0; index < sampleCount; index++)
            {
                var time = (float)index / SAMPLE_RATE;
                noiseState = noiseState * 1664525u + 1013904223u;
                var noise = ((noiseState >> 8) / 16777215f * 2f - 1f) * noiseAmount;
                var hum = Mathf.Sin(2f * Mathf.PI * baseFrequency * time) * 0.34f;
                var harmonic = Mathf.Sin(2f * Mathf.PI * baseFrequency * 2.01f * time) * 0.12f;
                samples[index] = (hum + harmonic + noise) * _edgeFade(index, sampleCount, 220);
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void _createCues()
        {
            m_cues.Add(DeadSignalAudioCue.Fire, _createCue("Signal Bolt", 520f, 0.48f, false));
            m_cues.Add(DeadSignalAudioCue.SignalImpact, _createCue("Signal Impact", 310f, 0.52f, false));
            m_cues.Add(DeadSignalAudioCue.SecurityImpact, _createCue("Security Impact", 92f, 0.68f, true));
            m_cues.Add(DeadSignalAudioCue.SapperPulse, _createCue("Sapper Drain", 145f, 0.62f, true));
            m_cues.Add(DeadSignalAudioCue.TowerOnline, _createCue("Tower Online", 740f, 0.64f, false));
            m_cues.Add(DeadSignalAudioCue.Salvage, _createCue("Salvage Secured", 880f, 0.55f, false));
            m_cues.Add(DeadSignalAudioCue.Shortcut, _createCue("Shortcut Open", 240f, 0.58f, true));
            m_cues.Add(DeadSignalAudioCue.Extraction, _createCue("Extraction Complete", 1040f, 0.68f, false));
        }

        private AudioClip _createCue(string clipName, float frequency, float amplitude, bool descending)
        {
            var sampleCount = Mathf.RoundToInt(SAMPLE_RATE * CUE_SECONDS);
            var samples = new float[sampleCount];
            var phase = 0f;
            for (var index = 0; index < sampleCount; index++)
            {
                var progress = (float)index / sampleCount;
                var sweep = descending ? Mathf.Lerp(1.35f, 0.62f, progress) : Mathf.Lerp(0.78f, 1.25f, progress);
                phase += 2f * Mathf.PI * frequency * sweep / SAMPLE_RATE;
                var envelope = Mathf.Sin(Mathf.PI * progress) * (1f - progress * 0.35f);
                samples[index] = (Mathf.Sin(phase) + Mathf.Sin(phase * 2.02f) * 0.22f) * amplitude * envelope;
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, SAMPLE_RATE, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private float _edgeFade(int index, int sampleCount, int fadeSamples)
        {
            var fadeIn = Mathf.Clamp01((float)index / fadeSamples);
            var fadeOut = Mathf.Clamp01((float)(sampleCount - index - 1) / fadeSamples);
            return Mathf.Min(fadeIn, fadeOut);
        }

        private float _cueVolume(DeadSignalAudioCue cue)
        {
            return cue == DeadSignalAudioCue.SecurityImpact || cue == DeadSignalAudioCue.SapperPulse ? 0.42f : 0.34f;
        }

        private void _handleAudioEnabledChanged(bool enabled)
        {
            _applyAudioEnabled(enabled);
        }

        private void _applyAudioEnabled(bool enabled)
        {
            if (m_deadZoneLoop == null || m_poweredLoop == null || m_cueSource == null)
            {
                return;
            }

            m_deadZoneLoop.mute = !enabled;
            m_poweredLoop.mute = !enabled;
            m_cueSource.mute = !enabled;
            if (enabled && !m_isPaused)
            {
                if (!m_deadZoneLoop.isPlaying)
                {
                    m_deadZoneLoop.Play();
                }

                if (!m_poweredLoop.isPlaying)
                {
                    m_poweredLoop.Play();
                }
            }
        }
    }
}
