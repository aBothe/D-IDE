using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using D_IDE.FormsDesigner;
using System.Drawing;
using System.ComponentModel.Design;
using D_Parser;
using System;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.TextEditor.Util;
using System.IO;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Parser;
using System.ComponentModel;

namespace D_IDE
{
	public class FXFormsDesignerException : Exception
	{
		public FXFormsDesignerException(string msg)
			: base(msg)
		{
		}
	}

	public class FXFormsDesigner : DockContent
	{
		public HostSurfaceManager surMgr=new HostSurfaceManager();
		public string FileName;

		public Form editForm
		{
			get {
				if(surMgr.ActiveDesignSurface.ComponentContainer.Components.Count > 0 && surMgr.ActiveDesignSurface.ComponentContainer.Components[0] is Form)
					return (Form)surMgr.ActiveDesignSurface.ComponentContainer.Components[0];
				else 
					return null;
			}
		}

		const string InitComment = "/*###FXForms - Begin###*/";
		const string EndComment = "/*###FXForms - End###*/";

		public bool SaveToFile(string File)
		{/*
			string fcon = System.IO.File.ReadAllText(File);

			string funcContent = "\r\nvoid InitializeComponents()\r\n{\r\n";
			// Insert intializers
			// Insert properties
			foreach (Control c in editForm.Controls)
			{
				funcContent +=
					"//\r\n// "+c.Name+"\r\n//\r\n" +
					c.Name + " = new " + c.GetType().Name + "(this,0,0,0,0);\r\n" +
				c.Name + ".Text = \"" + c.Text + "\"w;\r\n" +
				c.Name + ".Size = Point(" + c.Size.Width.ToString() + "," + c.Size.Height.ToString() + ");\r\n"+
				c.Name + ".Position = Point(" + c.Location.X.ToString() + "," + c.Location.Y.ToString() + ");\r\n"
				;
			}
			// Insert form properties
			funcContent += 
				"// Main window properties\r\n"+
				"this.Text = \"" + editForm.Text + "\"w;\r\n"+
				"this.Size = Point(" + editForm.Size.Width.ToString() + "," + editForm.Size.Height.ToString() + ");\r\n" +
				"this.Position = Point(" + editForm.Location.X.ToString() + "," + editForm.Location.Y.ToString() + ");\r\n"
				;
			// End up InitializeComponents();
			funcContent+="\r\n}\r\n\r\n\r\n";
			// Insert object declarations
			foreach (Control c in editForm.Controls)
			{
				funcContent += c.GetType().Name+" "+c.Name+";\r\n";
			}
			funcContent += "\r\n";

			int start = fcon.IndexOf(InitComment), endoff;
			if (start >= 0) // Search the InitComment string in the source file
			{
				start += InitComment.Length;

				endoff = fcon.IndexOf(EndComment, start);

				fcon = fcon.Remove(start, endoff - start);
				fcon=fcon.Insert(start, funcContent);
			}
			else // Search first class that extends the fx.window.Window class
			{
				List<string> imp=new List<string>();
				DNode dom= DParser.ParseText(File,"temp",fcon,out imp);
				if (dom == null) return false;
				DNode Node=null;
				foreach (INode n in dom)
				{
					DNode ch = n as DNode;
					string[] a = ch.superClass.Split('.');
					if (a.Length < 0 || a[a.Length - 1] != "Window") continue;

					Node = ch;
					break;
				}
				if (Node == null)
				{
					return false;
				}
				start = 0;
				for (int i = 1; i < Node.StartLocation.Line; i++)
				{
					start = fcon.IndexOf("\r\n", start) + 2;
					if (start < 0) return false;
				}

				start = fcon.IndexOf('{',start);
				if (start < 0) return false;
				start++;

				fcon=fcon.Insert(start,InitComment+funcContent+EndComment);
			}

			System.IO.File.WriteAllText(File,fcon);
*/
			return true;
		}

		protected bool ParseFile(string file)
		{/*
			editForm.Controls.Clear();

			string fcon = File.ReadAllText(file);

			int start = fcon.IndexOf(InitComment), endoff;
			if (start >= 0) // Search the InitComment string in the source file
			{
				start += InitComment.Length;

				endoff = fcon.IndexOf(EndComment, start);
				if (endoff < 0) return false;

				fcon = fcon.Substring(start, endoff - start);
			}
			else
				return false;

			List<string> imp = new List<string>();
			DNode dom = DParser.ParseText(file, "temp", fcon, out imp);
			string[] a=null;

			//Add all declarations to the editForm
			foreach (INode n in dom)
			{
				Control ctrl = null;
				DNode ch = n as DNode;

				if (ch.fieldtype == FieldType.Variable)
				{
					a = ch.type.Split('.');
					if (a.Length < 1) continue;

					switch (a[a.Length - 1])
					{
						default: break;
						case "Button":
							ctrl = new Button();
							ctrl.Text = ch.name;
							break;
						case "TextBox":
							ctrl = new TextBox();
							break;
					}

					if (ctrl != null)
					{
						ctrl.Name = ch.name;
						ctrl.Tag = ch;
					}
				}

				if (ctrl != null) AddControl(ctrl);
			}

			StringReader sr = new StringReader(fcon);
			DLexer lex = new DLexer(sr);

			Control scopedCtrl = null;
			
			lex.NextToken();
			while (lex.LookAhead.Kind != DTokens.OpenCurlyBrace)
				lex.NextToken();
			while (lex.LookAhead.Kind != DTokens.EOF)
			{
				lex.NextToken();
				switch (lex.LookAhead.Kind)
				{
					default: break;
						// button1 = new Button();
					case DTokens.This:
					case DTokens.Identifier:
						if (lex.LookAhead.Kind == DTokens.This)
						{
							scopedCtrl = editForm;
						}
						else
						{
							IDesignerHost idh = (IDesignerHost)this.surMgr.ActiveDesignSurface.GetService(typeof(IDesignerHost));

							foreach (IComponent ic in idh.Container.Components)
							{
								Control c = ic as Control;
								if (c.Name == (string)lex.LookAhead.Value)
								{
									scopedCtrl = c;
									break;
								}
							}
						}
						if (scopedCtrl == null) break;
						lex.NextToken();
						switch (lex.LookAhead.Kind)
						{
							default: break;
							case DTokens.Assign:
								lex.NextToken(); // skip to 'new'
								if (lex.LookAhead.Kind == DTokens.New)
								{
									lex.NextToken(); // skip to type name
									lex.NextToken(); // skip to open brace
									if (lex.LookAhead.Kind == DTokens.OpenParenthesis)
									{
										lex.NextToken(); // skip to 'this'

										lex.NextToken(); // skip to comma
										lex.NextToken(); // skip to 'x'-alignment
										int X = (int)lex.LookAhead.LiteralValue;

										lex.NextToken(); // skip to comma
										lex.NextToken(); // skip to 'y'-alignment
										int Y = (int)lex.LookAhead.LiteralValue;

										lex.NextToken(); // skip to comma
										lex.NextToken(); // skip to 'w'-alignment
										int Width = (int)lex.LookAhead.LiteralValue;

										lex.NextToken(); // skip to comma
										lex.NextToken(); // skip to 'h'-alignment
										int Height = (int)lex.LookAhead.LiteralValue;

										scopedCtrl.SetBounds(X, Y, Width, Height, BoundsSpecified.All);
									}
								}
								break;
							case DTokens.Dot:
								
								lex.NextToken(); // skip to property
								string propname = lex.LookAhead.Value;

								switch (propname)
								{
									case "Text":
										lex.NextToken(); // skip to '='
										if (lex.LookAhead.Kind == DTokens.Assign)
										{
											lex.NextToken(); // skip to text content
											scopedCtrl.Text = (string)lex.LookAhead.LiteralValue;
										}
										break;
									case "Position":
									case "Size":
										lex.NextToken(); // skip to '='
										if (lex.LookAhead.Kind != DTokens.Assign) break;
										lex.NextToken();
										if (lex.LookAhead.Kind != DTokens.Identifier && lex.LookAhead.Value!="Point") break;
										lex.NextToken();
										if (lex.LookAhead.Kind != DTokens.OpenParenthesis) break;
										lex.NextToken();
										if (lex.LookAhead.Kind != DTokens.Literal) break;
										int X = (int)lex.LookAhead.LiteralValue;
										lex.NextToken();
										if (lex.LookAhead.Kind != DTokens.Comma) break;
										lex.NextToken();
										int Y = (int)lex.LookAhead.LiteralValue;

										if (propname == "Size")
											scopedCtrl.Size = new Size(X, Y);
										else 
											scopedCtrl.Location = new Point(X,Y);
										
										break;
									default: break;
								}

								break;
						}
						scopedCtrl = null;
						break;
				}
			}

			sr.Close();
            */
			return true;
		}

		public void AddControl(Control c)
		{
			editForm.Controls.Add(c);
			IDesignerHost idh = (IDesignerHost)this.surMgr.ActiveDesignSurface.GetService(typeof(IDesignerHost));
			idh.Container.Add(c);
		}

		public FXFormsDesigner(string file) : base()
		{
			surMgr.AddService(typeof(System.Windows.Forms.PropertyGrid),D_IDEForm.thisForm.propView.propertyGrid);
			try
			{
				Control hsc = surMgr.GetNewHost(typeof(Form));
				hsc.BackColor = Color.White;
				hsc.Dock = DockStyle.Fill;
				this.Controls.Add(hsc);

				TabText = Path.GetFileNameWithoutExtension(file)+" - Forms";

				editForm.Text = "MyApp";
				/*
				Button b1 = new Button();
				b1.Text = "Test";
				b1.Location = new Point(20,10);
				b1.Size = new Size(120,26);
				b1.Show();
				AddControl(b1);*/

				ParseFile(FileName=file);
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public void Reload()
		{
			ParseFile(FileName);
		}

		public bool Save()
		{
			return SaveToFile(FileName);
		}
	}
}
