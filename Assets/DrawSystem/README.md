# UIDraw - UI Drawing System for Unity

A comprehensive drawing system for Unity UI that provides intuitive drawing capabilities directly on Unity's Canvas components. Perfect for creating drawing applications, digital art tools, signature capture, annotation systems, and interactive whiteboards.

## ? Features

### ?? Drawing Tools
- **Pen Brush**: Smooth line drawing with adjustable width and color
- **Fill Tool**: Bucket fill for quick area coloring with flood fill algorithm
- **Eraser**: Clean erasing with adjustable width
- **Undo System**: Multi-level undo with configurable history depth (up to 20 steps)

### ??? Input Support
- **Mouse Drawing**: Traditional mouse input for desktop applications
- **Touch Support**: Multi-touch ready for mobile devices
- **Stylus Input**: Full stylus/pen support with pressure detection using Unity's Input System
- **Event-Driven**: Clean event system for custom input handling

### ?? UI Integration
- **Canvas-Based**: Seamlessly integrates with Unity's UI system
- **RawImage Component**: Built on Unity's RawImage for optimal performance
- **Responsive**: Automatically handles different screen resolutions and UI scaling
- **Event System**: Works with Unity's EventSystem for proper UI interaction

### ?? Customization
- **Texture Settings**: Configurable texture width/height (default 512x512)
- **Color Management**: Full RGBA color support including transparency
- **Brush Settings**: Adjustable pen and eraser widths
- **Reset Options**: Canvas clearing with custom background colors
- **Initialization**: Option to reset canvas on play or load from preset texture

## ?? Package Contents

```
Assets/DrawSystem/
??? UIDrawable.cs           # Main drawing component
??? DrawingBrushes.cs       # Brush implementations and drawing logic
??? DrawingInputHandler.cs  # Input processing and coordinate conversion
??? DrawingUndoSystem.cs    # Undo/redo functionality
??? README.md              # This documentation
```

## ?? Quick Start

### Basic Setup (2 minutes)

1. **Create a Canvas** in your scene if you don't have one
2. **Add a RawImage** component to a GameObject under the Canvas
3. **Attach the UIDrawable script** to the same GameObject
4. **Configure settings** in the inspector:
   - Set texture width/height (recommended: 512x512 or higher)
   - Choose pen color and width
   - Set eraser width
   - Configure reset color (default: transparent)

### Minimal Code Example

```csharp
using UIDraw;
using UnityEngine;

public class DrawingDemo : MonoBehaviour
{
    public UIDrawable drawable;
    
    void Start()
    {
        // Set drawing color
        drawable.SetPenColor(Color.red);
        
        // Set brush width
        drawable.SetPenWidth(5);
    }
    
    public void OnUndoButton()
    {
        drawable.Undo();
    }
    
    public void OnClearButton()
    {
        drawable.ResetCanvas();
    }
}
```

## ?? API Reference

### UIDrawable (Main Component)

#### Drawing Control
```csharp
// Brush selection
void SetPenBrush()          // Switch to pen tool
void SetFillBrush()         // Switch to fill tool  
void SetEraserBrush()       // Switch to eraser tool

// Settings
void SetPenColor(Color color)     // Change pen color
void SetPenWidth(int width)       // Change pen width (minimum 1)
```

#### Advanced Brush Settings
Access through the internal brush system:
```csharp
// Enable/disable anti-aliasing for smooth circular edges
drawable.brushSystem.AntiAliasing = true;  // Default: false
// Note: Keep false when using Fill Tool to avoid semi-transparent edge pixels
```

#### Undo System
```csharp
void Undo()                       // Undo last action
bool CanUndo()                    // Check if undo is available
int GetUndoCount()                // Get number of available undos
void ClearUndoHistory()           // Clear undo history
```

#### Canvas Management
```csharp
void ResetCanvas()                // Clear canvas to reset color
Texture2D GetTexture()            // Get drawing texture
void SaveTexture(string filename) // Save as PNG to persistent data
```

### Configuration Options

#### Inspector Settings
- **Pen_Colour**: Default drawing color
- **Pen_Width**: Default pen brush width (1-50)
- **Eraser_Width**: Default eraser width (1-50)
- **textureWidth/Height**: Canvas resolution (recommended: 512-2048)
- **Reset_Canvas_On_Play**: Auto-clear canvas when starting
- **Reset_Colour**: Background color for cleared canvas
- **maxUndoSteps**: Undo history depth (1-50)

## ?? Input Handling

### Supported Input Methods

1. **Mouse**: Left-click and drag to draw
2. **Touch**: Single finger touch and drag
3. **Stylus**: Pen input with pressure detection (requires Unity Input System)

### Custom Input Events

```csharp
// Access the input handler for custom behavior
var inputHandler = GetComponent<UIDrawable>().inputHandler;
inputHandler.OnDrawingStart += OnDrawStart;
inputHandler.OnDrawingContinue += OnDrawContinue;
inputHandler.OnDrawingEnd += OnDrawEnd;
```


```

### Color Palette Integration

```csharp
public class ColorPalette : MonoBehaviour
{
    public UIDrawable drawable;
    public Button[] colorButtons;
    public Color[] colors;
    
    void Start()
    {
        for (int i = 0; i < colorButtons.Length; i++)
        {
            int index = i; // Capture for closure
            colorButtons[i].onClick.AddListener(() => {
                drawable.SetPenColor(colors[index]);
            });
        }
    }
}
```

### Save/Load System

```csharp
public class DrawingSaveLoad : MonoBehaviour
{
    public UIDrawable drawable;
    
    public void SaveDrawing(string filename)
    {
        drawable.SaveTexture(filename);
        Debug.Log($"Saved to: {Application.persistentDataPath}/{filename}.png");
    }
    
    public void LoadDrawing(string filename)
    {
        string path = $"{Application.persistentDataPath}/{filename}.png";
        if (System.IO.File.Exists(path))
        {
            byte[] data = System.IO.File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(data);
            
            // Apply loaded texture to canvas
            Graphics.CopyTexture(texture, drawable.GetTexture());
            drawable.GetTexture().Apply();
        }
    }
}
```

## ?? Use Cases

### Drawing Applications
- **Digital Art**: Create simple drawing and painting apps
- **Children's Games**: Educational drawing games and activities
- **Signature Capture**: Digital signature collection for forms
- **Annotation Tools**: Mark up images and documents

### Interactive Systems
- **Whiteboards**: Collaborative drawing spaces
- **Mind Mapping**: Visual brainstorming tools
- **Game Mechanics**: Drawing-based puzzles and challenges
- **Prototyping**: Quick UI mockup tools

## ? Performance Tips

### Texture Resolution
- **512x512**: Good for simple drawings, mobile-friendly
- **1024x1024**: Balanced quality and performance
- **2048x2048**: High quality, desktop recommended

### Memory Management
```csharp
// Optimize undo system for memory
drawable.undoSystem.SetMaxUndoSteps(10); // Reduce from default 20

// Clear history when switching modes
drawable.ClearUndoHistory();
```

## ?? Troubleshooting

### Common Issues

**Drawing not working?**
- Ensure EventSystem is present in scene
- Check that RawImage has proper Raycast Target setting
- Verify Canvas render mode is compatible

**Poor performance?**
- Reduce texture resolution
- Limit undo history depth
- Use lower brush widths for smoother performance

**Coordinate issues?**
- Check Canvas Scaler settings
- Ensure RawImage anchoring is correct
- Verify camera settings for non-overlay canvases

### Debug Mode
Enable debug logging in `DrawingInputHandler.cs` by uncommenting debug lines:
```csharp
Debug.Log($"Pen Pos: {screenPos}, Pixel Pos: {pixelPos}");
```

## ?? Requirements

- **Unity Version**: 2021.3 LTS or higher
- **Dependencies**: 
  - Unity UI (com.unity.ugui)
  - Unity Input System (com.unity.inputsystem) - for stylus support
- **Platforms**: All Unity-supported platforms
- **Scripting Backend**: Mono and IL2CPP compatible

## ?? Support

For support, feature requests, or bug reports:
- Check the troubleshooting section above
- Review the example scenes and scripts
- Ensure you're using the latest Unity LTS version

## ?? License

This asset is provided under Unity Asset Store license terms.

---

**Version**: 1.0  
**Namespace**: UIDraw  
**Compatibility**: Unity 2021.3+

*Happy Drawing! ??*