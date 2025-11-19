using MelonLoader;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(GrannyRecapturedMods.MasterMod), "Granny Helper", "1.2.0", "13.davidd")]
[assembly: MelonGame("Buttery Stancakes", "Granny - Recaptured")]
[assembly: MelonColor(0, 255, 0, 255)]

namespace GrannyRecapturedMods
{
    public class MasterMod : MelonMod
    {
        public static MelonPreferences_Category MyConfig;
        public static MelonPreferences_Entry<float> FovEntry;

        private Camera _cachedCamera;
        private FirstPersonController _player;
        private AudioSource[] _cachedAudioSources;

        private const float TurboSpeed = 10f;
        private const KeyCode TurboKey = KeyCode.B;
        private const KeyCode RestartKey = KeyCode.V;

        private bool _isSpeeding = false;
        private float _pendingFovChange = -1f;

        public override void OnInitializeMelon()
        {
            MyConfig = MelonPreferences.CreateCategory("GrannyMod_Settings");
            FovEntry = MyConfig.CreateEntry<float>("SavedFOV", 110f, "Your Custom FOV");

            LoggerInstance.Msg("------------------------------------------------");
            LoggerInstance.Msg(" GRANNY HELPER v1.2.0");
            LoggerInstance.Msg(" [V] Clean Restart");
            LoggerInstance.Msg(" [B] Ultra-Skip");
            LoggerInstance.Msg("------------------------------------------------");

            var consoleThread = new Thread(ConsoleInputListener) { IsBackground = true };
            consoleThread.Start();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            _cachedCamera = null;
            _player = null;
            _isSpeeding = false;
            _cachedAudioSources = null;
            Time.timeScale = 1.0f;
        }

        public override void OnUpdate()
        {
            if (_pendingFovChange != -1f)
            {
                FovEntry.Value = _pendingFovChange;
                MyConfig.SaveToFile();
                _pendingFovChange = -1f;
            }

            if (Input.GetKeyDown(RestartKey))
            {
                MelonCoroutines.Start(PerformCleanRestartRoutine());
            }

            HandleTurboMode();
        }

        public override void OnLateUpdate()
        {
            if (_cachedCamera == null)
            {
                _cachedCamera = Camera.main;
                return;
            }

            if (Math.Abs(_cachedCamera.fieldOfView - FovEntry.Value) > 0.1f)
            {
                _cachedCamera.fieldOfView = FovEntry.Value;
            }
        }

        private IEnumerator PerformCleanRestartRoutine()
        {
            Time.timeScale = 1.0f;
            ApplyPitchToCache(1.0f);

            SceneManager.LoadScene("Menu");

            while (SceneManager.GetActiveScene().name != "Menu")
                yield return null;

            yield return new WaitForSecondsRealtime(0.1f);

            var menuScript = UnityEngine.Object.FindObjectOfType<Menu>();
            if (menuScript != null)
            {
                menuScript.StartGame();
            }
        }

        private void HandleTurboMode()
        {
            if (Time.timeScale == 0f && !_isSpeeding) return;

            if (Input.GetKey(TurboKey) && IsSafeToSkip())
            {
                if (!_isSpeeding)
                {
                    _isSpeeding = true;
                    Time.timeScale = TurboSpeed;
                    _cachedAudioSources = UnityEngine.Object.FindObjectsOfType<AudioSource>();
                    ApplyPitchToCache(TurboSpeed);
                }
            }
            else
            {
                if (_isSpeeding)
                {
                    _isSpeeding = false;
                    Time.timeScale = 1.0f;
                    ApplyPitchToCache(1.0f);
                    _cachedAudioSources = null;
                }
            }
        }

        private void ApplyPitchToCache(float pitch)
        {
            if (_cachedAudioSources == null) return;

            foreach (var audio in _cachedAudioSources)
            {
                if (audio) audio.pitch = pitch;
            }
        }

        private bool IsSafeToSkip()
        {
            if (_player == null)
                _player = UnityEngine.Object.FindObjectOfType<FirstPersonController>();

            if (_player == null) return false;

            return _player.dead || (DayCounter.InBed && (_player.noControl || _player.frozen));
        }

        private void ConsoleInputListener()
        {
            while (true)
            {
                try
                {
                    string input = Console.ReadLine();
                    if (!string.IsNullOrEmpty(input))
                    {
                        string cleanInput = input.ToLower().Replace("fov", "").Trim();
                        if (float.TryParse(cleanInput, out float newFov))
                        {
                            _pendingFovChange = Mathf.Clamp(newFov, 30f, 160f);
                        }
                    }
                }
                catch { }
            }
        }
    }
}
