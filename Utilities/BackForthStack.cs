using System.Collections.Generic;

namespace Hani.Utilities
{
    internal sealed class BackForthStack
    {
        internal bool CanForth { get { return (forth.Count > 0); } }
        internal bool CanBack { get { return (back.Count > 0); } }

        private bool backFlagged = false;
        private Stack<string> back = new Stack<string>();
        private Stack<string> forth = new Stack<string>();

        internal BackForthStack()
        {
        }

        internal void Clear()
        {
            back.Clear();
            forth.Clear();
        }

        internal string PeekForth()
        {
            return CanForth ? forth.Peek() : string.Empty;
        }

        internal string PeekBack()
        {
            return CanBack ? back.Peek() : string.Empty;
        }

        internal string Forth()
        {
            string pop = CanForth ? forth.Pop() : string.Empty;
            return pop;
        }

        internal string Back()
        {
            backFlagged = true;
            string pop = CanBack ? back.Pop() : string.Empty;
            return pop;
        }

        internal void Save(string lastPath, string newPath)
        {
            if (lastPath.NullEmpty()) return;

            if (backFlagged) { forth.Push(lastPath); backFlagged = false; }
            else
            {
                if (newPath == PeekForth()) forth.Pop();
                back.Push(lastPath);
            }
        }
    }
}