using KombiRush.Sim;
using UnityEngine;

namespace KombiRush.Game
{
    /// <summary>
    /// The single entry point. One GameObject in the scene carries this component and it builds
    /// the camera, the road, the UI and the audio at runtime, then drives the simulation at a
    /// fixed step so the game plays identically at 30fps and 60fps.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameRoot : MonoBehaviour
    {
        private const float SimStep = 1f / 60f;
        private const int MaxStepsPerFrame = 5;
        private const float KombiScreenHeight = 0.20f;   // where the kombi sits, 0 = bottom

        private enum State { Menu, Playing, Over, Garage }

        private readonly SimConfig _config = SimConfig.Default();
        private readonly InputSteering _steering = new InputSteering();

        private Profile _profile;
        private RoadSim _sim;
        private RoadView _road;
        private Hud _hud;
        private Screens _screens;
        private Camera _camera;
        private AudioSource _engineSource;
        private AudioSource _sfxSource;

        private State _state = State.Menu;
        private float _accumulator;
        private float _shake;
        private float _wobblePhase;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;

            _camera = SetUpCamera();
            _profile = SaveIO.Load();

            _road = new RoadView(transform, _config);
            Transform canvas = UiKit.CreateCanvas(transform, "UI", 10).transform;
            EnsureEventSystem();
            _hud = new Hud(canvas);
            _screens = new Screens(canvas, _profile, StartRun, OpenGarage, OpenMenu, ToggleSound);

            _engineSource = gameObject.AddComponent<AudioSource>();
            _engineSource.clip = AudioKit.Engine;
            _engineSource.loop = true;
            _engineSource.volume = 0.32f;
            _engineSource.playOnAwake = false;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = 0.7f;

            int bonus = _profile.ClaimDailyBonus(SaveIO.TodayIndex());
            if (bonus > 0) SaveIO.Save(_profile);

            _hud.SetVisible(false);
            _screens.ShowMenu(bonus > 0 ? "Daily bonus paid: " + Economy.FormatCents(bonus) : "");
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            KeepCameraSized();

            if (_state == State.Playing && _sim != null)
            {
                _sim.SetTargetLane(_steering.Sample(_sim.TargetLane, _config.LaneCount));

                _accumulator += dt;
                int steps = 0;
                while (_accumulator >= SimStep && steps < MaxStepsPerFrame && !_sim.IsOver)
                {
                    _sim.Step(SimStep);
                    HandleEvents();
                    _accumulator -= SimStep;
                    steps++;
                }
                if (_accumulator > SimStep * MaxStepsPerFrame) _accumulator = 0f;

                _hud.Sync(_sim, dt);
                if (_sim.IsOver) FinishRun();
            }

            if (_sim != null)
            {
                _wobblePhase += dt * (4f + _sim.Speed * 0.5f);
                float wobble = Mathf.Sin(_wobblePhase) * (_state == State.Playing ? 0.7f : 0.2f);
                _road.Sync(_sim, wobble);
                FollowCamera(dt);
                if (_engineSource.isPlaying)
                    _engineSource.pitch = Mathf.Lerp(_engineSource.pitch,
                        0.75f + Mathf.Clamp01(_sim.Speed / _config.TopSpeed) * 0.85f, dt * 4f);
            }

            // Android back button
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_state == State.Playing) FinishRun();
                else if (_state != State.Menu) OpenMenu();
                else Application.Quit();
            }
        }

        // ---------------------------------------------------------------------
        // flow
        // ---------------------------------------------------------------------
        private void StartRun()
        {
            uint seed = (uint)Random.Range(1, int.MaxValue);
            _sim = new RoadSim(_config, _profile.Upgrades.Clone(), seed);
            _accumulator = 0f;
            _shake = 0f;
            _steering.Reset();
            _road.ClearEntities();
            _hud.ResetForRun();
            _hud.SetVisible(true);
            _screens.ShowNone();
            _state = State.Playing;
            if (_profile.SoundOn) _engineSource.Play();
        }

        private void FinishRun()
        {
            RunResult run = _sim.BuildResult();
            bool newBest = run.DistanceMetres > _profile.BestDistanceMetres;
            _profile.ApplyRun(run);
            SaveIO.Save(_profile);

            _engineSource.Stop();
            PlaySfx(AudioKit.Hit, 0.8f);
            _hud.SetVisible(false);
            _screens.ShowGameOver(run, newBest);
            _state = State.Over;
        }

        private void OpenMenu()
        {
            _state = State.Menu;
            _engineSource.Stop();
            _hud.SetVisible(false);
            _screens.ShowMenu("");
        }

        private void OpenGarage()
        {
            _state = State.Garage;
            _engineSource.Stop();
            _hud.SetVisible(false);
            _screens.ShowGarage();
        }

        private void ToggleSound()
        {
            _profile.SoundOn = !_profile.SoundOn;
            SaveIO.Save(_profile);
            AudioListener.volume = _profile.SoundOn ? 1f : 0f;
            if (!_profile.SoundOn) _engineSource.Stop();
            else if (_state == State.Playing) _engineSource.Play();
            _screens.ShowMenu("");
        }

        private void HandleEvents()
        {
            var events = _sim.Events;
            for (int i = 0; i < events.Count; i++)
            {
                SimEvent ev = events[i];
                switch (ev.Kind)
                {
                    case SimEventKind.Hit:
                        _shake = Mathf.Min(0.45f, _shake + 0.22f * ev.IntValue);
                        PlaySfx(AudioKit.Hit, 0.85f);
                        _hud.Toast(ev.Entity == EntityKind.Pothole ? "POTHOLE!" : "CRASH!", Palette.Bad);
                        break;
                    case SimEventKind.PassengerBoarded:
                        PlaySfx(AudioKit.Board, 0.55f);
                        _hud.Toast("+1 passenger", Palette.Paper);
                        break;
                    case SimEventKind.SeatsFull:
                        _hud.Toast("Kombi is full", Palette.Accent);
                        break;
                    case SimEventKind.CoinCollected:
                        PlaySfx(AudioKit.Coin, 0.5f);
                        break;
                    case SimEventKind.FuelCollected:
                        PlaySfx(AudioKit.Fuel, 0.6f);
                        _hud.Toast("FUEL", Palette.Good);
                        break;
                    case SimEventKind.Payout:
                        PlaySfx(AudioKit.Payout, 0.8f);
                        _hud.Toast(Economy.FormatCents(ev.IntValue) + "  x" + ev.FloatValue.ToString("0.00"),
                            Palette.Accent);
                        break;
                }
            }
        }

        private void PlaySfx(AudioClip clip, float volume)
        {
            if (!_profile.SoundOn || clip == null) return;
            _sfxSource.PlayOneShot(clip, volume);
        }

        // ---------------------------------------------------------------------
        // camera
        // ---------------------------------------------------------------------
        private Camera SetUpCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.DustDark;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.nearClipPlane = -20f;
            cam.farClipPlane = 40f;
            SizeCamera(cam);
            return cam;
        }

        private void SizeCamera(Camera cam)
        {
            float aspect = Screen.height <= 0 ? 0.5625f : (float)Screen.width / Screen.height;
            // the verge either side is 3m: enough to read the kerb and scenery, not so much that
            // the road stops filling the screen. On a 9:16 phone this shows about 27m of road ahead,
            // which is close to two seconds of reaction time at top speed.
            float halfWidthWanted = _config.LaneCount * _config.LaneWidth * 0.5f + 3.0f;
            cam.orthographicSize = Mathf.Clamp(halfWidthWanted / Mathf.Max(0.35f, aspect), 9f, 22f);
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private void KeepCameraSized()
        {
            if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight) return;
            SizeCamera(_camera);
        }

        private void FollowCamera(float dt)
        {
            float size = _camera.orthographicSize;
            float targetY = _sim.PlayerY + size * (1f - 2f * KombiScreenHeight);
            Vector3 p = _camera.transform.position;
            p.y = targetY;
            if (_shake > 0.0005f)
            {
                _shake = Mathf.Max(0f, _shake - dt * 1.6f);
                p.x = Random.Range(-_shake, _shake) * 0.6f;
                p.y += Random.Range(-_shake, _shake) * 0.6f;
            }
            else
            {
                p.x = 0f;
            }
            _camera.transform.position = p;
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) return;
            var go = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            go.transform.SetParent(null);
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            SaveIO.Save(_profile);
            _engineSource.Stop();
        }

        private void OnApplicationQuit() => SaveIO.Save(_profile);
    }
}
