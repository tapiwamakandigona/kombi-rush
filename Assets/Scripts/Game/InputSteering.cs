using UnityEngine;
using UnityEngine.EventSystems;

namespace KombiRush.Game
{
    /// <summary>
    /// One-thumb steering. Two habits are supported without a settings menu: tap the left or
    /// right half to shift one lane, or hold and slide to steer straight to a lane. Arrow keys
    /// and A/D work too, which is how the game is played in the editor.
    /// </summary>
    public sealed class InputSteering
    {
        private const float TapMaxSeconds = 0.25f;
        private const float DragThresholdFraction = 0.035f;   // of screen width

        private bool _pointerDown;
        private bool _dragging;
        private float _downTime;
        private float _downX;
        private bool _startedOverUi;

        public bool IsDragging => _dragging;

        /// <summary>Returns the lane the player is asking for this frame.</summary>
        public int Sample(int currentTarget, int laneCount)
        {
            int target = currentTarget;

            // --- keyboard (editor and any attached keyboard) ---
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) target--;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) target++;

            // --- pointer: touch on device, mouse in the editor ---
            bool down = false, held = false, up = false;
            float x = 0f;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                x = touch.position.x;
                down = touch.phase == TouchPhase.Began;
                held = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
                up = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            }
            else
            {
                x = Input.mousePosition.x;
                down = Input.GetMouseButtonDown(0);
                held = Input.GetMouseButton(0);
                up = Input.GetMouseButtonUp(0);
            }

            float width = Mathf.Max(1f, Screen.width);
            float dragThreshold = width * DragThresholdFraction;

            if (down)
            {
                _pointerDown = true;
                _dragging = false;
                _downTime = Time.unscaledTime;
                _downX = x;
                _startedOverUi = IsOverUi();
            }

            if (_pointerDown && held && !_startedOverUi)
            {
                if (!_dragging && Mathf.Abs(x - _downX) > dragThreshold) _dragging = true;
                if (_dragging) target = LaneUnderPointer(x, width, laneCount);
            }

            if (up)
            {
                if (_pointerDown && !_dragging && !_startedOverUi && Time.unscaledTime - _downTime <= TapMaxSeconds)
                    target += x < width * 0.5f ? -1 : 1;
                _pointerDown = false;
                _dragging = false;
                _startedOverUi = false;
            }

            return Mathf.Clamp(target, 0, laneCount - 1);
        }

        public void Reset()
        {
            _pointerDown = false;
            _dragging = false;
            _startedOverUi = false;
        }

        private static int LaneUnderPointer(float x, float width, int laneCount)
        {
            // the road occupies the middle 78% of the screen; steering uses the same span so the
            // thumb never has to reach the very edge of a big phone
            const float span = 0.78f;
            float low = (1f - span) * 0.5f;
            float t = Mathf.Clamp01((x / width - low) / span);
            return Mathf.Clamp(Mathf.RoundToInt(t * (laneCount - 1)), 0, laneCount - 1);
        }

        private static bool IsOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
