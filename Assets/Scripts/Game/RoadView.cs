using System.Collections.Generic;
using KombiRush.Sim;
using UnityEngine;

namespace KombiRush.Game
{
    /// <summary>
    /// Draws the simulation: tarmac, kerbs, scrolling lane markings, roadside scenery, the kombi
    /// and every entity. Nothing here decides game rules - it only reads RoadSim state.
    /// </summary>
    public sealed class RoadView
    {
        private const int DashCount = 26;
        private const float DashSpacing = 4.2f;
        private const int KerbCount = 30;
        private const float KerbSpacing = 3.0f;
        private const int BushCount = 14;
        private const float BushSpacing = 9.0f;

        private readonly Transform _root;
        private readonly Transform _static;
        private readonly SimConfig _cfg;
        private readonly Transform[] _dashes;
        private readonly Transform[] _kerbsLeft;
        private readonly Transform[] _kerbsRight;
        private readonly Transform[] _bushes;
        private SpriteRenderer[] _kerbSrLeft;
        private SpriteRenderer[] _kerbSrRight;
        private readonly Dictionary<int, SpriteRenderer> _views = new Dictionary<int, SpriteRenderer>(64);
        private readonly Stack<SpriteRenderer> _pool = new Stack<SpriteRenderer>(64);
        private readonly List<int> _stale = new List<int>(32);

        private readonly Transform _kombi;
        private readonly Transform _kombiShadow;
        private readonly SpriteRenderer _kombiRenderer;

        public float RoadWidth => _cfg.LaneCount * _cfg.LaneWidth;

        public RoadView(Transform parent, SimConfig cfg)
        {
            _cfg = cfg;
            _root = new GameObject("Road").transform;
            _root.SetParent(parent, false);
            // the tarmac and verges are single big quads that ride along with the camera, so the
            // road looks endless without spawning anything
            _static = new GameObject("Surface").transform;
            _static.SetParent(_root, false);

            float half = RoadWidth * 0.5f;

            // ground either side of the road, wide enough for any phone aspect
            MakeQuad("GroundLeft", new Vector2(-half - 20f, 0f), new Vector2(40f, 4000f), Palette.Dust, -40);
            MakeQuad("GroundRight", new Vector2(half + 20f, 0f), new Vector2(40f, 4000f), Palette.Dust, -40);
            MakeQuad("Tarmac", Vector2.zero, new Vector2(RoadWidth, 4000f), Palette.Tarmac, -30);
            MakeQuad("EdgeLineLeft", new Vector2(-half + 0.18f, 0f), new Vector2(0.14f, 4000f), Palette.LaneLine, -25);
            MakeQuad("EdgeLineRight", new Vector2(half - 0.18f, 0f), new Vector2(0.14f, 4000f), Palette.LaneLine, -25);

            _dashes = new Transform[DashCount * (cfg.LaneCount - 1)];
            int d = 0;
            for (int lane = 1; lane < cfg.LaneCount; lane++)
            {
                float x = (lane - cfg.LaneCount * 0.5f) * cfg.LaneWidth;
                for (int i = 0; i < DashCount; i++)
                {
                    var t = MakeSprite("Dash", SpriteFactory.LaneDash, Palette.LaneLine, -24).transform;
                    t.localPosition = new Vector3(x, 0f, 0f);
                    _dashes[d++] = t;
                }
            }

            _kerbsLeft = new Transform[KerbCount];
            _kerbsRight = new Transform[KerbCount];
            _kerbSrLeft = new SpriteRenderer[KerbCount];
            _kerbSrRight = new SpriteRenderer[KerbCount];
            for (int i = 0; i < KerbCount; i++)
            {
                _kerbSrLeft[i] = MakeKerb(-half - 0.28f, i);
                _kerbSrRight[i] = MakeKerb(half + 0.28f, i);
                _kerbsLeft[i] = _kerbSrLeft[i].transform;
                _kerbsRight[i] = _kerbSrRight[i].transform;
            }

            _bushes = new Transform[BushCount];
            for (int i = 0; i < BushCount; i++)
            {
                var sr = MakeSprite("Bush", SpriteFactory.RoadsideBush, Color.white, -20);
                _bushes[i] = sr.transform;
            }

            _kombiShadow = MakeSprite("KombiShadow", SpriteFactory.SoftShadow, new Color(1f, 1f, 1f, 0.75f), 8).transform;
            _kombiShadow.localScale = new Vector3(1.25f, 1.6f, 1f);
            _kombiRenderer = MakeSprite("Kombi", SpriteFactory.Kombi, Color.white, 10);
            _kombi = _kombiRenderer.transform;
        }

        public float LaneToX(float lane) => (lane - (_cfg.LaneCount - 1) * 0.5f) * _cfg.LaneWidth;

        public void Sync(RoadSim sim, float wobble)
        {
            float playerX = LaneToX(sim.LaneF);
            float y = sim.PlayerY;

            _kombi.localPosition = new Vector3(playerX, y, 0f);
            float lean = Mathf.Clamp((sim.TargetLane - sim.LaneF) * -7f, -9f, 9f);
            _kombi.localRotation = Quaternion.Euler(0f, 0f, lean + wobble);
            _kombiShadow.localPosition = new Vector3(playerX + 0.12f, y - 0.34f, 0f);

            _static.localPosition = new Vector3(0f, y, 0f);
            ScrollLine(_dashes, DashSpacing, y, DashCount);
            ScrollColumn(_kerbsLeft, _kerbSrLeft, KerbSpacing, y);
            ScrollColumn(_kerbsRight, _kerbSrRight, KerbSpacing, y);
            ScrollBushes(y);
            SyncEntities(sim);
        }

        private void SyncEntities(RoadSim sim)
        {
            var entities = sim.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                Entity e = entities[i];
                if (!e.Alive) continue;
                if (!_views.TryGetValue(e.Id, out SpriteRenderer sr) || sr == null)
                {
                    sr = Rent();
                    Dress(sr, e);
                    _views[e.Id] = sr;
                }
                float laneCentre = e.Lane + (e.Span - 1) * 0.5f;
                sr.transform.localPosition = new Vector3(LaneToX(laneCentre), e.Y, 0f);
            }

            // return views whose entity is gone
            _stale.Clear();
            foreach (KeyValuePair<int, SpriteRenderer> kv in _views)
            {
                bool found = false;
                for (int i = 0; i < entities.Count; i++)
                {
                    if (entities[i].Id != kv.Key || !entities[i].Alive) continue;
                    found = true;
                    break;
                }
                if (!found) _stale.Add(kv.Key);
            }
            for (int i = 0; i < _stale.Count; i++)
            {
                SpriteRenderer sr = _views[_stale[i]];
                _views.Remove(_stale[i]);
                Return(sr);
            }
        }

        public void ClearEntities()
        {
            foreach (KeyValuePair<int, SpriteRenderer> kv in _views) Return(kv.Value);
            _views.Clear();
        }

        private void Dress(SpriteRenderer sr, Entity e)
        {
            sr.color = Color.white;
            sr.transform.localRotation = Quaternion.identity;
            sr.transform.localScale = Vector3.one;
            switch (e.Kind)
            {
                case EntityKind.Pothole:
                    sr.sprite = SpriteFactory.Pothole;
                    sr.sortingOrder = -10;
                    break;
                case EntityKind.Traffic:
                    sr.sprite = SpriteFactory.Traffic(e.Id % 3);
                    sr.sortingOrder = 6;
                    sr.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); // oncoming
                    break;
                case EntityKind.Roadblock:
                    sr.sprite = SpriteFactory.Roadblock;
                    sr.sortingOrder = 5;
                    break;
                case EntityKind.Passenger:
                    sr.sprite = SpriteFactory.Passenger;
                    sr.sortingOrder = 4;
                    break;
                case EntityKind.Coin:
                    sr.sprite = SpriteFactory.Coin;
                    sr.sortingOrder = 3;
                    break;
                case EntityKind.FuelCan:
                    sr.sprite = SpriteFactory.FuelCan;
                    sr.sortingOrder = 3;
                    break;
                case EntityKind.Stop:
                    sr.sprite = SpriteFactory.StopSign;
                    sr.sortingOrder = 2;
                    break;
            }
        }

        private SpriteRenderer Rent()
        {
            if (_pool.Count > 0)
            {
                SpriteRenderer sr = _pool.Pop();
                sr.gameObject.SetActive(true);
                return sr;
            }
            return MakeSprite("Entity", SpriteFactory.Coin, Color.white, 0);
        }

        private void Return(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.gameObject.SetActive(false);
            _pool.Push(sr);
        }

        private void ScrollLine(Transform[] items, float spacing, float cameraY, int perLane)
        {
            if (items.Length == 0) return;
            int lanes = items.Length / perLane;
            float start = Mathf.Floor((cameraY - 12f) / spacing) * spacing;
            for (int lane = 0; lane < lanes; lane++)
            for (int i = 0; i < perLane; i++)
            {
                Transform t = items[lane * perLane + i];
                Vector3 p = t.localPosition;
                p.y = start + i * spacing;
                t.localPosition = p;
            }
        }

        private void ScrollColumn(Transform[] items, SpriteRenderer[] renderers, float spacing, float cameraY)
        {
            float start = Mathf.Floor((cameraY - 12f) / spacing) * spacing;
            for (int i = 0; i < items.Length; i++)
            {
                Transform t = items[i];
                Vector3 p = t.localPosition;
                p.y = start + i * spacing;
                t.localPosition = p;
                // alternate red and white kerb blocks, the way they are painted at home; it also
                // makes speed readable even when the frame rate dips
                bool light = Mathf.RoundToInt(p.y / spacing) % 2 == 0;
                renderers[i].color = light ? Palette.Kerb : Palette.Bad;
            }
        }

        private void ScrollBushes(float cameraY)
        {
            float half = RoadWidth * 0.5f;
            float start = Mathf.Floor((cameraY - 12f) / BushSpacing) * BushSpacing;
            for (int i = 0; i < _bushes.Length; i++)
            {
                Transform t = _bushes[i];
                float y = start + i * BushSpacing;
                bool leftSide = (Mathf.RoundToInt(y / BushSpacing) & 1) == 0;
                float x = leftSide ? -half - 1.6f - (i % 3) * 0.4f : half + 1.6f + (i % 3) * 0.4f;
                t.localPosition = new Vector3(x, y, 0f);
            }
        }

        private SpriteRenderer MakeKerb(float x, int index)
        {
            SpriteRenderer sr = MakeSprite("Kerb", SpriteFactory.Solid, Palette.Kerb, -22);
            sr.transform.localScale = new Vector3(0.34f, KerbSpacing * 0.5f, 1f);
            sr.transform.localPosition = new Vector3(x, index * KerbSpacing, 0f);
            return sr;
        }

        private void MakeQuad(string name, Vector2 centre, Vector2 size, Color color, int order)
        {
            SpriteRenderer sr = MakeSprite(name, SpriteFactory.Solid, color, order);
            sr.transform.SetParent(_static, false);
            sr.transform.localScale = new Vector3(size.x, size.y, 1f);
            sr.transform.localPosition = new Vector3(centre.x, centre.y, 0f);
        }

        private SpriteRenderer MakeSprite(string name, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
