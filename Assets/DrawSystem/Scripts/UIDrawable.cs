using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIDraw
{
    [RequireComponent(typeof(RawImage))]
    /// <summary>
    /// Main drawing canvas component for UI-based drawing
    /// 1. Attach this to a RawImage component in a Canvas
    /// 2. Set the texture width and height in the inspector
    /// 3. The RawImage will automatically handle the drawing area
    /// 4. Hold down left mouse to draw on this texture!
    /// </summary>
    public class UIDrawable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Drawing Settings")]
        public Color Pen_Colour = Color.black;
        public int Pen_Width = 3;
        public int Eraser_Width = 3;

        [Header("Canvas Settings")]
        public int textureWidth = 512;
        public int textureHeight = 512;
        public bool Reset_Canvas_On_Play = true;
        public Color Reset_Colour = new Color(0, 0, 0, 0);

        [Header("Reset Options")]
        public bool Reset_To_This_Texture_On_Play = false;
        public Texture2D reset_texture;

        [Header("Undo Settings")]
        public int maxUndoSteps = 20;

        // Core systems
        private DrawingBrushes brushSystem;
        private DrawingInputHandler inputHandler;
        private DrawingUndoSystem undoSystem;

        // UI Components
        private RawImage rawImage;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private Camera uiCamera;

        // Drawing variables
        private Texture2D drawable_texture;
        private Color[] clean_colours_array;

        //////////////////////////////////////////////////////////////////////////////
        // PUBLIC API

        /// <summary>
        /// Undoes the last drawing action
        /// </summary>
        public void Undo()
        {
            undoSystem.Undo(drawable_texture);
        }

        /// <summary>
        /// Clears the undo history
        /// </summary>
        public void ClearUndoHistory()
        {
            undoSystem.ClearUndoHistory();
        }

        /// <summary>
        /// Returns true if there are actions that can be undone
        /// </summary>
        public bool CanUndo()
        {
            return undoSystem.CanUndo();
        }

        /// <summary>
        /// Returns the number of available undo steps
        /// </summary>
        public int GetUndoCount()
        {
            return undoSystem.GetUndoCount();
        }

        /// <summary>
        /// Sets the current brush to pen
        /// </summary>
        public void SetPenBrush()
        {
            brushSystem.SetPenBrush();
        }

        /// <summary>
        /// Sets the current brush to fill
        /// </summary>
        public void SetFillBrush()
        {
            brushSystem.SetFillBrush();
        }

        /// <summary>
        /// Sets the current brush to eraser
        /// </summary>
        public void SetEraserBrush()
        {
            brushSystem.SetEraserBrush();
        }

        /// <summary>
        /// Resets the canvas to the default color
        /// </summary>
        public void ResetCanvas()
        {
            drawable_texture.SetPixels(clean_colours_array);
            drawable_texture.Apply();
            undoSystem.ClearUndoHistory();
        }

        /// <summary>
        /// Sets the pen color
        /// </summary>
        public void SetPenColor(Color newColor)
        {
            Pen_Colour = newColor;
            if (brushSystem != null)
                brushSystem.PenColor = newColor;
        }

        /// <summary>
        /// Sets the pen width
        /// </summary>
        public void SetPenWidth(int newWidth)
        {
            Pen_Width = Mathf.Max(1, newWidth);
            if (brushSystem != null)
                brushSystem.PenWidth = Pen_Width;
        }

        /// <summary>
        /// Gets the drawing texture
        /// </summary>
        public Texture2D GetTexture()
        {
            return drawable_texture;
        }

        /// <summary>
        /// Saves the texture as PNG
        /// </summary>
        public void SaveTexture(string filename)
        {
            byte[] data = drawable_texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(Application.persistentDataPath + "/" + filename + ".png", data);
        }

        //////////////////////////////////////////////////////////////////////////////
        // INPUT EVENT HANDLERS

        public void OnPointerDown(PointerEventData eventData)
        {
            inputHandler.HandlePointerDown(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            inputHandler.HandlePointerUp(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            inputHandler.HandleDrag(eventData);
        }

        //////////////////////////////////////////////////////////////////////////////
        // UNITY LIFECYCLE

        void Update()
        {
            inputHandler?.Update();
            SyncBrushSettings();
        }

        void Awake()
        {
            InitializeComponents();
            InitializeTexture();
            InitializeSystems();
            SetupInputCallbacks();
            
            if (Reset_Canvas_On_Play)
            {
                ResetCanvas();
            }
            else if (Reset_To_This_Texture_On_Play && reset_texture != null)
            {
                Graphics.CopyTexture(reset_texture, drawable_texture);
                drawable_texture.Apply();
            }
        }

        void OnValidate()
        {
            if (undoSystem != null)
            {
                undoSystem.SetMaxUndoSteps(maxUndoSteps);
            }
        }

        //////////////////////////////////////////////////////////////////////////////
        // INITIALIZATION

        private void InitializeComponents()
        {
            rawImage = GetComponent<RawImage>();
            rectTransform = GetComponent<RectTransform>();

            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    uiCamera = null;
                else
                    uiCamera = parentCanvas.worldCamera;
            }
        }

        private void InitializeTexture()
        {
            drawable_texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            drawable_texture.filterMode = FilterMode.Point;
            drawable_texture.wrapMode = TextureWrapMode.Clamp;

            rawImage.texture = drawable_texture;

            clean_colours_array = new Color[textureWidth * textureHeight];
            for (int i = 0; i < clean_colours_array.Length; i++)
                clean_colours_array[i] = Reset_Colour;

        }

        private void InitializeSystems()
        {
            // Initialize systems
            brushSystem = new DrawingBrushes(drawable_texture);
            inputHandler = new DrawingInputHandler(rectTransform, uiCamera, textureWidth, textureHeight);
            undoSystem = new DrawingUndoSystem(maxUndoSteps);

            // Sync initial settings
            SyncBrushSettings();
        }

        private void SetupInputCallbacks()
        {
            inputHandler.OnDrawingStart += OnDrawingStart;
            inputHandler.OnDrawingContinue += OnDrawingContinue;
            inputHandler.OnDrawingEnd += OnDrawingEnd;
        }

        private void SyncBrushSettings()
        {
            if (brushSystem != null)
            {
                brushSystem.PenColor = Pen_Colour;
                brushSystem.PenWidth = Pen_Width;
                brushSystem.EraserWidth = Eraser_Width;
                brushSystem.ResetColor = Reset_Colour;
            }
        }

        //////////////////////////////////////////////////////////////////////////////
        // DRAWING CALLBACKS

        private void OnDrawingStart(Vector2 pixelPos)
        {
            undoSystem.SaveTextureState(drawable_texture);
            brushSystem.ResetPreviousDragPosition();
            brushSystem.CurrentBrush(pixelPos);
        }

        private void OnDrawingContinue(Vector2 pixelPos)
        {
            brushSystem.CurrentBrush(pixelPos);
        }

        private void OnDrawingEnd()
        {
            undoSystem.PushUndoState();
            brushSystem.ResetPreviousDragPosition();
        }
    }
}