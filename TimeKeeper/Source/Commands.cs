//-----------------------------------------------------------------------
// <copyright file="Commands.cs" company="Lovett Software">
//   (c) Lovett Software.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeKeeper
{
    public abstract class Command
    {
        public abstract string Caption { get; }
        public abstract string Description { get; }
        public abstract bool Do();
        public abstract void Undo();
        public abstract void Redo();
    }

    public abstract class TaskModelCommand : Command
    {
        TaskModel model;

        protected TaskModelCommand(TaskModel model)
        {
            this.model = model;
        }

        public TaskModel Model { get { return this.model; } }
    }

    public class InsertCommand : TaskModelCommand
    {
        int position;
        TaskItem newItem;

        public InsertCommand(TaskModel model, int position)
            : base(model)
        {
            this.position = position; 
        }

        public override string Caption
        {
            get { return "Insert"; }
        }

        public override string Description
        {
            get { return "Insert a new task above the current one";  }
        }

        public override bool Do()
        {
            newItem = new TaskItem() { Model = this.Model, StartTime = DateTime.Now };
            if (this.position < this.Model.Tasks.Count)
            {
                TaskItem next = this.Model.Tasks[position];
                newItem.StartTime = next.StartTime;
            }
            this.Model.Tasks.Insert(this.position, newItem);
            return true;
        }

        public override void Undo()
        {
            this.Model.Tasks.RemoveAt(this.position);
        }

        public override void Redo()
        {
            this.Model.Tasks.Insert(this.position, newItem);
        }
    }


    public class DeleteCommand : TaskModelCommand
    {
        int position;
        TaskItem deleted;

        public DeleteCommand(TaskModel model, int position)
            : base(model)
        {
            this.position = position;
        }

        public override string Caption
        {
            get { return "Delete"; }
        }

        public override string Description
        {
            get { return "Delete the current task"; }
        }

        public override bool Do()
        {
            if (position > 0 && position < this.Model.Tasks.Count)
            {
                deleted = this.Model.Tasks[position];
                this.Model.Tasks.RemoveAt(this.position);
                // Fill the duration gap on previous task.
                if (position-1 > 0 && position < this.Model.Tasks.Count)
                {
                    TaskItem previous = this.Model.Tasks[position-1];
                    TaskItem next = this.Model.Tasks[position];
                    previous.Duration = next.StartTime - previous.StartTime;
                }
                return true;
            }
            return false;
        }

        public override void Undo()
        {
            if (deleted != null)
            {
                this.Model.Tasks.Insert(this.position, deleted);

                // Fix the duration on previous task to make room for undeleted task.
                if (position-1 > 0 )
                {
                    TaskItem previous = this.Model.Tasks[position - 1];
                    previous.Duration = deleted.StartTime - previous.StartTime;
                }
            }
        }

        public override void Redo()
        {
            Do();
        }
    }
}
