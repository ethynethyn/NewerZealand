using System.Collections.Generic;
using UnityEngine;

namespace UIDraw
{
    /// <summary>
    /// Contains all brush types and drawing logic for the UIDrawable system
    /// </summary>
    public class DrawingBrushes
    {
        private Texture2D drawableTexture;
        private Color32[] currentColors;
        
        // Drawing settings
        public Color PenColor { get; set; } = Color.black;
        public int PenWidth { get; set; } = 3;
        public int EraserWidth { get; set; } = 3;
        public Color ResetColor { get; set; } = new Color(0, 0, 0, 0);
        public bool AntiAliasing { get; set; } = false;
        
        // Texture properties
        public int TextureWidth { get; private set; }
        public int TextureHeight { get; private set; }
        
        // Previous position for smooth drawing
        private Vector2 previousDragPosition;

        public delegate void BrushFunction(Vector2 pixelPosition);
        public BrushFunction CurrentBrush { get; set; }

        /// <summary>
        /// Initialize the brush system with a texture
        /// </summary>
        public DrawingBrushes(Texture2D texture)
        {
            drawableTexture = texture;
            TextureWidth = texture.width;
            TextureHeight = texture.height;
            CurrentBrush = PenBrush;
        }

        /// <summary>
        /// Sets the previous drag position (used for stroke continuity)
        /// </summary>
        public void SetPreviousDragPosition(Vector2 position)
        {
            previousDragPosition = position;
        }

        /// <summary>
        /// Resets the previous drag position
        /// </summary>
        public void ResetPreviousDragPosition()
        {
            previousDragPosition = Vector2.zero;
        }

        //////////////////////////////////////////////////////////////////////////////
        // BRUSH TYPES

        /// <summary>
        /// Default pen brush for UI drawing
        /// </summary>
        public void PenBrush(Vector2 pixelPos)
        {
            currentColors = drawableTexture.GetPixels32();

            if (previousDragPosition == Vector2.zero)
            {
                MarkPixelsToColour(pixelPos, PenWidth, PenColor);
            }
            else
            {
                ColourBetween(previousDragPosition, pixelPos, PenWidth, PenColor);
            }
            ApplyMarkedPixelChanges();
            previousDragPosition = pixelPos;
        }

        /// <summary>
        /// Fill brush for UI drawing (flood fill)
        /// </summary>
        public void FillBrush(Vector2 pixelPosition)
        {
            int x = (int)pixelPosition.x;
            int y = (int)pixelPosition.y;

            if (x < 0 || x >= TextureWidth || y < 0 || y >= TextureHeight) return;

            Color targetColor = drawableTexture.GetPixel(x, y);
            if (targetColor == PenColor) return;

            Queue<Vector2> pixels = new Queue<Vector2>();
            pixels.Enqueue(new Vector2(x, y));

            while (pixels.Count > 0)
            {
                Vector2 currentPixel = pixels.Dequeue();
                int cx = (int)currentPixel.x;
                int cy = (int)currentPixel.y;

                if (cx < 0 || cx >= TextureWidth || cy < 0 || cy >= TextureHeight) continue;
                if (drawableTexture.GetPixel(cx, cy) != targetColor) continue;

                drawableTexture.SetPixel(cx, cy, PenColor);

                pixels.Enqueue(new Vector2(cx + 1, cy));
                pixels.Enqueue(new Vector2(cx - 1, cy));
                pixels.Enqueue(new Vector2(cx, cy + 1));
                pixels.Enqueue(new Vector2(cx, cy - 1));
            }

            drawableTexture.Apply();
        }

        /// <summary>
        /// Eraser brush for UI drawing
        /// </summary>
        public void EraserBrush(Vector2 pixelPos)
        {
            currentColors = drawableTexture.GetPixels32();

            if (previousDragPosition == Vector2.zero)
            {
                MarkPixelsToColour(pixelPos, EraserWidth, ResetColor);
            }
            else
            {
                ColourBetween(previousDragPosition, pixelPos, EraserWidth, ResetColor);
            }
            ApplyMarkedPixelChanges();
            previousDragPosition = pixelPos;
        }

        //////////////////////////////////////////////////////////////////////////////
        // BRUSH SETTERS

        public void SetPenBrush()
        {
            CurrentBrush = PenBrush;
        }

        public void SetFillBrush()
        {
            CurrentBrush = FillBrush;
        }

        public void SetEraserBrush()
        {
            CurrentBrush = EraserBrush;
        }

        //////////////////////////////////////////////////////////////////////////////
        // DRAWING METHODS

        private void ColourBetween(Vector2 startPoint, Vector2 endPoint, int width, Color color)
        {
            float distance = Vector2.Distance(startPoint, endPoint);
            if (distance < 1f) return;

            Vector2 currentPosition = startPoint;
            float lerpSteps = 1f / distance;

            for (float lerp = 0; lerp <= 1; lerp += lerpSteps)
            {
                currentPosition = Vector2.Lerp(startPoint, endPoint, lerp);
                MarkPixelsToColour(currentPosition, width, color);
            }
        }

        private void MarkPixelsToColour(Vector2 centerPixel, int penThickness, Color colorOfPen)
        {
            float radius = penThickness + 0.5f; // Add 0.5 for better circle coverage
            int minX = Mathf.Max(0, (int)(centerPixel.x - radius));
            int maxX = Mathf.Min(TextureWidth - 1, (int)(centerPixel.x + radius));
            int minY = Mathf.Max(0, (int)(centerPixel.y - radius));
            int maxY = Mathf.Min(TextureHeight - 1, (int)(centerPixel.y + radius));

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // Calculate actual distance from center point (not pixel center)
                    float dx = x - centerPixel.x;
                    float dy = y - centerPixel.y;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    // Draw pixels within radius with soft edge for smoother circle
                    if (distance <= radius)
                    {
                        if (AntiAliasing && distance > radius - 1f)
                        {
                            // Anti-aliasing: blend edge pixels for smoother circle
                            float alpha = 1f - (distance - (radius - 1f));
                            Color blendedColor = BlendColors(GetCurrentPixelColor(x, y), colorOfPen, alpha);
                            MarkPixelToChange(x, y, blendedColor);
                        }
                        else
                        {
                            MarkPixelToChange(x, y, colorOfPen);
                        }
                    }
                }
            }
        }

        private Color GetCurrentPixelColor(int x, int y)
        {
            int arrayPos = y * TextureWidth + x;
            if (arrayPos >= currentColors.Length || arrayPos < 0) 
                return Color.clear;
            return currentColors[arrayPos];
        }

        private Color BlendColors(Color background, Color foreground, float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            return new Color(
                background.r * (1 - alpha) + foreground.r * alpha,
                background.g * (1 - alpha) + foreground.g * alpha,
                background.b * (1 - alpha) + foreground.b * alpha,
                background.a * (1 - alpha) + foreground.a * alpha
            );
        }

        private void MarkPixelToChange(int x, int y, Color color)
        {
            int arrayPos = y * TextureWidth + x;

            if (arrayPos >= currentColors.Length || arrayPos < 0) return;

            currentColors[arrayPos] = color;
        }

        private void ApplyMarkedPixelChanges()
        {
            drawableTexture.SetPixels32(currentColors);
            drawableTexture.Apply();
        }
    }
}