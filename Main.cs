using MelonLoader;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(GrannyRecapturedMods.MasterMod), "Granny Helper", "1.3.0", "13.davidd")]
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
        
        // Turbo Variables
        private AudioSource[] _cachedAudioSources;
        private bool _isMenu = true;
        private bool _isSpeeding = false;
        private float _pendingFovChange = -1f;

        private const float TurboSpeed = 10f;
        private const KeyCode TurboKey = KeyCode.B;
        private const KeyCode RestartKey = KeyCode.V;

        public override void OnInitializeMelon()
        {
            MyConfig = MelonPreferences.CreateCategory("GrannyMod_Settings");
            FovEntry = MyConfig.CreateEntry<float>("SavedFOV", 110f, "Your Custom FOV");

            LoggerInstance.Msg("------------------------------------------------");
            LoggerInstance.Msg(" GRANNY HELPER v1.3.0");
            LoggerInstance.Msg(" [V] Instant Restart");
            LoggerInstance.Msg(" [B] Ultra-Skip (Fixed Audio)");
            LoggerInstance.Msg("------------------------------------------------");

            var consoleThread = new Thread(ConsoleInputListener) { IsBackground = true };
            consoleThread.Start();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            _cachedCamera = null;
            _player = null;
            _cachedAudioSources = null;
            _isSpeeding = false;

            _isMenu = sceneName.Equals("Menu", StringComparison.OrdinalIgnoreCase);
            
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
            ResetAllAudio(); 
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void HandleTurboMode()
        {
            if (Time.timeScale == 0f && !_isSpeeding) return;

            bool safe = IsSafeToSkip();

            if (Input.GetKey(TurboKey) && safe)
            {
                if (!_isSpeeding)
                {
                    // --- START TURBO ---
                    _isSpeeding = true;
                    Time.timeScale = TurboSpeed;

                    // Find current audio and pitch UP
                    _cachedAudioSources = UnityEngine.Object.FindObjectsOfType<AudioSource>();
                    ApplyPitchToCache(_cachedAudioSources, TurboSpeed);
                }
            }
            else
            {
                if (_isSpeeding)
                {
                    // --- STOP TURBO ---
                    _isSpeeding = false;
                    Time.timeScale = 1.0f;

                    // FIX: Perform a FRESH scan here. 
                    // This finds any audio that started playing *during* the skip
                    // and forces it back to normal.
                    ResetAllAudio();
                    
                    _cachedAudioSources = null;
                }
            }
        }

        private void ResetAllAudio()
        {
            var allAudio = UnityEngine.Object.FindObjectsOfType<AudioSource>();
            ApplyPitchToCache(allAudio, 1.0f);
        }

        private void ApplyPitchToCache(AudioSource[] sources, float pitch)
        {
            if (sources == null) return;
            foreach (var audio in sources)
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
