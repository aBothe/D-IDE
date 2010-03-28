using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Microsoft.Win32;
using System.IO;

namespace D_IDE
{
	public partial class IDESettings : DockContent
	{
		public IDESettings()
		{
			this.DockAreas = DockAreas.Document;
			InitializeComponent();

			ReadValues();
		}

		public void ReadValues()
		{
			singleInst.Checked = D_IDE_Properties.Default.SingleInstance;
			defPrjDir.Text = D_IDE_Properties.Default.DefaultProjectDirectory;
			updates.Checked = D_IDE_Properties.Default.WatchForUpdates;
			reopenLastPrj.Checked = D_IDE_Properties.Default.OpenLastPrj;
			restoreLastSession.Checked = D_IDE_Properties.Default.OpenLastFiles;

			parsedFiles.Items.AddRange(D_IDE_Properties.Default.parsedDirectories.ToArray());

			exe_cmp.Text = D_IDE_Properties.Default.exe_cmp;
			exe_win.Text = D_IDE_Properties.Default.exe_win;
			exe_console.Text = D_IDE_Properties.Default.exe_console;
			exe_dll.Text = D_IDE_Properties.Default.exe_dll;
			exe_lib.Text = D_IDE_Properties.Default.exe_lib;
			exe_rc.Text = D_IDE_Properties.Default.exe_res;
			dbg_exe.Text = D_IDE_Properties.Default.exe_dbg;
			UseIntegDbg.Checked = D_IDE_Properties.Default.UseExternalDebugger;

			cmp_to_obj.Text = D_IDE_Properties.Default.cmp_obj;
			link_win_exe.Text = D_IDE_Properties.Default.link_win_exe;
			link_to_exe.Text = D_IDE_Properties.Default.link_to_exe;
			link_to_dll.Text = D_IDE_Properties.Default.link_to_dll;
			link_to_lib.Text = D_IDE_Properties.Default.link_to_lib;
			rc.Text = D_IDE_Properties.Default.cmp_res;
			dbg_args.Text = D_IDE_Properties.Default.dbg_args;

			parsedFileList.Items.Clear();
			foreach (DModule gpf in D_IDE_Properties.GlobalModules)
			{
				parsedFileList.Items.Add(gpf.mod_file);
			}

			logbuildprogress_chk.Checked = D_IDE_Properties.Default.LogBuildProgress;
			showCompleteLog.Checked = D_IDE_Properties.Default.ShowBuildCommands;
			CreatePDB.Checked=D_IDE_Properties.Default.CreatePDBOnBuild;
			ShowDbgPanelsOnDebugging.Checked = D_IDE_Properties.Default.ShowDbgPanelsOnDebugging;
			StoreSettingsAtUserDocs.Checked = System.IO.File.Exists(Program.UserDocStorageFile);

			verbosedbgoutput.Checked = D_IDE_Properties.Default.VerboseDebugOutput;
			AutoSkipUnknownCode.Checked = D_IDE_Properties.Default.SkipUnknownCode;

			HighlightingEntries = D_IDE_Properties.Default.SyntaxHighlightingEntries;
			foreach (string ext in HighlightingEntries.Keys)
				HighLightingExts.Items.Add(ext);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void IDESettings_FormClosing(object sender, FormClosingEventArgs e)
		{
			D_IDE_Properties.Default.SingleInstance = singleInst.Checked;
			D_IDE_Properties.Default.DefaultProjectDirectory = defPrjDir.Text;
			D_IDE_Properties.Default.WatchForUpdates = updates.Checked;
			D_IDE_Properties.Default.OpenLastPrj = reopenLastPrj.Checked;
			D_IDE_Properties.Default.OpenLastFiles = restoreLastSession.Checked;

			D_IDE_Properties.Default.exe_cmp = exe_cmp.Text;
			D_IDE_Properties.Default.exe_win = exe_win.Text;
			D_IDE_Properties.Default.exe_console = exe_console.Text;
			D_IDE_Properties.Default.exe_dll = exe_dll.Text;
			D_IDE_Properties.Default.exe_lib = exe_lib.Text;
			D_IDE_Properties.Default.exe_res = exe_rc.Text;
			D_IDE_Properties.Default.exe_dbg = dbg_exe.Text;
			D_IDE_Properties.Default.UseExternalDebugger=UseIntegDbg.Checked;
			D_IDE_Properties.Default.CreatePDBOnBuild = CreatePDB.Checked;
			D_IDE_Properties.Default.ShowDbgPanelsOnDebugging = ShowDbgPanelsOnDebugging.Checked;
			if (StoreSettingsAtUserDocs.Checked && !File.Exists(Program.UserDocStorageFile))
			{
				File.WriteAllText(Program.UserDocStorageFile, "Remove this file if settings are stored locally");
			}
			else if (!StoreSettingsAtUserDocs.Checked && File.Exists(Program.UserDocStorageFile))
			{
				File.Delete(Program.UserDocStorageFile);
			}

			D_IDE_Properties.Default.cmp_obj = cmp_to_obj.Text;
			D_IDE_Properties.Default.link_win_exe = link_win_exe.Text;
			D_IDE_Properties.Default.link_to_exe = link_to_exe.Text;
			D_IDE_Properties.Default.link_to_dll = link_to_dll.Text;
			D_IDE_Properties.Default.link_to_lib = link_to_lib.Text;
			D_IDE_Properties.Default.cmp_res = rc.Text;
			D_IDE_Properties.Default.dbg_args = dbg_args.Text;

			D_IDE_Properties.Default.parsedDirectories.Clear();
			foreach (string dir in parsedFiles.Items)
				D_IDE_Properties.Default.parsedDirectories.Add(dir);

			D_IDE_Properties.Default.LogBuildProgress = logbuildprogress_chk.Checked;
			D_IDE_Properties.Default.ShowBuildCommands = showCompleteLog.Checked;

			D_IDE_Properties.Default.VerboseDebugOutput = verbosedbgoutput.Checked;
			D_IDE_Properties.Default.SkipUnknownCode = AutoSkipUnknownCode.Checked;

			D_IDE_Properties.Default.SyntaxHighlightingEntries = HighlightingEntries;
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (parsedFiles.SelectedIndex >= 0)
				parsedFiles.Items.RemoveAt(parsedFiles.SelectedIndex);
		}

		private void button3_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog fd = new FolderBrowserDialog();
			fd.SelectedPath = D_IDE_Properties.Default.lastSearchDir;
			if (fd.ShowDialog() == DialogResult.OK && !parsedFiles.Items.Contains(fd.SelectedPath))
			{
				parsedFiles.Items.Add(fd.SelectedPath);
				D_IDE_Properties.Default.lastSearchDir = fd.SelectedPath;
			}
		}

		private void parsedFileList_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			ListViewItem lvi = parsedFileList.GetItemAt(e.X, e.Y);
			if (lvi == null) return;

			Form1.thisForm.Open(lvi.Text);
		}

		private void button4_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog fd = new FolderBrowserDialog();
			fd.SelectedPath = defPrjDir.Text;
			if (fd.ShowDialog() == DialogResult.OK)
			{
				defPrjDir.Text = fd.SelectedPath;
			}
		}

		private void Assoc_DProj_CheckedChanged(object sender, EventArgs e)
		{
			try { Registry.ClassesRoot.DeleteSubKeyTree(DProject.prjext); }
			catch { }
			try { Registry.ClassesRoot.DeleteSubKeyTree(".d"); }
			catch { }
			try { Registry.ClassesRoot.DeleteSubKeyTree(".rc"); }
			catch { }
			
			try
			{
				RegistryKey rk = Registry.ClassesRoot.CreateSubKey(DProject.prjext).CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
				rk.SetValue("", "\"" + Application.ExecutablePath + "\" \"%1\"", RegistryValueKind.String);
				rk = Registry.ClassesRoot.CreateSubKey(DProject.prjext).CreateSubKey("DefaultIcon");
				rk.SetValue("", "\"" + Application.StartupPath + "\\dproj.ico\"", RegistryValueKind.String);

				rk = Registry.ClassesRoot.CreateSubKey(".d").CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
				rk.SetValue("", "\"" + Application.ExecutablePath + "\" \"%1\"", RegistryValueKind.String);
				rk = Registry.ClassesRoot.CreateSubKey(".d").CreateSubKey("DefaultIcon");
				rk.SetValue("", "\"" + Application.StartupPath + "\\d.ico\"", RegistryValueKind.String);

				rk = Registry.ClassesRoot.CreateSubKey(".rc").CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
				rk.SetValue("", "\"" + Application.ExecutablePath + "\" \"%1\"", RegistryValueKind.String);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void button6_Click(object sender, EventArgs e)
		{
			try
			{
				Registry.ClassesRoot.DeleteSubKeyTree(DProject.prjext);
				Registry.ClassesRoot.DeleteSubKeyTree(".d");
				Registry.ClassesRoot.DeleteSubKeyTree(".rc");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void button6_Click_1(object sender, EventArgs e)
		{
			D_IDE_Properties.Default = new D_IDE_Properties();
			ReadValues();
		}

		#region Highlighting
		Dictionary<string, string> HighlightingEntries;
		private void HighlightingAddExt_Click(object sender, EventArgs e)
		{
			if (HighLightingExt.Text != "" && !HighlightingEntries.ContainsKey(HighLightingExt.Text))
			{
				HighLightingExts.Items.Add(HighLightingExt.Text);
				HighlightingEntries.Add(HighLightingExt.Text,HighLightingAssocXSHDFile.Text);
			}
		}
		
		private void HighLightingDelExt_Click(object sender, EventArgs e)
		{
			if (HighLightingExts.SelectedIndex >= 0)
			{
				HighlightingEntries.Remove((string)HighLightingExts.SelectedItem);
				HighLightingExts.Items.RemoveAt(HighLightingExts.SelectedIndex);
			}
		}

		private void HighLightingExts_SelectedIndexChanged(object sender, EventArgs e)
		{
			if(HighLightingExts.SelectedItem!=null)
				HighLightingAssocXSHDFile.Text = HighlightingEntries[(string)HighLightingExts.SelectedItem];
		}

		private void HighLightingSearchXSHD_Click(object sender, EventArgs e)
		{
			OpenFileDialog of = new OpenFileDialog();
			of.Filter = "Highlighting style document (*.xshd)|*.xshd";
			of.CheckFileExists = true;
			of.FilterIndex = 0;
			of.Title = "Select style info file";
			if (of.ShowDialog() == DialogResult.OK)
			{
				HighLightingAssocXSHDFile.Text = of.FileName;
				if (HighLightingExts.SelectedItem != null)
					HighlightingEntries[(string)HighLightingExts.SelectedItem]=of.FileName;
			}
		}
		#endregion
	}
}
