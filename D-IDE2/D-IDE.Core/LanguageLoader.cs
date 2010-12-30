using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D_IDE.Core;
using System.Reflection;

namespace D_IDE
{
	public class LanguageLoader
	{
		/// <summary>
		/// Central access point for retrieving any loaded language
		/// </summary>
		public static List<AbstractLanguageBinding> Bindings = new List<AbstractLanguageBinding>();

		/// <summary>
		/// Loads a language interface dll into the RAM and puts its language binding into <see cref="Bindings"/>
		/// </summary>
		/// <param name="file"></param>
		/// <param name="LanguageInterface"></param>
		/// <returns></returns>
		public static AbstractLanguageBinding LoadLanguageInterface(string file, string LanguageInterface)
		{
			var ass = Assembly.LoadFrom(file);

			if (ass == null)
				throw new Exception("Could not load " + file);

			var lang = ass.CreateInstance(LanguageInterface) as AbstractLanguageBinding;

			if (lang == null)
				throw new Exception("Could not instantiate " + LanguageInterface + " of " + file);

			Bindings.Add(lang);

			return lang;
		}
	}
}
