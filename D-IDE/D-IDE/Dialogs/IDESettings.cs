﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Microsoft.Win32;

namespace D_IDE
{
	public partial class IDESettings : DockContent
	{
		public IDESettings()
		{
			this.DockAreas = DockAreas.Document;
			InitializeComponent();

			singleInst.Checked = D_IDE_Properties.Default.SingleInstance;
			defPrjDir.Text = D_IDE_Properties.Default.DefaultProjectDirectory;
			updates.Checked = D_IDE_Properties.Default.WatchForUpdates;
			reopenLastPrj.Checked = D_IDE_Properties.Default.OpenLastPrj;

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

			D_IDE_Properties.Default.exe_cmp = exe_cmp.Text;
			D_IDE_Properties.Default.exe_win = exe_win.Text;
			D_IDE_Properties.Default.exe_console = exe_console.Text;
			D_IDE_Properties.Default.exe_dll = exe_dll.Text;
			D_IDE_Properties.Default.exe_lib = exe_lib.Text;
			D_IDE_Properties.Default.exe_res = exe_rc.Text;
			D_IDE_Properties.Default.exe_dbg = dbg_exe.Text;
			D_IDE_Properties.Default.UseExternalDebugger=UseIntegDbg.Checked;

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
	}
}
