using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using D_IDE.Core;

namespace D_IDE
{
	/// <summary>
	/// A binding that manages all generic file types
	/// </summary>
	internal class GenericFileBinding:AbstractLanguageBinding
	{
		public GenericFileBinding()
		{
			var exts=new string[]{".txt"};
			_moduletypes.Add(new FileTemplate() { 
				Name="Text file",
				Description="Empty text file",
				Extensions=exts,
				DefaultFilePrefix="TextFile",

				LargeImage=CommonIcons.txt32,
				SmallImage=CommonIcons.txt16
			});
		}

		public override string LanguageName{get { return "General"; }}
		public override object LanguageIcon	{get { return new BitmapImage(new Uri("../Resources/file.png",UriKind.Relative)); }		}
		
		List<FileTemplate> _moduletypes = new List<FileTemplate>();
		public override FileTemplate[] ModuleTemplates { get { return _moduletypes.ToArray(); } }
	}
}
