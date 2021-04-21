using LocalizeHelper;
using System;

namespace EnglishGermanPolish
{
    class Program
    {
        #region Methods

        static void Main(string[] args)
        {
            Localize.SwitchLocale("en");
            if (Translation.Language != "English")
            {
                Console.WriteLine(@"❌ Failed switching to English");
                Environment.Exit(-1);
            }
            Localize.SwitchLocale("de");
            if (Translation.Language != "German")
            {
                Console.WriteLine(@"❌ Failed switching to German");
                Environment.Exit(-2);
            }
            Localize.SwitchLocale("pl");
            if (Translation.Language != "Polish")
            {
                Console.WriteLine(@"❌ Failed switching to Polish");
                Environment.Exit(-3);
            }
            Console.WriteLine(@"✅ All localizations are OK");
            Environment.Exit(0);
        }

        #endregion Methods
    }
}