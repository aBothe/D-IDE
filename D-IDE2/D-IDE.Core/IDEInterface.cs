using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace D_IDE.Core
{
	public class IDEInterface
	{
		public List<ILanguageBinding> Bindings = new List<ILanguageBinding>();

		public ILanguageBinding LoadLanguageInterface(string file,string LanguageInterface)
		{
			var ass = Assembly.LoadFrom(file);
			
			var lang = ass.CreateInstance(LanguageInterface) as ILanguageBinding;
			if (lang == null)
				throw new Exception("Could not instantiate "+LanguageInterface+" of "+file);

			return lang;
		}
	}
}
