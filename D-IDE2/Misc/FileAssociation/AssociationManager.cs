/*
* Copyright (c) 2006, Brendan Grant (grantb@dahat.com)
* All rights reserved.
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
*     * All original and modified versions of this source code must include the
*       above copyright notice, this list of conditions and the following
*       disclaimer.
*     * This code may not be used with or within any modules or code that is 
*       licensed in any way that that compels or requires users or modifiers
*       to release their source code or changes as a requirement for
*       the use, modification or distribution of binary, object or source code
*       based on the licensed source code. (ex: Cannot be used with GPL code.)
*     * The name of Brendan Grant may be used to endorse or promote products
*       derived from this software without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY BRENDAN GRANT ``AS IS'' AND ANY EXPRESS OR
* IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
* OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO
* EVENT SHALL BRENDAN GRANT BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
* SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
* PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; 
* OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
* WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
* OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
* ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace BrendanGrant.Helpers.FileAssociation
{
	/// <summary>
	/// Provides more streamlined interface for associating a single or multiple extensions with a single program.
	/// </summary>
	public class AssociationManager
	{
		public static bool IsAssociated(string progId, string extension)
		{
			var fai = new FileAssociationInfo(extension);

			return fai.Exists && fai.ProgID == progId;
		}

		public static void RemoveAssociation(string extension)
		{
			var fai = new FileAssociationInfo(extension);

			if (fai.Exists)
				fai.Delete();
		}

		public static void RemoveProgramInfo(string progId)
		{
			var pai = new ProgramAssociationInfo(progId);

			if (pai.Exists)
				pai.Delete();
		}

		/// <summary>
		/// Associates a single executable with a list of extensions.
		/// </summary>
		/// <param name="progId">Name of program id</param>
		/// <param name="executablePath">Path to executable to start including arguments.</param>
		/// <param name="extensions">String array of extensions to associate with program id.</param>
		/// <example>progId = "MyTextFile"
		/// executablePath = "notepad.exe %1"
		/// extensions = ".txt", ".text"</example>
		public static void Associate(string progId, string executablePath, params string[] extensions)
		{
			Associate(progId, extensions);

			var pai = new ProgramAssociationInfo(progId);

			if (!pai.Exists)
				pai.Create();

			pai.AddVerb(new ProgramVerb("open", executablePath+" \"%1\"")); // Note: the %1 ensures that the opened file will be passed to the program's command line
		}

		/// <summary>
		/// Associates an already existing program id with a list of extensions.
		/// </summary>
		/// <param name="progId">The program id to associate extensions with.</param>
		/// <param name="extensions">String array of extensions to associate with program id.</param>
		public static void Associate(string progId, params string[] extensions)
		{
			foreach (string s in extensions)
			{
				FileAssociationInfo fai = new FileAssociationInfo(s);

				if (!fai.Exists)
					fai.Create(progId);

				fai.ProgID = progId;
			}
		}

	}
}
