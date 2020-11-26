using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHunspell;

namespace SpellTextBox
{
    public sealed class SpellCheckerProvider:IDisposable
    {
        private static readonly Lazy<SpellCheckerProvider> Lazy = new Lazy<SpellCheckerProvider>(() => new SpellCheckerProvider());
        public static SpellCheckerProvider Instance { get { return Lazy.Value; } }

        private Dictionary<string, SpellChecker> _spellCheckers = new Dictionary<string, SpellChecker>();

        private SpellCheckerProvider()
        {
        }

        public SpellChecker GetSpellChecker(string dictionaryPath)
        {
            if (_spellCheckers.ContainsKey(dictionaryPath))
                return _spellCheckers[dictionaryPath];
            else
            {
                try
                {
                    var tmp = new SpellChecker(new Hunspell(dictionaryPath + ".aff", dictionaryPath + ".dic"));
                    _spellCheckers.Add(dictionaryPath, tmp);
                    return tmp;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                
            }
        }


        public void Dispose()
        {
            foreach (var spellChecker in _spellCheckers)
            {
                spellChecker.Value.Dispose();
            }
        }
    }

    
}
