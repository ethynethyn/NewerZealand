using System.Collections.Generic;
using UnityEngine;

namespace UIDraw
{
    /// <summary>
    /// Handles undo functionality for drawing operations on textures
    /// </summary>
    public class DrawingUndoSystem
    {
        private Stack<Color32[]> undoStack = new Stack<Color32[]>();
        private Color32[] textureBeforeStroke;
        private int maxUndoSteps;

        /// <summary>
        /// Initialize the undo system with a maximum number of undo steps
        /// </summary>
        /// <param name="maxSteps">Maximum number of undo actions to store</param>
        public DrawingUndoSystem(int maxSteps = 20)
        {
            maxUndoSteps = maxSteps;
        }

        /// <summary>
        /// Saves the current texture state before starting a new drawing stroke
        /// </summary>
        /// <param name="texture">The texture to save the state from</param>
        public void SaveTextureState(Texture2D texture)
        {
            textureBeforeStroke = texture.GetPixels32();
        }

        /// <summary>
        /// Pushes the saved texture state to the undo stack after completing a stroke
        /// </summary>
        public void PushUndoState()
        {
            if (textureBeforeStroke != null)
            {
                undoStack.Push(textureBeforeStroke);
                
                // Limit the undo stack size
                while (undoStack.Count > maxUndoSteps)
                {
                    undoStack.Pop();
                }
                
                textureBeforeStroke = null;
            }
        }

        /// <summary>
        /// Undoes the last drawing action
        /// </summary>
        /// <param name="texture">The texture to apply the undo to</param>
        /// <returns>True if undo was performed, false if no undo available</returns>
        public bool Undo(Texture2D texture)
        {
            if (undoStack.Count > 0)
            {
                Color32[] previousState = undoStack.Pop();
                texture.SetPixels32(previousState);
                texture.Apply();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Clears the undo history
        /// </summary>
        public void ClearUndoHistory()
        {
            undoStack.Clear();
            textureBeforeStroke = null;
        }

        /// <summary>
        /// Returns true if there are actions that can be undone
        /// </summary>
        public bool CanUndo()
        {
            return undoStack.Count > 0;
        }

        /// <summary>
        /// Returns the number of available undo steps
        /// </summary>
        public int GetUndoCount()
        {
            return undoStack.Count;
        }

        /// <summary>
        /// Sets the maximum number of undo steps
        /// </summary>
        /// <param name="maxSteps">New maximum number of undo steps</param>
        public void SetMaxUndoSteps(int maxSteps)
        {
            maxUndoSteps = maxSteps;
            
            // Trim the stack if it's now too large
            while (undoStack.Count > maxUndoSteps)
            {
                undoStack.Pop();
            }
        }

        /// <summary>
        /// Gets the current maximum undo steps setting
        /// </summary>
        public int GetMaxUndoSteps()
        {
            return maxUndoSteps;
        }
    }
}