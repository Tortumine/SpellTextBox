using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpellTextBox
{
    public class Word
    {
        public int LineIndex { get; set; }

        public int Index { get; set; }

        public string Text { get; set; }

        public int Length
        {
            get { return Text.Length; }
        }

        public Word()
        {
        }

        public Word(string text, int index,int lineIndex)
        {
            Index = index;
            Text = text;
            LineIndex = lineIndex;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
