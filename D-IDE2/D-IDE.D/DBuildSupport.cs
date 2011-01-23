using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using D_IDE.Core;
using System.IO;
using Parser.Core;

namespace D_IDE.D
{
	public class DBuildSupport
	{
		#region Properties
		readonly List<GenericError> TempBuildErrorList = new List<GenericError>();
		Process TempPrc = null;
		#endregion

		public GenericError[] BuildSingleModule(string FileName)
		{
			TempBuildErrorList.Clear();

			string exe = Path.ChangeExtension(FileName, ".exe");

			var dmd = DSettings.Instance.DMD2;
			TempPrc = FileExecution.ExecuteSilentlyAsync(
				Path.Combine(dmd.BaseDirectory, dmd.SoureCompiler),
				String.Format("\"{0}\" -of\"{1}\"", FileName,exe),
				Path.GetDirectoryName(FileName),
				OnOutput, OnError, OnExit
				);

			if (TempPrc != null && !TempPrc.HasExited)
				TempPrc.WaitForExit(10000);

			if(File.Exists(exe))
				CreatePDBFromExe(exe);

			return TempBuildErrorList.ToArray();
		}

		public bool CreatePDBFromExe(string Executable)
		{
			if (!File.Exists(Executable))
			{
				IDEInterface.Log("Debug information database creation failed - "+Executable+" does not exist");
				return false;
			}

			bool res = false;
			string pdb = Path.ChangeExtension(Executable, ".pdb");

			if (File.Exists(pdb))
				File.Delete(pdb); // Enforce recreation of the database

			IDEInterface.Log("Create debug information database " + pdb);
			try
			{
				var prc = FileExecution.ExecuteSilentlyAsync("cv2pdb.exe", "\"" + Executable + "\"", Path.GetDirectoryName(Executable),
				OnOutput, OnError, delegate()
				{
					if (File.Exists(pdb))
					{
						IDEInterface.Log("Database created successfully");
						res = true;
					}
					else
						TempBuildErrorList.Add(new GenericError()
						{
							Message = "Database creation failed",
							FileName = pdb,
							Type = GenericError.ErrorType.Warning
						});
				});

				if (prc != null && !prc.HasExited)
					prc.WaitForExit(10000); // A time out of 10 seconds should be enough
			}
			catch (Exception ex) { ErrorLogger.Log(ex); }
			return res;
		}

		#region Message Callbacks
		void OnExit()
		{
			IDEInterface.Log("Process ended with code " + TempPrc.ExitCode.ToString());
		}

		static void OnOutput(string s)
		{
			IDEInterface.Log(s);
		}

		void OnError(string s)
		{
			int to = s.IndexOf(".d(");
			if (to < 0)
			{
				TempBuildErrorList.Add(new BuildError(s));
			}
			else
			{
				to += 2;
				string FileName = s.Substring(0, to);
				to += 1;
				int to2 = s.IndexOf("):", to);
				if (to2 < 0) return;
				int lineNumber = Convert.ToInt32(s.Substring(to, to2 - to));
				string errmsg = s.Substring(to2 + 2);

				TempBuildErrorList.Add(new BuildError(errmsg, FileName, new CodeLocation(0, lineNumber)));
			}
		}
		#endregion
	}
}
