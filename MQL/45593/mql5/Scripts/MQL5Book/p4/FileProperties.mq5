//+------------------------------------------------------------------+
//|                                               FileProperties.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

const string fileprop = "MQL5Book/fileprop";

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int handle = 0; // incorrect handle (it's not obtained from FileOpen)
   ulong size = FileSize(handle);
   if(_LastError)
   {
      Print("FileSize ", size, ", error=", E2S(_LastError) + "(" + (string)_LastError + ")");
      // Got: FileSize 0, error=WRONG_FILEHANDLE(5008)
   }
   PRTF(FileGetInteger(handle, FILE_SIZE)); // -1 / WRONG_FILEHANDLE(5008)
   
   // create new file or empty existing one
   handle = PRTF(FileOpen(fileprop, FILE_TXT | FILE_WRITE | FILE_ANSI)); // 1
   // write some text
   PRTF(FileWriteString(handle, "Test Text\n")); // 11
   PRTF(FileGetInteger(fileprop, FILE_SIZE)); // 0, not yet written to disk
   PRTF(FileGetInteger(handle, FILE_SIZE)); // 11
   PRTF(FileSize(handle)); // 11
   PRTF(FileGetInteger(handle, FILE_MODIFY_DATE)); // 1629730884 / ok
   PRTF(FileGetInteger(handle, FILE_IS_TEXT)); // 1
   PRTF(FileGetInteger(handle, FILE_IS_BINARY)); // 0
   // the next call will fail, because this property is one of those
   // which are not supported, when requested by filename
   PRTF(FileGetInteger(fileprop, FILE_IS_TEXT)); // -1 / INVALID_PARAMETER(4003)
   Sleep(1000); // wait 1 second to make noticable change in modification time
   FileClose(handle);
   PRTF(FileGetInteger(fileprop, FILE_MODIFY_DATE)); // 1629730885 / ok
   
   PRTF((datetime)FileGetInteger("MQL5Book", FILE_CREATE_DATE));
   // Got: 2021.08.09 22:38:00 / FILE_IS_DIRECTORY(5018)
}
//+------------------------------------------------------------------+
