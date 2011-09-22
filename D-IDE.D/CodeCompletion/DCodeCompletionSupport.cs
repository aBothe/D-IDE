using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using D_IDE.Core;
using D_Parser.Dom;
using D_Parser.Dom.Expressions;
using D_Parser.Dom.Statements;
using D_Parser.Parser;
using D_Parser.Resolver;
using ICSharpCode.AvalonEdit.CodeCompletion;
using D_Parser.Completion;

namespace D_IDE.D
{
	public class DCodeCompletionSupport:AbstractCompletionSupport
	{
		public DCodeCompletionSupport(ICompletionDataGenerator CompletionDataGenerator)
			: base(CompletionDataGenerator) { }

		public static void BuildCompletionData(DEditorDocument EditorDocument, IList<ICompletionData> l, string EnteredText,
			out string lastResultPath)
		{
			var ccs = new DCodeCompletionSupport(new IDECompletionDataGenerator(l));

			ccs.BuildCompletionData(EditorDocument,EnteredText,out lastResultPath);
		}

		public static bool CanShowCompletionWindow(DEditorDocument EditorDocument)
		{
			return !DResolver.CommentSearching.IsInCommentAreaOrString(EditorDocument.Editor.Text, EditorDocument.Editor.CaretOffset);
		}

		public static void BuildToolTip(DEditorDocument EditorDocument, ToolTipRequestArgs ToolTipRequest)
		{
			if (!ToolTipRequest.InDocument)
				return;

			var ttContents = BuildToolTip(EditorDocument);

			if (ttContents == null)
				return;

			int offset = EditorDocument.Editor.Document.GetOffset(ToolTipRequest.Line, ToolTipRequest.Column);

			try
			{
				var vertStack = new StackPanel() { Orientation = Orientation.Vertical };
				string lastDescription = "";
				foreach (var tt in ttContents)
				{
					vertStack.Children.Add(
						ToolTipContentHelper.CreateToolTipContent(
						tt.Title,
						lastDescription == tt.Description ? null : lastDescription= tt.Description)
						as UIElement);
				}

				ToolTipRequest.ToolTipContent = vertStack;
			}
			catch { }
		}

		#region Module enumeration helper

		public static IEnumerable<IAbstractSyntaxTree> EnumAvailableModules(DEditorDocument Editor)
		{
			return EnumAvailableModules(Editor.HasProject ? Editor.Project as DProject : null);
		}

		public static IEnumerable<IAbstractSyntaxTree> EnumAvailableModules(DProject Project)
		{
			var ret = new List<IAbstractSyntaxTree>();

			if (Project != null)
			{
				// Add all parsed global modules that belong to the project's compiler configuration
				var cch = Project.CompilerConfiguration.ASTCache.ParseCache;
				if (cch != null)
					ret.AddRange(cch);

				// Add all modules that exist in the current solution.
				if (Project.Solution != null)
				{
					foreach (var prj in Project.Solution)
						if (prj is DProject)
							ret.AddRange((prj as DProject).ParsedModules);
				}
				else // If no solution present, add scanned modules of our current project only
					ret.AddRange(Project.ParsedModules);
			}
			else // If no project present, only add the modules of the default compiler configuration
			{
				var cch = DSettings.Instance.DMDConfig().ASTCache.ParseCache;
				if (cch != null)
					ret.AddRange(cch);
			}

			return ret;
		}
		#endregion

		#region Image helper
		static Dictionary<string, ImageSource> images = new Dictionary<string, ImageSource>();
		static bool wasInitialized = false;

		static BitmapSource ConvertWFToWPFBitmap(System.Drawing.Bitmap bmp)
		{
			return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}

		static void InitImages()
		{
			if (wasInitialized)
				return;

			try
			{
				var bi = new BitmapImage();

				images["keyword"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Keyword);
				images["namespace"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_NameSpace);

				images["class"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Class);
				images["class_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalClass);
				images["class_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateClass);
				images["class_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedClass);

				images["struct"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Struct);
				images["struct_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalStruct);
				images["struct_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateStruct);
				images["struct_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedStruct);

				images["interface"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Interface);
				images["interface_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalInterface);
				images["interface_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateInterface);
				images["interface_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedInterface);

				images["enum"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Enum);
				images["enum_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalEnum);
				images["enum_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateEnum);
				images["enum_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedEnum);

				images["method"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Method);
				images["method_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalMethod);
				images["method_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateMethod);
				images["method_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedMethod);

				images["parameter"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Parameter);
				images["local"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Local);

				images["field"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Field);
				images["field_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalField);
				images["field_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateField);
				images["field_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedField);

				images["property"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Property);
				images["property_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalProperty);
				images["property_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateProperty);
				images["property_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedProperty);

				images["delegate"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Delegate);
				images["delegate_internal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_InternalDelegate);
				images["delegate_private"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_PrivateDelegate);
				images["delegate_protected"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_ProtectedDelegate);

				images["literal"] = ConvertWFToWPFBitmap(DIcons.Icons_16x16_Literal);
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex);
			}
			wasInitialized = true;
		}

		public static ImageSource GetNodeImage(string key)
		{
			if (!wasInitialized)
				InitImages();

			if (images.ContainsKey(key))
				return images[key];
			return null;
		}
		#endregion
	}

	/// <summary>
	/// Mediation interface between low-level D_Parser and high-level DCodeCompletionSupport
	/// </summary>
	public class IDECompletionDataGenerator : ICompletionDataGenerator
	{
		public IList<ICompletionData> CompletionList;

		public IDECompletionDataGenerator(IList<ICompletionData> l)
		{
			CompletionList = l;
		}

		public void Add(int Token)
		{
			CompletionList.Add(new TokenCompletionData(Token));
		}

		public void Add(INode Node)
		{
			CompletionList.Add(new DCompletionData(Node));
		}

		public void Add(string ModuleName, IAbstractSyntaxTree Module = null, string PathOverride = null)
		{
			CompletionList.Add(new NamespaceCompletionData(ModuleName, Module) { ExplicitModulePath=PathOverride });
		}
	}

	public class TokenCompletionData : ICompletionData
	{
		public int Token { get; set; }

		public TokenCompletionData(int Token)
		{
			try
			{
				this.Token = Token;
				Text = DTokens.GetTokenString(Token);
				Description = ToolTipContentHelper.CreateToolTipContent(Text, DTokens.GetDescription(Token));
				Image = DCodeCompletionSupport.GetNodeImage("keyword");
			}
			catch (Exception ex)
			{
				ErrorLogger.Log(ex);
			}
		}

		public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ICSharpCode.AvalonEdit.Document.ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			textArea.Document.Replace(completionSegment, Text);
		}

		public object Content
		{
			get { return Text; }
		}

		public object Description
		{
			get;
			private set;
		}

		public System.Windows.Media.ImageSource Image
		{
			get;
			private set;
		}

		public double Priority
		{
			get { return 1; }
		}

		public string Text
		{
			get;
			private set;
		}
	}

	public class NamespaceCompletionData : ICompletionData
	{
		public string ModuleName { get; private set; }
		public string ExplicitModulePath { get; set; }
		public IAbstractSyntaxTree AssociatedModule { get; private set; }

		public NamespaceCompletionData(string ModuleName, IAbstractSyntaxTree AssocModule=null)
		{
			this.ModuleName = ModuleName;
			AssociatedModule = AssocModule;

			Init();
		}

		void Init()
		{
			bool IsPackage = AssociatedModule == null;

			var descString=(IsPackage ? "(Package)" : "(Module)");

			if (!string.IsNullOrWhiteSpace(ExplicitModulePath))
				descString += ExplicitModulePath;
			else if (AssociatedModule != null)
			{
				descString +=" "+ AssociatedModule.FileName;

				if (AssociatedModule.Description != null)
					descString += "\r\n" + AssociatedModule.Description;
			}

			Description = ToolTipContentHelper.CreateToolTipContent(
				IsPackage ? ModuleName : AssociatedModule.ModuleName, descString);
		}

		public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ICSharpCode.AvalonEdit.Document.ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			textArea.Document.Replace(completionSegment, Text);
		}

		public object Content
		{
			get { return Text; }
		}

		public object Description
		{
			get;
			private set;
		}

		public System.Windows.Media.ImageSource Image
		{
			get { return DCodeCompletionSupport.GetNodeImage("namespace"); }
		}

		public double Priority
		{
			get { return 1; }
		}

		public string Text
		{
			get { return ModuleName; }
		}
	}

	public class DCompletionData : ICompletionData, IComparable<ICompletionData>
	{
		public DCompletionData(INode n)
		{
			Node = n;
		}

		public string NodeString
		{
			get
			{
				try
				{
					return Node.ToString();
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Parser);
					return "";
				}
			}
		}

		/// <summary>
		/// Returns node string without attributes and without node path
		/// </summary>
		public string PureNodeString
		{
			get
			{
				try
				{
					if (Node is DNode)
						return (Node as DNode).ToString(false, false);
					return Node.ToString();
				}
				catch (Exception ex)
				{
					ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Parser);
					return "";
				}
			}
		}

		public INode Node { get; protected set; }

		public void Complete(ICSharpCode.AvalonEdit.Editing.TextArea textArea, ICSharpCode.AvalonEdit.Document.ISegment completionSegment, EventArgs insertionRequestEventArgs)
		{
			textArea.Document.Replace(completionSegment, Text);
		}

		public object Content
		{
			get { return Text; }
		}

		public object Description
		{
			// If an empty description was given, do not show an empty decription tool tip
			get
			{
				try
				{
					return ToolTipContentHelper.CreateToolTipContent(NodeString, Node.Description);
				}
				catch (Exception ex) { ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Parser); }
				return null;
			}
		}

		public System.Windows.Media.ImageSource Image
		{
			get
			{
				try
				{
					var n = Node as DNode;

					if (n == null)
						return null;

					if (n is DClassLike)
					{
						switch ((n as DClassLike).ClassType)
						{
							case DTokens.Template:
							case DTokens.Class:
								if (n.ContainsAttribute(DTokens.Package))
									return DCodeCompletionSupport.GetNodeImage("class_internal");
								else if (n.ContainsAttribute(DTokens.Protected))
									return DCodeCompletionSupport.GetNodeImage("class_protected");
								else if (n.ContainsAttribute(DTokens.Private))
									return DCodeCompletionSupport.GetNodeImage("class_private");
								return DCodeCompletionSupport.GetNodeImage("class");

							case DTokens.Union:
							case DTokens.Struct:
								if (n.ContainsAttribute(DTokens.Package))
									return DCodeCompletionSupport.GetNodeImage("struct_internal");
								else if (n.ContainsAttribute(DTokens.Protected))
									return DCodeCompletionSupport.GetNodeImage("struct_protected");
								else if (n.ContainsAttribute(DTokens.Private))
									return DCodeCompletionSupport.GetNodeImage("struct_private");
								return DCodeCompletionSupport.GetNodeImage("struct");

							case DTokens.Interface:
								if (n.ContainsAttribute(DTokens.Package))
									return DCodeCompletionSupport.GetNodeImage("interface_internal");
								else if (n.ContainsAttribute(DTokens.Protected))
									return DCodeCompletionSupport.GetNodeImage("interface_protected");
								else if (n.ContainsAttribute(DTokens.Private))
									return DCodeCompletionSupport.GetNodeImage("interface_private");
								return DCodeCompletionSupport.GetNodeImage("interface");
						}
					}
					else if (n is DEnum)
					{
						if (n.ContainsAttribute(DTokens.Package))
							return DCodeCompletionSupport.GetNodeImage("enum_internal");
						else if (n.ContainsAttribute(DTokens.Protected))
							return DCodeCompletionSupport.GetNodeImage("enum_protected");
						else if (n.ContainsAttribute(DTokens.Private))
							return DCodeCompletionSupport.GetNodeImage("enum_private");
						return DCodeCompletionSupport.GetNodeImage("enum");
					}
					else if (n is DMethod)
					{
						//TODO: Getter or setter functions should be declared as a >single< property only
						if (n.ContainsPropertyAttribute())
						{
							if (n.ContainsAttribute(DTokens.Package))
								return DCodeCompletionSupport.GetNodeImage("property_internal");
							else if (n.ContainsAttribute(DTokens.Protected))
								return DCodeCompletionSupport.GetNodeImage("property_protected");
							else if (n.ContainsAttribute(DTokens.Private))
								return DCodeCompletionSupport.GetNodeImage("property_private");
							return DCodeCompletionSupport.GetNodeImage("property");
						}

						if (n.ContainsAttribute(DTokens.Package))
							return DCodeCompletionSupport.GetNodeImage("method_internal");
						else if (n.ContainsAttribute(DTokens.Protected))
							return DCodeCompletionSupport.GetNodeImage("method_protected");
						else if (n.ContainsAttribute(DTokens.Private))
							return DCodeCompletionSupport.GetNodeImage("method_private");
						return DCodeCompletionSupport.GetNodeImage("method");
					}
					else if (n is DEnumValue)
						return DCodeCompletionSupport.GetNodeImage("literal");
					else if (n is DVariable)
					{
						if (n.ContainsPropertyAttribute())
						{
							if (n.ContainsAttribute(DTokens.Package))
								return DCodeCompletionSupport.GetNodeImage("property_internal");
							else if (n.ContainsAttribute(DTokens.Protected))
								return DCodeCompletionSupport.GetNodeImage("property_protected");
							else if (n.ContainsAttribute(DTokens.Private))
								return DCodeCompletionSupport.GetNodeImage("property_private");
							return DCodeCompletionSupport.GetNodeImage("property");
						}

						if (n.Type is DelegateDeclaration)
						{
							if (n.ContainsAttribute(DTokens.Package))
								return DCodeCompletionSupport.GetNodeImage("delegate_internal");
							else if (n.ContainsAttribute(DTokens.Protected))
								return DCodeCompletionSupport.GetNodeImage("delegate_protected");
							else if (n.ContainsAttribute(DTokens.Private))
								return DCodeCompletionSupport.GetNodeImage("delegate_private");
							return DCodeCompletionSupport.GetNodeImage("delegate");
						}

						if (n.ContainsAttribute(DTokens.Const))
							return DCodeCompletionSupport.GetNodeImage("literal");

						var realParent = n.Parent as DNode;

						if (n.Parent is IAbstractSyntaxTree && !(n as DVariable).IsAlias)
							return DCodeCompletionSupport.GetNodeImage("field");

						if (realParent == null)
							return DCodeCompletionSupport.GetNodeImage("local");

						if (realParent is DClassLike)
						{
							if (n.ContainsAttribute(DTokens.Package))
								return DCodeCompletionSupport.GetNodeImage("field_internal");
							else if (n.ContainsAttribute(DTokens.Protected))
								return DCodeCompletionSupport.GetNodeImage("field_protected");
							else if (n.ContainsAttribute(DTokens.Private))
								return DCodeCompletionSupport.GetNodeImage("field_private");
							return DCodeCompletionSupport.GetNodeImage("field");
						}

						if (realParent is DMethod)
						{
							if ((realParent as DMethod).Parameters.Contains(n))
								return DCodeCompletionSupport.GetNodeImage("parameter");
							return DCodeCompletionSupport.GetNodeImage("local");
						}

						if (realParent.ContainsTemplateParameter(n.Name))
							return DCodeCompletionSupport.GetNodeImage("parameter");
					}
				}
				catch (Exception ex) { ErrorLogger.Log(ex, ErrorType.Error, ErrorOrigin.Parser); }
				return null;
			}
		}

		public double Priority
		{
			get { return 1; }
		}

		public string Text
		{
			get { return Node.Name; }
			protected set { }
		}

		public int CompareTo(ICompletionData other)
		{
			return Node.Name!=null?Node.Name.CompareTo(other.Text):-1;
		}
	}

	public class ToolTipContentHelper
	{
		public static object CreateToolTipContent(string Head, string Description = null)
		{
			var nodeLabel = new TextBlock() { Text = Head, Foreground=Brushes.Black, FontWeight = FontWeights.DemiBold, Padding = new Thickness(0) };

			if (string.IsNullOrWhiteSpace(Description))
				return nodeLabel;
			else
			{
				var descLabel = new TextBlock() { Text = Description, Padding = new Thickness(0) };

				var vertSplitter = new StackPanel() { Orientation = Orientation.Vertical };
				vertSplitter.Children.Add(nodeLabel);
				vertSplitter.Children.Add(descLabel);

				return vertSplitter;
			}
		}
	}
}
