﻿using System.Globalization;
using System.Threading;

namespace LocalizeHelper
{
    public static class Localize
    {
        public static void SwitchLocale(string culture)
        {
            if (string.IsNullOrEmpty(culture))
            {
                CultureInfo.DefaultThreadCurrentCulture = null;
                CultureInfo.DefaultThreadCurrentUICulture = null;
                return;
            }
            CultureInfo ci;
            try
            {
                ci = new CultureInfo(culture);
            }
            catch (CultureNotFoundException)
            {
                ci = null;
            }
            CultureInfo.DefaultThreadCurrentCulture = ci;
            CultureInfo.DefaultThreadCurrentUICulture = ci;
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }
    }
}
