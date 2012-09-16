#pragma once

#include "stdafx.h"
#include "dbghelp.h"
#include <windows.h>

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Collections::Generic;
using namespace System::Windows::Forms;

namespace DebugEngineWrapper
{
	/*public*/ ref class SymbolExtracter
	{
	public:
		IMAGE_FILE_HEADER* header;
		IMAGE_OPTIONAL_HEADER* opt_header;
		array<IMAGE_SECTION_HEADER*>^ Sections;

		List<String^>^ LibrariesUsed;
		List<CodeViewModule^>^ Modules;

		SymbolExtracter()
		{

		}

		ULONG64 RVAToFileOffset(ULONG64 Offset)
		{
			for(int i=0;i<Sections->Length;i++)
			{
				if(Offset>=Sections[i]->VirtualAddress && Offset<=Sections[i]->VirtualAddress+Sections[i]->Misc.VirtualSize)
					return Offset-Sections[i]->VirtualAddress+Sections[i]->PointerToRawData;
			}
			return 0;
		}

		bool DoExtract(String^ binFile)
		{
			if(!File::Exists(binFile)) return false;

			FileStream^ fs=gcnew FileStream(binFile,FileMode::Open,FileAccess::Read,FileShare::Read);
			System::IO::BinaryReader^ br=gcnew BinaryReader(fs);

#pragma region Read out header data
			UINT pe_offset=0,pe_sig=0;
			array<unsigned char>^ tbuf;
			UINT headersize=0;

			// Skip to the offset of the PE signature
			fs->Seek(0x3c,SeekOrigin::Begin);
			pe_offset=br->ReadUInt32();

			// Skip to the PE signature which should be "PE\0\0"
			fs->Seek(pe_offset,SeekOrigin::Begin);
			pe_sig=br->ReadUInt32();
			if ( pe_sig != 0x4550 )
				throw gcnew Exception("File is not an COFF PE executable");

			// Now the IMAGE_FILE_HEADER follows immediately
			tbuf=br->ReadBytes(sizeof(IMAGE_FILE_HEADER));
			pin_ptr<void> ptr = &tbuf[0];
			header=reinterpret_cast<IMAGE_FILE_HEADER*>(ptr);
			if ( header->Machine != 0x14c )	
				throw gcnew Exception("Unsupported machine Id in COFF file");

			// And the optional image header
			headersize = header->SizeOfOptionalHeader- sizeof(IMAGE_OPTIONAL_HEADER);
			tbuf=br->ReadBytes(sizeof(IMAGE_OPTIONAL_HEADER));
			ptr = &tbuf[0];
			opt_header=reinterpret_cast<IMAGE_OPTIONAL_HEADER*>(ptr);
			if ( opt_header->Magic != IMAGE_NT_OPTIONAL_HDR_MAGIC )
				throw gcnew Exception("Unknown optional header magic");

			// Read out the sections - they'll be needed for locating file offsets via virtual addresses
			Sections=gcnew array<IMAGE_SECTION_HEADER*>(header->NumberOfSections);
			for(int i=0;i<header->NumberOfSections;i++)
			{
				tbuf=br->ReadBytes(sizeof(IMAGE_SECTION_HEADER));
				ptr = &tbuf[0];
				Sections[i]=reinterpret_cast<IMAGE_SECTION_HEADER*>(ptr);
			}

			// Seek the IMAGE_DIRECTORY_ENTRY_DEBUG entry
			br->BaseStream->Seek(RVAToFileOffset(opt_header->DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].VirtualAddress),SeekOrigin::Begin);
			ULONG NumberOfDirEntries=opt_header->DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].Size/sizeof(IMAGE_DEBUG_DIRECTORY);
			tbuf=br->ReadBytes(opt_header->DataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG].Size);
			ptr = &tbuf[0];

			// Parse each directory entry for symbols
			for(UINT i=0;i<NumberOfDirEntries;i++)
			{
				if(reinterpret_cast<IMAGE_DEBUG_DIRECTORY*>((&ptr)[i])->Type==IMAGE_DEBUG_TYPE_CODEVIEW)
				{
					ParseCV(reinterpret_cast<IMAGE_DEBUG_DIRECTORY*>((&ptr)[i]),br);
				}
			}

#pragma endregion
			// Release all handles
			fs->Close();

			return true;
		}


		static UINT intFromStr(char* str)
		{
			UINT ret;
			for (int i=strlen(str);i>=0;i--)
				ret = ret<<8 | str[i];
			return ret;
		}

	protected:
#pragma region Parse Code View Sections
		static UINT cv_nb09_sig=intFromStr("NB09");

		void ParseCV(IMAGE_DEBUG_DIRECTORY* Header,BinaryReader^ br)
		{
			LibrariesUsed=gcnew List<String^>();	
			Modules=gcnew List<CodeViewModule^>();	

			ULONG64 oldoffset=br->BaseStream->Position;

			// Head to the section data
			br->BaseStream->Seek(Header->PointerToRawData,SeekOrigin::Begin);

			// Store CV essential Base address
			// Note: All offset given by CV are related to lfaBase!
			ULONG64 lfaBase=Header->PointerToRawData;
#define ToAbsOffset(a) (lfaBase+a)
			array<UCHAR>^ tbuf;
			array<UCHAR>^ tbuf2;
			pin_ptr<UCHAR> ptr,ptr2;
			SubsectionHeader* dirhdr;

			// Read out CV signature
			UINT sig=br->ReadUInt32();
			if(sig!=cv_nb09_sig)
				throw gcnew Exception("Unsupported CodeView Version");

			// lfo = Long File Offset that is related from lfaBase
			UINT lfoDir=br->ReadUInt32();
			br->BaseStream->Seek(ToAbsOffset(lfoDir),SeekOrigin::Begin);

			// Read out subsection header
			tbuf=br->ReadBytes(sizeof(SubsectionHeader));
			pin_ptr<UCHAR> dirhptr=&tbuf[0];
			dirhdr=reinterpret_cast<SubsectionHeader*>(dirhptr);

			// Read out subsection entries
			array<SubsectionTableEntry*>^ secs=gcnew array<SubsectionTableEntry*>(dirhdr->SubsectionCount);
			for(UINT i=0;i<dirhdr->SubsectionCount;i++)
			{
				tbuf=br->ReadBytes(dirhdr->EntrySize);
				ptr=&tbuf[0];
				SubsectionTableEntry* ent=reinterpret_cast<SubsectionTableEntry*>(ptr);
				secs[i]=ent;
			}

			// Parse these sections
			for(UINT i=0;i<dirhdr->SubsectionCount;i++)
			{
				SubsectionTableEntry* ent=secs[i];

				// Head to the subsection data
				br->BaseStream->Seek(ToAbsOffset(ent->DataOffset),SeekOrigin::Begin);

				switch ( ent->subsection )
				{
				case sstModule:
					{
						// Read module header
						tbuf2=br->ReadBytes(sizeof(ModuleHeader));
						pin_ptr<void> modptr=&tbuf2[0];
						ModuleHeader*	modhdr=reinterpret_cast<ModuleHeader*>(modptr);

						if(modhdr->Style!=intFromStr("CV"))
						{
							throw gcnew Exception("Wrong CV Module Header format!");
						}

						CodeViewModule^ mod=gcnew CodeViewModule(modhdr);

						// Read segment info
						for(UINT j=0;j<modhdr->SegmentCount;j++)
						{
							tbuf2=br->ReadBytes(sizeof(SegInfo));
							pin_ptr<void> tptr=&tbuf2[0];
							mod->SegmentInfo[j]=reinterpret_cast<SegInfo*>(tptr);
						}

						// Read length and the name
						BYTE len=br->ReadByte();
						tbuf2=br->ReadBytes(len);
						ptr2=&tbuf2[0];
						mod->Name=gcnew String((char*)ptr2,0,len);

						Modules->Add(mod);
						break;
					}
				case sstAlignSym:
					/*debug(cvsections) DbgIO.println("sstAlignSym section");
					assert ( entry.iMod <= cv.modulesByIndex.length );
					Module mod = cv.modulesByIndex[entry.iMod-1];
					parseSymbols(cv, section_data, mod.symbols, true, mod);*/
					break;
				case sstSrcModule:
					/*debug(cvsections) DbgIO.println("sstSrcModule section");
					assert( entry.iMod <= cv.modulesByIndex.length );
					parseSrcModule(cv, section_data, cv.modulesByIndex[entry.iMod-1]);*/
					break;
				case sstLibraries: // Read out all libs that are imported while compiling
					{
						// Skip \0 char that represents the first entry
						br->ReadByte();
						UINT j=1; // set it to 1 because we already read the initial \0
						while(j<ent->SectionSize)
						{
							BYTE len=br->ReadByte(); // Read out the string length
							if(len<1) break;
							tbuf2=br->ReadBytes(len);
							ptr2=&tbuf2[0];
							LibrariesUsed->Add(gcnew String((char*)ptr2,0,len));
							j+=1+len;
						}
						break;
					}
				case sstGlobalSym:
					//parsePackedSymbols(cv, section_data, cv.global_sym);
					break;
				case sstGlobalPub:
					//parsePackedSymbols(cv, section_data, cv.global_pub);
					break;
				case sstStaticSym:
					//parsePackedSymbols(cv, section_data, cv.static_sym);
					break;
				case sstGlobalTypes:
					//parseGlobalTypes(cv, section_data, entry.lfo);
					break;
				case sstSegMap:
					break;
				case sstSegName:
					break;
				case sstFileIndex:
					break;
				case sstSymbols:
				case sstTypes:
				case sstPublic:
				case sstPublicSym:
				case sstSrcLnSeg:
				case sstMPC:
				case sstPreComp:
				case sstPreCompMap:
				case sstOffsetMap16:
				case sstOffsetMap32:

					break;
				default:
					break;
				}
			}

			br->BaseStream->Seek(oldoffset,SeekOrigin::Begin);
		}
#pragma endregion
	};
}