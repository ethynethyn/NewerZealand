using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UIDraw
{
    /// <summary>
    /// Handles input processing for drawing operations
    /// </summary>
    public class DrawingInputHandler
    {
        private RectTransform rectTransform;
        private Camera uiCamera;
        private int textureWidth;
        private int textureHeight;
        private bool isDrawing = false;

        // Events
        public System.Action<Vector2> OnDrawingStart;
        public System.Action<Vector2> OnDrawingContinue;
        public System.Action OnDrawingEnd;

        /// <summary>
        /// Initialize the input handler
        /// </summary>
        public DrawingInputHandler(RectTransform rectTransform, Camera uiCamera, int textureWidth, int textureHeight)
        {
            this.rectTransform = rectTransform;
            this.uiCamera = uiCamera;
            this.textureWidth = textureWidth;
            this.textureHeight = textureHeight;
        }

        /// <summary>
        /// Update method to handle stylus input
        /// </summary>
        public void Update()
        {
            // Check if stylus is available
            if (Pen.current != null)
            {
                // Is pen touching the screen?
                bool touching = Pen.current.tip.isPressed;

                // Only process if pen is touching
                if (touching)
                {
                    // Position of the pen (screen coordinates)
                    Vector2 screenPos = Pen.current.position.ReadValue();

                    // Convert screen position to pixel coordinates
                    Vector2 pixelPos = GetPixelPosition(screenPos);

                    // Pressure
                    float pressure = Pen.current.pressure.ReadValue();

                    // Only draw if we have valid pixel coordinates
                    if (pixelPos.x >= 0 && pixelPos.y >= 0)
                    {
                        if (!isDrawing)
                        {
                            // Start drawing
                            isDrawing = true;
                            OnDrawingStart?.Invoke(pixelPos);
                        }
                        else
                        {
                            // Continue drawing
                            OnDrawingContinue?.Invoke(pixelPos);
                        }
                    }

                }
                // do not add here an "else" clause to end drawing because the PointerUp event will handle it
            }
        }

        /// <summary>
        /// Handle pointer down events
        /// </summary>
        public void HandlePointerDown(PointerEventData eventData)
        {
            isDrawing = true;

            Vector2 pixelPos = GetPixelPosition(eventData);

            if (pixelPos.x >= 0 && pixelPos.y >= 0)
            {
                OnDrawingStart?.Invoke(pixelPos);
            }
        }

        /// <summary>
        /// Handle pointer up events
        /// </summary>
        public void HandlePointerUp(PointerEventData eventData)
        {
            isDrawing = false;
            OnDrawingEnd?.Invoke();
        }

        /// <summary>
        /// Handle drag events
        /// </summary>
        public void HandleDrag(PointerEventData eventData)
        {
            if (!isDrawing) return;
            
            Vector2 pixelPos = GetPixelPosition(eventData);
            
            if (pixelPos.x >= 0 && pixelPos.y >= 0)
            {
                OnDrawingContinue?.Invoke(pixelPos);
            }
        }

        /// <summary>
        /// Check if currently drawing
        /// </summary>
        public bool IsDrawing()
        {
            return isDrawing;
        }

        //////////////////////////////////////////////////////////////////////////////
        // COORDINATE CONVERSION

        private Vector2 GetPixelPosition(PointerEventData eventData)
        {
            return GetPixelPosition(eventData.position);
        }

        private Vector2 GetPixelPosition(Vector2 pos)
        {
            Vector2 localPoint;
            bool isValid = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                pos,
                uiCamera,
                out localPoint
            );

            if (!isValid) return new Vector2(-1, -1);

            // Get the rect dimensions
            Rect rect = rectTransform.rect;

            // Convert local point to normalized coordinates (0-1)
            float normalizedX = (localPoint.x + rect.width * 0.5f) / rect.width;
            float normalizedY = (localPoint.y + rect.height * 0.5f) / rect.height;

            // Clamp to 0-1 range
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);

            // Convert to pixel coordinates - NO Y-FLIP for UI consistency
            int pixelX = Mathf.RoundToInt(normalizedX * (textureWidth - 1));
            int pixelY = Mathf.RoundToInt(normalizedY * (textureHeight - 1));

            // Final bounds check
            if (pixelX < 0 || pixelX >= textureWidth || pixelY < 0 || pixelY >= textureHeight)
                return new Vector2(-1, -1);

            return new Vector2(pixelX, pixelY);
        }
    }
}