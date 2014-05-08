//-----------------------------------------------------------------------
// <copyright file="UndoManager.cs" company="Lovett Software">
//   (c) Lovett Software.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace TimeKeeper
{
    public class UndoManager
    {
        ObservableCollection<Command> history = new ObservableCollection<Command>();
        int position;

        public ObservableCollection<Command> History { get { return history; } }

        public Command PeekUndo() 
        {
            if (position > 0) return history[position-1];
            return null;
        }
        
        public Command PeekRedo() 
        {
            if (position < history.Count) return history[position];
            return null;
        }

        public event EventHandler Changed;

        void OnChanged()
        {
            if (Changed != null) Changed(this,null);
        }

        public void Add(Command command)
        {
            if (command.Do())
            {
                // truncate the undo stack at this point.
                while (position < history.Count)
                {
                    history.RemoveAt(position);
                }
                history.Add(command);
                position++;
                OnChanged();
            }
        }

        public bool CanUndo
        {
            get { return position > 0; }
        }

        public void Undo()
        {
            if (position > 0)
            {
                position--;
                history[position].Undo();
                OnChanged();
            }
        }

        public bool CanRedo 
        {
            get { return position < history.Count; }
        }

        public void Redo()
        {
            if (position < history.Count) 
            {
                history[position].Redo();
                position++;
                OnChanged();
            }
        }

    }
}
