using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CharacterCounter
{
    public struct TextState
    {
        public TextState(int cursorLocation, int selectionLength, string text)
        {   
            CursorLocation = cursorLocation;
            SelectionLength = selectionLength;
            Text = text;
        }

        public readonly int CursorLocation;
        public readonly int SelectionLength;
        public readonly string Text;
    }

    public partial class Form1 : Form
    {
        private Stack<TextState> undoStack = new Stack<TextState>();
        private Stack<TextState> redoStack = new Stack<TextState>();

        public Form1()
        {
            InitializeComponent();
            ResizeFormItems(this); //set initial sizes programmatically

            this.Closed += formClosing;
        }

        
        private void formClosing(object sender, EventArgs e)
        {
            //save data to the clipboard
            Clipboard.SetText(txtInput.Text);
        }

        private void txtInput_TextChanged(object sender, EventArgs e)
        {
            void handleUndoStackUpdate()
            {
                if (undoStack.Count == 0 || undoStack.Peek().Text != txtInput.Text) //don't save the same selectedText twice
                {
                    var state = new TextState(txtInput.SelectionStart,txtInput.SelectionLength,txtInput.Text);
                    undoStack.Push(state); //push changes onto the stack (will obviously consume RAM)
                }

                txtInput.ClearUndo(); //we don't want the built-in undo
            }

            handleUndoStackUpdate();
            lblCharacterCount.Text = txtInput.Text.Length.ToString();
            
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            ResizeFormItems((Control)sender);
        }

        private void ResizeFormItems(Control form)
        {
            lblCharacterCountName.Top = form.Height - 60; //always 60 from the bottom
            lblCharacterCount.Top = lblCharacterCountName.Top; //same height

            lblCharacterCount.Left = form.Width - 60; //30 from the edge
            lblCharacterCountName.Left = lblCharacterCount.Left - 120; //always an extra 120 pixels back

            txtInput.Width = ((Control)form).Width - 39; //So that 13 from the edge ??
            txtInput.Height = ((Control)form).Height - 80; //so that using as much space as possible
        }

        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            TextState currentTextState;

            switch (keys)
            {
                case Keys.Control | Keys.Shift | Keys.L: //lowercase words
                    currentTextState = new TextState(txtInput.SelectionStart, txtInput.SelectionLength, txtInput.Text);
                    //toggle to upper

                    if (txtInput.SelectedText == txtInput.SelectedText.ToLower()) //if already lower
                    {
                        return true; //no-op
                    }
                    else
                    {
                        SetTextState(txtInput, currentTextState, txtInput.SelectedText?.ToLower());
                    }

                    break;
                case Keys.Control | Keys.Shift | Keys.U: //uppercase words
                    currentTextState = new TextState(txtInput.SelectionStart,txtInput.SelectionLength,txtInput.Text);
                    //toggle to upper

                    if (txtInput.SelectedText == txtInput.SelectedText.ToUpper()) //if already upper
                    {
                        return true; //no-op
                    }
                    else
                    {
                        SetTextState(txtInput, currentTextState, txtInput.SelectedText?.ToUpper());
                    }

                    break;
                case Keys.Control | Keys.Shift | Keys.T: //title case words
                case Keys.Control | Keys.Shift | Keys.OemQuestion: //title case words
                    currentTextState = new TextState(txtInput.SelectionStart, txtInput.SelectionLength, txtInput.Text);

                    SetTextState(txtInput,currentTextState, 
                        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                            txtInput.SelectedText.ToLower()) //must first convert to ToLower because ToTitleCase ignores words that are all uppercase (see: https://docs.microsoft.com/en-us/dotnet/api/system.globalization.textinfo.totitlecase?view=netframework-4.7.2&f1url=%3FappId%3DDev16IDEF1%26l%3DEN-US%26k%3Dk(System.Globalization.TextInfo.ToTitleCase);k(TargetFrameworkMoniker-.NETFramework,Version%253Dv4.7.2);k(DevLang-csharp)%26rd%3Dtrue)
                        );
                    break;
                case Keys.Control | Keys.Z: //undo

                    if (undoStack.Count == 0) //nothing to do
                    {
                        break;
                    }

                    //let's save the current selectedText for redo
                    currentTextState = undoStack.Pop(); //because we save after every change, the current selectedText is always at the top of the stack
                    redoStack.Push(currentTextState);

                    if (undoStack.Count == 0) //the base case, when we hit the root
                    {
                        txtInput.Text = String.Empty; //return to empty string (our initial state)
                    }
                    else //otherwise, let's set whatever we popped off as the selectedText
                    {
                        /* Now, pop off the next item -- which is the selectedText
                         immediately PRIOR to the current selectedText -- 
                         and restore it*/
                        var oldText = undoStack.Pop(); //the second pop has the data we actually want
                        SetTextState(txtInput,oldText);
                    }
                    break;
                case Keys.Control | Keys.Shift | Keys.Z:
                case Keys.Control | Keys.Y: //redo
                    if (redoStack.Count == 0)
                    {
                        break; //nothing to do
                    }

                    /* In the case of re-do, we don't need to save any undo selectedText.
                       Because, rememeber, every time the selectedText box changes we save
                       the undo selectedText. So the undo selectedText was saved by whatever operation
                       last changed the textbox selectedText. */

                    //pop off the old selectedText and restore it
                    var futureText = redoStack.Pop();
                    SetTextState(txtInput,futureText);

                    break;
            }
            return base.ProcessCmdKey(ref message, keys);
        }

        /// <summary>
        /// Set the selectedText control to a new state. Set SelectedText to <see cref="selectedText"/> if selectedText is not null.
        /// </summary>
        /// <param name="txtControl"></param>
        /// <param name="textState"></param>
        /// <param name="selectedText">
        private void SetTextState(TextBox txtControl, TextState textState, string selectedText = null)
        {
            txtControl.Text = textState.Text;

            if (selectedText != null)
            {
                txtControl.SelectedText = selectedText;
            }

            //we want the same selectedText to be selected after our change
            txtControl.SelectionStart = textState.CursorLocation;
            txtControl.SelectionLength = textState.SelectionLength;
            txtControl.ScrollToCaret();
        }
    }
}
