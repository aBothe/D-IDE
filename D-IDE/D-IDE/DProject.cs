using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using D_Parser;
using System.Windows.Forms;

namespace D_IDE
{
	[Serializable()]
	public class DProject
	{
		[NonSerialized()]
		public const string prjext = ".dproj";
		public enum PrjType
		{
			WindowsApp=0,
			ConsoleApp,
			Dll,
			StaticLib,
		}

		public void ParseAll()
		{
			if(files == null) files = new List<DModule>();
			files.Clear();
			foreach(string fn in resourceFiles)
			{
				if(!DModule.Parsable(fn)) continue;
				files.Add(new DModule(fn));
			}
		}

		public bool isRelease;

		public List<string> libs;
		public string execargs;
		public string linkargs;
		public string compileargs;

		public PrjType type;
		public string name;
		public string targetfilename;

		[NonSerialized()]
		public string prjfn;
		public string basedir;

		[NonSerialized()]
		public List<DModule> files=new List<DModule>();
		public List<string> resourceFiles=new List<string>();
		public List<string> lastopen=new List<string>();

		public DProject()
		{
			isRelease = false;
			libs = new List<string>();
			execargs = "";
			linkargs = "";
			compileargs = "";

			name = "";
			targetfilename = "";
			prjfn = "";
			basedir = ".";
			files = new List<DModule>();
			resourceFiles = new List<string>();
			lastopen = new List<string>();
		}

		public bool Contains(string file)
		{
			return resourceFiles.Contains(file);
		}

		public bool AddSrc(string file)
		{
			if(!File.Exists(file)) return false;

			if(resourceFiles.Contains(file))
			{
				MessageBox.Show("File \"" + file + "\" already exists in project!"); return false;
			}
			resourceFiles.Add(file);
			return true;
			/*
			string tmod_name = Path.GetFileNameWithoutExtension(file);
			foreach(DModule tpf in files) // Check if module name already exists
			{
				if(tpf.mod == tmod_name) { MessageBox.Show("File \""+file+"\" already exists in project!"); return false; }
			}
			files.Add(new DModule(file, Path.GetFileNameWithoutExtension(file)));
			return true;*/
		}

		public void RemoveNonExisting()
		{
			List<string> newRC = new List<string>(resourceFiles);
			foreach(string rc in resourceFiles)
			{
				if(!File.Exists(rc))
				{
					newRC.Remove(rc);
				}
			}
			resourceFiles = newRC;
		}

		public bool RenameFile(string from, string to)
		{
			if(!resourceFiles.Contains(from)) return false;

			DModule dm=FileDataByFile(from);
			if(dm != null) dm.mod_file = to;

			DocumentInstanceWindow diw = Form1.thisForm.FileDataByFile(from);
			if(diw != null)
			{
				diw.fileData.mod_file = to;
				diw.Update();
			}

			resourceFiles.Remove(from);
			resourceFiles.Add(to);
			return true;
		}

		public static DProject LoadFrom(string fn)
		{
			DProject ret = null;

			if(File.Exists(fn))
			{
				BinaryFormatter formatter = new BinaryFormatter();

				Stream stream = File.Open(fn, FileMode.Open);

				try
				{
					ret = (DProject)formatter.Deserialize(stream);
				}
				catch(Exception ex) { MessageBox.Show(ex.Message); }
				stream.Close();
			}
			if(ret != null) {
				ret.files = new List<DModule>();
				ret.prjfn = fn;
				/*foreach(DModule pf in ret.files)
				{
					if(!ret.resourceFiles.Contains(pf.mod_file))
					{
						ret.resourceFiles.Add(pf.mod_file);
					}
				}*/
			}
			return ret;
		}

		public void Save()
		{
			if(String.IsNullOrEmpty(prjfn)) return;
			BinaryFormatter formatter = new BinaryFormatter();

			Stream stream = File.Open(prjfn, FileMode.Create);
			formatter.Serialize(stream, this);
			stream.Close();
		}

		public DModule FileDataByFile(string fn)
		{
			foreach(DModule pf in files)
			{
				if(pf.mod_file == fn) return pf;
			}
			return null;
		}
	}
}
