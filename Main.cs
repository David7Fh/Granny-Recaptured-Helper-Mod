using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(GrannyRecapturedMods.MasterMod), "Granny Helper", "1.4.1", "13.davidd")]
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

        private Dictionary<int, float> _originalPitches = new Dictionary<int, float>();

        private bool _isMenu = true;
        private bool _isSpeeding = false;
        private float _pendingFovChange = -1f;

        private const float TurboSpeed = 15f;
        private const KeyCode TurboKey = KeyCode.B;
        private const KeyCode RestartKey = KeyCode.V;

        public override void OnInitializeMelon()
        {
            MyConfig = MelonPreferences.CreateCategory("GrannyMod_Settings");
            FovEntry = MyConfig.CreateEntry<float>("SavedFOV", 110f, "Your Custom FOV");

            LoggerInstance.Msg("------------------------------------------------");
            LoggerInstance.Msg(" GRANNY HELPER v1.4.1");
            LoggerInstance.Msg(" [V] Instant Restart (Pause Fix)");
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
            _originalPitches.Clear();

            _isMenu = sceneName.Equals("Menu", StringComparison.OrdinalIgnoreCase);

            Time.timeScale = 1.0f;
            AudioListener.pause = false;
        }

        public override void OnUpdate()
        {
            if (_pendingFovChange != -1f)
            {
                FovEntry.Value = _pendingFovChange;
                MyConfig.SaveToFile();
                _pendingFovChange = -1f;
            }

            if (_isMenu) return;

            if (Input.GetKeyDown(RestartKey))
            {
                PerformInstantRestart();
            }

            HandleTurboMode();
        }

        public override void OnLateUpdate()
        {
            if (_isMenu || _cachedCamera == null)
            {
                if (!_isMenu && _cachedCamera == null) _cachedCamera = Camera.main;
                return;
            }

            if (Math.Abs(_cachedCamera.fieldOfView - FovEntry.Value) > 0.1f)
            {
                _cachedCamera.fieldOfView = FovEntry.Value;
            }
        }

        private void PerformInstantRestart()
        {
            Time.timeScale = 1.0f;
            AudioListener.pause = false;
            RestoreAudioPitches();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

                    var allAudio = UnityEngine.Object.FindObjectsOfType<AudioSource>();
                    _originalPitches.Clear();

                    foreach (var audio in allAudio)
                    {
                        if (audio != null)
                        {
                            int id = audio.GetInstanceID();
                            if (!_originalPitches.ContainsKey(id))
                            {
                                _originalPitches.Add(id, audio.pitch);
                            }
                            audio.pitch *= TurboSpeed;
                        }
                    }
                }
            }
            else
            {
                if (_isSpeeding)
                {
                    _isSpeeding = false;
                    Time.timeScale = 1.0f;
                    RestoreAudioPitches();
                }
            }
        }

        private void RestoreAudioPitches()
        {
            var allAudio = UnityEngine.Object.FindObjectsOfType<AudioSource>();

            foreach (var audio in allAudio)
            {
                if (audio != null)
                {
                    int id = audio.GetInstanceID();
                    if (_originalPitches.ContainsKey(id))
                    {
                        audio.pitch = _originalPitches[id];
                    }
                    else
                    {
                        audio.pitch = 1.0f;
                    }
                }
            }
            _originalPitches.Clear();
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
