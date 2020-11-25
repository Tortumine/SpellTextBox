using NHunspell;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Timers;

namespace SpellTextBox
{
    public class SpellTextBox : TextBox
    {
        public ObservableCollection<Word> misspelledWords = new ObservableCollection<Word>();
        public ObservableCollection<Word> suggestedWords = new ObservableCollection<Word>();
        private Word selectedMisspelledWord;

        public SpellChecker SpellChecker;

        AdornerLayer adornerLayer;
        RedUnderlineAdorner adorner;

        static Timer textChangedTimer = new System.Timers.Timer(500);
        ElapsedEventHandler textChangedTimerOnElapse;

        static SpellTextBox()
        {
            TextProperty.OverrideMetadata(typeof(SpellTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(TextPropertyChanged)));
        }

        public SpellTextBox() : base()
        {
            CreateTimer();

            this.ContextMenu = new ContextMenu();
            this.ContextMenu.Opened += OnContextMenuOpening;
            this.SizeChanged += OnTextBoxSizeChanged;

            Loaded += (s, e) =>
            {
                Initialize();
                if (Window.GetWindow(this) != null)
                    Window.GetWindow(this).Closing += (s1, e1) => Dispose();
            };
        }
        public SpellTextBox(SpellChecker spellChecker) : base()
        {
            CreateTimer();

            var cm = new ContextMenu();
            this.ContextMenu = cm;
            this.ContextMenu.Opened += OnContextMenuOpening;
            this.SizeChanged += OnTextBoxSizeChanged;

            Loaded += (s, e) =>
            {
                Initialize();
                if (Window.GetWindow(this) != null)
                    Window.GetWindow(this).Closing += (s1, e1) => Dispose();
            };

            if (spellChecker != null)
                this.SpellChecker = spellChecker;
        }

        public SpellChecker InitSpellChecker()
        {
            SpellChecker = SpellCheckerProvider.Instance.GetSpellChecker(DictionaryPath);
            return SpellChecker;
        }

        void CreateTimer()
        {
            textChangedTimer.AutoReset = false;
            textChangedTimerOnElapse = new ElapsedEventHandler(textChangedTimer_Elapsed);
            textChangedTimer.Elapsed += textChangedTimerOnElapse;

        }

        private static void TextPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((SpellTextBox)sender).IsSpellcheckCompleted = false;
            textChangedTimer.Stop();
            textChangedTimer.Start();
        }

        private void textChangedTimer_Elapsed(object sender,  System.Timers.ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(new System.Action(() => 
            {
                misspelledWords = SpellChecker.CheckSpelling(this);
                RaiseSpellcheckCompletedEvent();
            }));
        }

        #region SpellcheckCompleted Event

        public static readonly RoutedEvent SpellcheckCompletedEvent = EventManager.RegisterRoutedEvent("SpellcheckCompleted", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SpellTextBox));

        public event RoutedEventHandler SpellcheckCompleted
        {
            add { AddHandler(SpellcheckCompletedEvent, value); }
            remove { RemoveHandler(SpellcheckCompletedEvent, value); }
        }

        void RaiseSpellcheckCompletedEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(SpellTextBox.SpellcheckCompletedEvent);
            RaiseEvent(newEventArgs);
            IsSpellcheckCompleted = true;
        }

        public bool IsSpellcheckCompleted { get; set; }

        #endregion

        #region ResizeEvent

        protected void OnTextBoxSizeChanged(object sender, RoutedEventArgs e)
        {
            ((SpellTextBox)sender).IsSpellcheckCompleted = false;
            textChangedTimer.Stop();
            textChangedTimer.Start();
        }

        #endregion

        protected void OnContextMenuOpening(object sender, RoutedEventArgs e)
        {
            int lineIndex = GetLineIndexFromCharacterIndex(SelectionStart);
            int lineFirstCharIndex = GetCharacterIndexFromLineIndex(lineIndex);

            selectedMisspelledWord = misspelledWords.FirstOrDefault(w => w.LineIndex == GetLineIndexFromCharacterIndex(SelectionStart) && SelectionStart >= lineFirstCharIndex + w.Index && SelectionStart <= lineFirstCharIndex + w.Index + w.Length);

            var suggestedWords = SpellChecker.GetSuggestions(selectedMisspelledWord);
            ContextMenu.Items.Clear();
            
            foreach (Word word in suggestedWords)
            {
                MenuItem mi = new MenuItem();
                mi.Header = word.Text;
                mi.FontWeight = FontWeights.Bold;
                mi.Command = new DelegateCommand(
                    delegate
                    {
                        ReplaceSelectedWord(word);
                    });
                mi.CommandParameter = word;
                mi.CommandTarget = this;
                ContextMenu.Items.Add(mi);
            }
            Separator separatorMenuItem1 = new Separator();
            ContextMenu.Items.Add(separatorMenuItem1);

            if (misspelledWords!=null)
            {
                MenuItem mi = new MenuItem();
                mi.Header = StringResources.AddCustom;
                mi.Command = new DelegateCommand(
                    delegate
                    {
                        SpellChecker.SaveToCustomDictionary(selectedMisspelledWord.Text);
                        this.FireTextChangeEvent();
                    });
                mi.CommandTarget = this;
                ContextMenu.Items.Add(mi);

                Separator separatorMenuItem2 = new Separator();
                ContextMenu.Items.Add(separatorMenuItem2);
            }
            
            MenuItem menuItemCopy = new MenuItem { Header = "Copy", Command = ApplicationCommands.Copy };
            MenuItem menuItemPaste = new MenuItem { Header = "Paste", Command = ApplicationCommands.Paste };
            MenuItem menuItemSelectAll = new MenuItem { Header = "Select All", Command = ApplicationCommands.SelectAll };

            ContextMenu.Items.Add(menuItemCopy);
            ContextMenu.Items.Add(menuItemPaste);
            ContextMenu.Items.Add(menuItemSelectAll);

        }

        public static readonly DependencyProperty DictionaryPathProperty =
            DependencyProperty.Register(
            "DictionaryPath",
            typeof(string),
            typeof(SpellTextBox));

        public string DictionaryPath
        {
            get { return (string)this.GetValue(DictionaryPathProperty); }
            set { this.SetValue(DictionaryPathProperty, value); }
        }

        public static readonly DependencyProperty CustomDictionaryPathProperty =
            DependencyProperty.Register(
            "CustomDictionaryPath",
            typeof(string),
            typeof(SpellTextBox));

        public string CustomDictionaryPath
        {
            get { return (string)this.GetValue(CustomDictionaryPathProperty) ?? "CustomDictionary.txt"; }
            set { this.SetValue(CustomDictionaryPathProperty, value); }
        }

        public static readonly DependencyProperty IsSpellCheckEnabledProperty =
            DependencyProperty.Register(
            "IsSpellCheckEnabled",
            typeof(bool),
            typeof(SpellTextBox));

        public bool IsSpellCheckEnabled
        {
            get { return (bool)this.GetValue(IsSpellCheckEnabledProperty); }
            set { this.SetValue(IsSpellCheckEnabledProperty, value); }
        }


        public void Initialize()
        {
            adornerLayer = AdornerLayer.GetAdornerLayer(this);
            adorner = new RedUnderlineAdorner(this);
            adornerLayer.Add(adorner);
            InitSpellChecker();
        }

        public void Dispose()
        {
            adorner.Dispose();
            adornerLayer.Remove(adorner);
            this.ContextMenu.Opened -= OnContextMenuOpening;
            textChangedTimer.Elapsed -= textChangedTimerOnElapse;
            if (SpellChecker != null)
                SpellChecker.Dispose();
        }

        public void ReplaceSelectedWord(Word WordToReplaceWith)
        {
            if (WordToReplaceWith.Text != StringResources.NoSuggestions)
            {
                int lineIndex = selectedMisspelledWord.LineIndex;
                int wordIndex = selectedMisspelledWord.Index;
                int textIndex = GetCharacterIndexFromLineIndex(lineIndex) + wordIndex;
                string replacement = WordToReplaceWith.Text;
                Text = Text.Remove(textIndex, selectedMisspelledWord.Length).Insert(textIndex, replacement);
                SelectionStart = textIndex + WordToReplaceWith.Length;
            }
        }
        
        public void FireTextChangeEvent()
        {

        }
    }
}
