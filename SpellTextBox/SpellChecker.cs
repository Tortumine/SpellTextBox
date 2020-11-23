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
    public class SpellChecker : INotifyPropertyChanged
    {
        private SpellTextBox box;
        private Hunspell hunSpell;
        private HashSet<Word> words;
        private ObservableCollection<Word> misspelledWords;
        private ObservableCollection<Word> suggestedWords;
        private HashSet<string> ignoredWords;
        private Word selectedMisspelledWord;
        private Word selectedSuggestedWord;

        public SpellChecker(Hunspell HunSpell, SpellTextBox Parent)
        {
            hunSpell = HunSpell; 
            box = Parent;
            Words = new HashSet<Word>();
            MisspelledWords = new ObservableCollection<Word>();
            IgnoredWords = new HashSet<string>();
            SuggestedWords = new ObservableCollection<Word>();
        }

        public void Dispose()
        {
            if (hunSpell != null)
                hunSpell.Dispose();
        }

        public HashSet<Word> Words
        {
            get { return words; }
            set
            {
                words = value;
                OnPropertyChanged("Words");
            }
        }

        public ObservableCollection<Word> MisspelledWords
        {
            get { return misspelledWords; }
            set
            {
                misspelledWords = value;
                OnPropertyChanged("MisspelledWords");
                if (SelectedMisspelledWord != null)
                    SelectedMisspelledWord = null;
            }
        }

        public ObservableCollection<MenuAction> MenuActions
        {
            get
            {
                List<MenuAction> commands = SuggestedWords.Select(w => new MenuAction()
                {
                    Name = w.Text,
                    Command = new DelegateCommand(
                        delegate
                        {
                            box.ReplaceSelectedWord(w);
                        })
                }).ToList();

                if (commands.Count == 0)
                {
                    commands.Add(new MenuAction()
                    {
                        Name = StringResources.Copy,
                        Command = ApplicationCommands.Copy
                    });
                    commands.Add(new MenuAction()
                    {
                        Name = StringResources.Cut,
                        Command = ApplicationCommands.Cut
                    });
                    commands.Add(new MenuAction()
                    {
                        Name = StringResources.Paste,
                        Command = ApplicationCommands.Paste
                    });
                }
                else
                {
                    commands.Add(new MenuAction()
                    {
                        Name = StringResources.AddCustom,
                        Command = new DelegateCommand(
                            delegate
                            {
                                SaveToCustomDictionary(SelectedMisspelledWord.Text);

                                box.FireTextChangeEvent();
                            })
                    });
                }

                return new ObservableCollection<MenuAction>(commands);
            }
        }

        public ObservableCollection<Word> SuggestedWords
        {
            get { return suggestedWords; }
            set
            {
                suggestedWords = value;
                OnPropertyChanged("SuggestedWords");
            }
        }

        public HashSet<string> IgnoredWords
        {
            get { return ignoredWords; }
            set
            {
                ignoredWords = value;
                OnPropertyChanged("IgnoredWords");
            }
        }

        public Word SelectedMisspelledWord
        {
            get { return selectedMisspelledWord; }
            set
            {
                selectedMisspelledWord = value;
                LoadSuggestions(value);
                OnPropertyChanged("SelectedMisspelledWord");
                OnPropertyChanged("IsReplaceEnabled");
            }
        }

        public Word SelectedSuggestedWord
        {
            get { return selectedSuggestedWord; }
            set
            {
                selectedSuggestedWord = value;
                OnPropertyChanged("SelectedSuggestedWord");
                OnPropertyChanged("IsReplaceEnabled");
            }
        }

        public void LoadSuggestions(Word misspelledWord)
        {
            if (misspelledWord != null)
            {
                SuggestedWords = new ObservableCollection<Word>(hunSpell.Suggest(misspelledWord.Text).Select(s => new Word(s, misspelledWord.Index, misspelledWord.LineIndex)));
                if (SuggestedWords.Count == 0) SuggestedWords = new ObservableCollection<Word> { new Word(StringResources.NoSuggestions, 0,0) };
            }
            else
            {
                SuggestedWords = new ObservableCollection<Word>();
            }
            OnPropertyChanged("SuggestedWords");
        }

        public void ClearLists()
        {
            Words.Clear();
            MisspelledWords.Clear();
        }

        public void CheckSpelling(SpellTextBox textBox)
        {
            if (box.IsSpellCheckEnabled)
            {
                ClearLists();

                int index =0;
                for (int lineIndex = 0; lineIndex < textBox.LineCount; lineIndex++)
                {
                    var matches = Regex.Matches(textBox.GetLineText(lineIndex), @"\w+[^\s]*\w+|\w");
                    var matchesString = matches.Cast<Match>().Select(match => match.Value).ToList();

                    for (var i = 0; i < matches.Count; i++)
                    {
                        Match match = matches[i];
                        if (!IgnoredWords.Contains(match.Value) && !hunSpell.Spell(match.Value))
                        {
                            MisspelledWords.Add(new Word(match.Value, match.Index, lineIndex));
                        }
                    }
                }

                OnPropertyChanged("MisspelledWords");
                OnPropertyChanged("IgnoredWords");
            }
        }

        public void LoadCustomDictionary()
        {
            string[] strings = File.ReadAllLines(box.CustomDictionaryPath);
            foreach (var str in strings)
            {
                hunSpell.Add(str);
            }
        }

        public void SaveToCustomDictionary(string word)
        {
            File.AppendAllText(box.CustomDictionaryPath, $@"{word.ToLower()}{Environment.NewLine}");
            hunSpell.Add(word);
            IgnoredWords.Add(word);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;
            if (handler != null) this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
