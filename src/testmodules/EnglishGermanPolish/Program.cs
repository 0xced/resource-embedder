﻿using LocalizeHelper;
using System;

namespace EnglishGermanPolish
{
	class Program
	{
		static void Main(string[] args)
		{
			Localize.SwitchLocale("en");
			if (Translation.Language != "English")
			{
				Environment.Exit(-1);
			}
			Localize.SwitchLocale("de");
			if (Translation.Language != "German")
			{
				Environment.Exit(-2);
			}
			Localize.SwitchLocale("pl");
			if (Translation.Language != "Polish")
			{
				Environment.Exit(-3);
			}
			Environment.Exit(0);
		}
	}
}
