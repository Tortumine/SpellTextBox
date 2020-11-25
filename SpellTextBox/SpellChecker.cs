using NHunspell;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Input;
using System;
using System.Collections.Specialized;
using System.IO;

namespace SpellTextBox
{
    public class SpellChecker : IDisposable
    {
        private Hunspell _hunSpell;
        private HashSet<string> ignoredWords;
        private string _customDictionaryPath;

        public EventArgs e = null;
        public event IgnoredWordsChangedHandler IgnoredWordsChanged;
        public delegate void IgnoredWordsChangedHandler(SpellChecker m, EventArgs e);
        
        public SpellChecker(Hunspell hunSpell,string customDictionaryPath = null)
        {
            _hunSpell = hunSpell; 
            IgnoredWords = new HashSet<string>();
            if (String.IsNullOrEmpty(customDictionaryPath))
            {
                _customDictionaryPath = "Dictionaries\\CustomDictionary.txt";

            }
            LoadCustomDictionary();
        }

        public void Dispose()
        {
            _hunSpell?.Dispose();
        }

        
        public HashSet<string> IgnoredWords
        {
            get { return ignoredWords; }
            set
            {
                ignoredWords = value;
                IgnoredWordsChanged?.Invoke(this, e);
            }
        }

        public void AddToIgnoredWords(string word)
        {
            ignoredWords.Add(word);
            IgnoredWordsChanged?.Invoke(this, e);
        }

        public ICollection<Word> GetSuggestions(Word misspelledWord)
        {
            ObservableCollection<Word> ret = new ObservableCollection<Word> { new Word(StringResources.NoSuggestions, 0, 0) };

            if (misspelledWord == null)
                return new ObservableCollection<Word> { new Word(StringResources.NoSuggestions, 0, 0) };
            
            ret = new ObservableCollection<Word>(_hunSpell.Suggest(misspelledWord.Text).Select(s => new Word(s, misspelledWord.Index, misspelledWord.LineIndex)));
            if (ret.Count == 0)
                ret = new ObservableCollection<Word> { new Word(StringResources.NoSuggestions, 0, 0) };
            return ret;
        }

        public ObservableCollection<Word> CheckSpelling(SpellTextBox textBox)
        {
            ObservableCollection<Word> ret = new ObservableCollection<Word>();
            for (int lineIndex = 0; lineIndex < textBox.LineCount; lineIndex++)
            {
                var matches = Regex.Matches(textBox.GetLineText(lineIndex), @"\w+[^\s]*\w+|\w");

                for (var i = 0; i < matches.Count; i++)
                {
                    Match match = matches[i];
                    if (!IgnoredWords.Contains(match.Value) && !_hunSpell.Spell(match.Value) && !Regex.IsMatch(match.Value, "^(?:(?:31(\\/|-|\\.)(?:0?[13578]|1[02]))\\1|(?:(?:29|30)(\\/|-|\\.)(?:0?[1,3-9]|1[0-2])\\2))(?:(?:1[6-9]|[2-9]\\d)?\\d{2})$|^(?:29(\\/|-|\\.)0?2\\3(?:(?:(?:1[6-9]|[2-9]\\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))$|^(?:0?[1-9]|1\\d|2[0-8])(\\/|-|\\.)(?:(?:0?[1-9])|(?:1[0-2]))\\4(?:(?:1[6-9]|[2-9]\\d)?\\d{2})$"))
                    {
                        ret.Add(new Word(match.Value, match.Index, lineIndex));
                    }
                }
            }
            return ret;
        }

        public IEnumerable<Word> CheckSpelling(List<Word> words)
        {
            List<Word> ret = new List<Word>();
            for (var i = 0; i < words.Count(); i++)
            {
                Word word = words[i];
                if (!IgnoredWords.Contains(word.Text) && !_hunSpell.Spell(word.Text) && !Regex.IsMatch(word.Text, "^(?:(?:31(\\/|-|\\.)(?:0?[13578]|1[02]))\\1|(?:(?:29|30)(\\/|-|\\.)(?:0?[1,3-9]|1[0-2])\\2))(?:(?:1[6-9]|[2-9]\\d)?\\d{2})$|^(?:29(\\/|-|\\.)0?2\\3(?:(?:(?:1[6-9]|[2-9]\\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))$|^(?:0?[1-9]|1\\d|2[0-8])(\\/|-|\\.)(?:(?:0?[1-9])|(?:1[0-2]))\\4(?:(?:1[6-9]|[2-9]\\d)?\\d{2})$"))
                {
                    ret.Add(new Word(word.Text, word.Index, word.LineIndex));
                }
            }

            return ret;
        }

        private void LoadCustomDictionary()
        {
            string[] strings = File.ReadAllLines(_customDictionaryPath);
            foreach (var str in strings)
            {
                _hunSpell.Add(str);
            }
        }

        public void SaveToCustomDictionary(string word)
        {
            File.AppendAllText(_customDictionaryPath, $@"{word.ToLower()}{Environment.NewLine}");
            _hunSpell.Add(word);
            IgnoredWords.Add(word);
        }
    }
}
