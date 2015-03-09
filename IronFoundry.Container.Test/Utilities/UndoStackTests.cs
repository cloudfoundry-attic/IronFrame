using System;
using System.Collections.Generic;
using Xunit;

namespace IronFoundry.Container.Utilities
{
    public class UndoStackTests
    {
        [Fact]
        public void UndoRunInReverseOrder()
        {
            List<string> undosRun = new List<string>();

            UndoStack undo = new UndoStack();
            undo.Push(() => undosRun.Add("first"));
            undo.Push(() => undosRun.Add("second"));
            undo.Push(() => undosRun.Add("third"));

            undo.UndoAll();

            Assert.Equal(3, undosRun.Count);
            Assert.Equal("third", undosRun[0]);
            Assert.Equal("second", undosRun[1]);
            Assert.Equal("first", undosRun[2]);
        }

        [Fact]
        public void EveryUndoIsRun()
        {
            UndoStack undo = new UndoStack();

            bool firstUndo = false;
            bool thirdUndo = false;

            undo.Push(() => { firstUndo = true; });
            undo.Push(() => { throw new InvalidOperationException(); });
            undo.Push(() => { thirdUndo = true;  });

            try
            {
                undo.UndoAll();
                Assert.True(false, "Expected an Aggregate exception to be thrown.");
            }
            catch (AggregateException)
            {
            }

            Assert.Equal(true, firstUndo);
            Assert.Equal(true, thirdUndo);
        }

        [Fact]
        public void WhenUndoThrows_ItThrowsAggregate()
        {
            UndoStack undo = new UndoStack();

            undo.Push(() => { throw new ArgumentException(); });
            undo.Push(() => { throw new InvalidOperationException(); });
            undo.Push(() => { });

            Action undoAction = () => undo.UndoAll();
            Assert.Throws<AggregateException>(undoAction);

            try
            {
                undoAction();
            }
            catch (AggregateException ex)
            {
                Assert.Equal(2, ex.InnerExceptions.Count);
                Assert.Equal(typeof(InvalidOperationException), ex.InnerExceptions[0].GetType());
                Assert.Equal(typeof(ArgumentException), ex.InnerExceptions[1].GetType());
            }
        }
    }
}
