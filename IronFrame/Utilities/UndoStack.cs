using System;
using System.Collections.Generic;

namespace IronFrame.Utilities
{
    internal class UndoStack
    {
        private readonly Stack<Action> undoActions = new Stack<Action>();

        public void Push(Action undoAction)
        {
            undoActions.Push(undoAction);
        }

        public void UndoAll()
        {
            var exceptions = new List<Exception>();

            foreach (var undo in undoActions)
            {
                try
                {
                    undo();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Some of the undo actions failed.", exceptions);
            }
        }
    }
}