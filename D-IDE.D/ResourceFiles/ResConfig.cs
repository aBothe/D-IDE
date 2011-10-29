using System.Xml;

namespace D_IDE.ResourceFiles
{
	public class ResConfig
	{
		public static ResConfig Instance = new ResConfig();

		public string ResourceCompilerPath="rc.exe";

		public string ResourceCompilerArguments=DefaultArgumentString;

		public const string DefaultArgumentString = "/fo \"$res\" \"$rc\"";

		public void Save(XmlWriter x)
		{
			x.WriteStartDocument();

			x.WriteStartElement("ResourceFileBinding");

			x.WriteStartElement("CompilerPath");
			x.WriteCData(ResourceCompilerPath);
			x.WriteEndElement();

			x.WriteStartElement("CompilerArguments");
			x.WriteCData(ResourceCompilerArguments);
			x.WriteEndElement();

			/*
			x.WriteStartElement("BracketHightlighting");
			x.WriteAttributeString("value", EnableMatchingBracketHighlighting.ToString().ToLower());
			x.WriteEndElement();
			*/
			x.WriteEndElement();
		}

		public void Load(XmlReader x)
		{
			while (x.Read())
			{
				switch (x.LocalName)
				{
						/*
					case "BracketHightlighting":
						if (x.MoveToAttribute("value"))
							EnableMatchingBracketHighlighting = x.ReadContentAsBoolean();
						break;*/

					case "CompilerArguments":
						ResourceCompilerArguments = x.ReadString();
						break;

					case "CompilerPath":
						ResourceCompilerPath = x.ReadString();
						break;
				}
			}
		}
	}
}
