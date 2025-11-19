using Il2Cpp;
using MelonLoader;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(GrannyRecapturedMods.MasterMod), "Granny Helper", "1.0.0", "13.davidd pentru xslayder")]
[assembly: MelonGame("Buttery Stancakes", "Granny - Recaptured")]
[assembly: MelonColor(0, 255, 0, 255)]

namespace GrannyRecapturedMods
{
    public class MasterMod : MelonMod
    {
        public static MelonPreferences_Category MyConfig;
        public static MelonPreferences_Entry<float> FovEntry;

        private bool _isMenu = true;
        private bool _sceneIsSafe = false;
        private float _safetyTimer = 0f;

        private bool _isRestarting = false;
        private float _restartDelay = 0f;

        private Camera _cachedCamera;
        private FirstPersonController _player;

        private const float TurboSpeed = 10f;
        private const KeyCode TurboKey = KeyCode.B;
        private bool _isSpeeding = false;

        private const KeyCode RestartKey = KeyCode.V;

        public override void OnInitializeMelon()
        {
            MyConfig = MelonPreferences.CreateCategory("GrannyMod_Settings");
            FovEntry = MyConfig.CreateEntry<float>("SavedFOV", 110f, "Your Custom FOV");

            LoggerInstance.Msg("------------------------------------------------");
            LoggerInstance.Msg($" GRANNY HELPER v1.0.0");
            LoggerInstance.Msg($" [V] Clean Restart");
            LoggerInstance.Msg($" [B] Ultra-Skip");
            LoggerInstance.Msg("------------------------------------------------");

            var consoleThread = new Thread(ConsoleInputListener) { IsBackground = true };
            consoleThread.Start();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            _cachedCamera = null;
            _player = null;
            _sceneIsSafe = false;

            ForceAudioPitch(1.0f);
            Time.timeScale = 1.0f;
            _isSpeeding = false;

            if (sceneName.Contains("Menu"))
            {
                _isMenu = true;
                if (_isRestarting)
                {
                    _restartDelay = 0.1f;
                    LoggerInstance.Msg("Restarting: Menu loaded, auto-starting...");
                }
            }
            else
            {
                _isMenu = false;
                _isRestarting = false;
                _safetyTimer = 3.0f;
            }
        }

        public override void OnUpdate()
        {
            if (_isMenu && _isRestarting)
            {
                _restartDelay -= Time.unscaledDeltaTime;
                if (_restartDelay <= 0f)
                {
                    var menuScript = UnityEngine.Object.FindObjectOfType<Menu>();
                    if (menuScript != null)
                    {
                        menuScript.StartGame();
                        _isRestarting = false;
                    }
                }
                return;
            }

            if (_isMenu) return;

            if (Input.GetKeyDown(RestartKey))
            {
                PerformCleanRestart();
                return;
            }

            if (_safetyTimer > 0f)
            {
                _safetyTimer -= Time.deltaTime;
                if (_safetyTimer <= 0f) _sceneIsSafe = true;
                return;
            }

            HandleTurboMode();

            if (_sceneIsSafe && _cachedCamera == null)
            {
                try { _cachedCamera = Camera.main; } catch { }
            }
        }

        public override void OnLateUpdate()
        {
            if (_isMenu || !_sceneIsSafe || _cachedCamera == null) return;

            try
            {
                float target = FovEntry.Value;
                if (Math.Abs(_cachedCamera.fieldOfView - target) > 0.01f)
                {
                    _cachedCamera.fieldOfView = target;
                }
            }
            catch { _cachedCamera = null; }
        }

        private void PerformCleanRestart()
        {
            LoggerInstance.Msg("[Game] Clean Restart Initiated...");

            Time.timeScale = 1.0f;
            ForceAudioPitch(1.0f);
            _isSpeeding = false;

            _isRestarting = true;

            SceneManager.LoadScene("Menu");
        }

        private void HandleTurboMode()
        {
            if (Input.GetKey(TurboKey))
            {
                if (IsSafeToSkip())
                {
                    if (!_isSpeeding)
                    {
                        Time.timeScale = TurboSpeed;
                        ForceAudioPitch(TurboSpeed);
                        _isSpeeding = true;
                    }
                }
                else
                {
                    ResetSpeed();
                }
            }
            else
            {
                ResetSpeed();
            }
        }

        private bool IsSafeToSkip()
        {
            if (_player == null) _player = UnityEngine.Object.FindObjectOfType<FirstPersonController>();
            if (_player == null) return false;

            if (_player.dead) return true;
            if (DayCounter.InBed && (_player.noControl || _player.frozen)) return true;

            return false;
        }

        private void ResetSpeed()
        {
            if (_isSpeeding)
            {
                if (Time.timeScale > 0.1f)
                {
                    Time.timeScale = 1.0f;
                    ForceAudioPitch(1.0f);
                    _isSpeeding = false;
                }
            }
        }

        private void ForceAudioPitch(float pitch)
        {
            var allAudio = UnityEngine.Object.FindObjectsOfType<AudioSource>();
            foreach (var audio in allAudio)
            {
                if (audio != null) audio.pitch = pitch;
            }
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
                            newFov = Mathf.Clamp(newFov, 30f, 160f);
                            FovEntry.Value = newFov;
                            MyConfig.SaveToFile();
                        }
                    }
                }
                catch { }
            }
        }
    }
}