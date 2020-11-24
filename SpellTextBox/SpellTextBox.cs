using NHunspell;
using System.Collections.Generic;
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
        static SpellTextBox()
        {
            TextProperty.OverrideMetadata(typeof(SpellTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(TextPropertyChanged)));
        }

        public SpellTextBox() : base()
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
        }

        #region Timer

        static Timer textChangedTimer = new System.Timers.Timer(500);
        ElapsedEventHandler textChangedTimerOnElapse;

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
                Checker.CheckSpelling(this);
                RaiseSpellcheckCompletedEvent();
            }));
        }

        #endregion

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

            Checker.SelectedMisspelledWord = Checker.MisspelledWords.FirstOrDefault(w => w.LineIndex == GetLineIndexFromCharacterIndex(SelectionStart) && SelectionStart >= lineFirstCharIndex + w.Index && SelectionStart <= lineFirstCharIndex + w.Index + w.Length);
           
            this.ContextMenu.Items.Clear();
            foreach (var item in Checker.MenuActions)
            {
                var mi = new MenuItem();
                mi.Header = item.Name;
                mi.Command = item.Command;
                this.ContextMenu.Items.Add(mi);
            }
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
            get { return (string)this.GetValue(CustomDictionaryPathProperty) ?? "custom.txt"; }
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

        AdornerLayer myAdornerLayer;
        RedUnderlineAdorner myAdorner;

        public void Initialize()
        {
            myAdornerLayer = AdornerLayer.GetAdornerLayer(this);
            myAdorner = new RedUnderlineAdorner(this);
            myAdornerLayer.Add(myAdorner);
        }

        public void Dispose()
        {
            myAdorner.Dispose();
            myAdornerLayer.Remove(myAdorner);
            this.ContextMenu.Opened -= OnContextMenuOpening;
            textChangedTimer.Elapsed -= textChangedTimerOnElapse;
            if (checker != null)
                checker.Dispose();
        }

        private SpellChecker checker;

        public SpellChecker Checker
        {
            get { return checker ?? CreateSpellCheker(); }
            set { checker = value; }
        }

        private SpellChecker CreateSpellCheker()
        {
            checker = new SpellChecker(new Hunspell(DictionaryPath + ".aff", DictionaryPath + ".dic"), this);
            checker.LoadCustomDictionary();
            return checker;
        }

        public void ReplaceSelectedWord(Word WordToReplaceWith)
        {
            if (WordToReplaceWith.Text != StringResources.NoSuggestions)
            {
                int lineIndex = Checker.SelectedMisspelledWord.LineIndex;
                int wordIndex = Checker.SelectedMisspelledWord.Index;
                int textIndex = GetCharacterIndexFromLineIndex(lineIndex) + wordIndex;
                string replacement = WordToReplaceWith.Text;
                Text = Text.Remove(textIndex, Checker.SelectedMisspelledWord.Length).Insert(textIndex, replacement);
                SelectionStart = textIndex + WordToReplaceWith.Length;
            }
        }

        public void FireTextChangeEvent()
        {
            int c = SelectionStart;
            string s = Text;
            Text = s + " ";
            Text = s;
            SelectionStart = c;
        }

        private ICommand _replace;
        public ICommand Replace
        {
            get
            {
                return _replace ?? (_replace = new DelegateCommand(
                        delegate
                        {
                            ReplaceSelectedWord(checker.SelectedSuggestedWord);
                        }));
            }
        }
    }
}
