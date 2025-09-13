//+------------------------------------------------------------------+
//|                                                   FileSelect.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   string filenames[]; // dynamic array: adaptive size
   string fixed[1]; // too short array for many files
   const string filter = // a couple of examples
      "Text documents (*.txt)|*.txt"
      "|Files with short names|????.*"
      "|All files (*.*)|*.*";
   
   // if nothing selected, script exists instantly
   // otherwise it proceeds to the next steps
   // (even on errors for demo purposes)

   // open a single file for reading
   Print("Open a file");
   if(PRTF(FileSelectDialog(NULL, "MQL5book", filter,
      0, filenames, NULL)) == 0) return;
   ArrayPrint(filenames);

   // open a single file for writing (possibly a new file)
   Print("Save as a file");
   if(PRTF(FileSelectDialog(NULL, "MQL5book", NULL,
      FSD_WRITE_FILE, filenames, NULL)) == 0) return;
   ArrayPrint(filenames);
   
   // open multiple files for reading (dynamic array)
   Print("Open multiple files (dynamic)");
   if(PRTF(FileSelectDialog(NULL, "MQL5book", NULL,
      FSD_FILE_MUST_EXIST | FSD_ALLOW_MULTISELECT, filenames, NULL)) == 0) return;
   ArrayPrint(filenames);

   // open multiple files for reading (fixed array)
   Print("Open multiple files (fixed, choose more than 1 file for error)");
   if(PRTF(FileSelectDialog(NULL, "MQL5book", NULL,
      FSD_FILE_MUST_EXIST | FSD_ALLOW_MULTISELECT, fixed, NULL)) == 0) return;
   ArrayPrint(fixed);

   // select a folder
   Print("Select a folder");
   if(PRTF(FileSelectDialog(NULL, "MQL5book/nonexistent", NULL,
      FSD_SELECT_FOLDER, filenames, NULL)) == 0) return;
   ArrayPrint(filenames);

   // incorrect flag combination can produce an error
   Print("Select a folder");
   if(PRTF(FileSelectDialog(NULL, "MQL5book", NULL,
      FSD_SELECT_FOLDER | FSD_WRITE_FILE, filenames, NULL)) == 0) return;
   // on error input/output array does not change
   ArrayPrint(filenames);
}
//+------------------------------------------------------------------+
/*
   Output (example)
   
   Open a file
   FileSelectDialog(NULL,MQL5book,filter,0,filenames,NULL)=1 / ok
   "MQL5Book\utf8.txt"
   Save as a file
   FileSelectDialog(NULL,MQL5book,NULL,FSD_WRITE_FILE,filenames,NULL)=1 / ok
   "MQL5Book\newfile"
   Open multiple files (dynamic)
   FileSelectDialog(NULL,MQL5book,NULL,FSD_FILE_MUST_EXIST|FSD_ALLOW_MULTISELECT,filenames,NULL)=5 / ok
   "MQL5Book\ansi1252.txt" "MQL5Book\unicode1.txt" "MQL5Book\unicode2.txt" "MQL5Book\unicode3.txt" "MQL5Book\utf8.txt"    
   Open multiple files (fixed, choose more than 1 file for error)
   FileSelectDialog(NULL,MQL5book,NULL,FSD_FILE_MUST_EXIST|FSD_ALLOW_MULTISELECT,fixed,NULL)=-1 / ARRAY_RESIZE_ERROR(4007)
   null
   Select a folder
   FileSelectDialog(NULL,MQL5book/nonexistent,NULL,FSD_SELECT_FOLDER,filenames,NULL)=1 / ok
   "MQL5Book"
   Select a folder
   FileSelectDialog(NULL,MQL5book,NULL,FSD_SELECT_FOLDER|FSD_WRITE_FILE,filenames,NULL)=-1 / INTERNAL_ERROR(4001)
   "MQL5Book"
   
*/
//+------------------------------------------------------------------+
