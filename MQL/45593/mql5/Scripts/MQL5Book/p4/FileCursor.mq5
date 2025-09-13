//+------------------------------------------------------------------+
//|                                                   FileCursor.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"
#include <MQL5Book/StructPrint.mqh>

const string fileraw = "MQL5Book/cursor.raw";
const string filetxt = "MQL5Book/cursor.csv";
const string file100 = "MQL5Book/k100.raw";

//+------------------------------------------------------------------+
//| Produce a string with file current position, and EOF, EOL flags  |
//+------------------------------------------------------------------+
string FileState(int handle)
{
   return StringFormat("P:%I64d, F:%s, L:%s",
      FileTell(handle),
      (string)FileIsEnding(handle),
      (string)FileIsLineEnding(handle));
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int handle;
   Print("\n * Phase I. Binary file");
   // create new file or open existing one for appending data
   // the file will grow a bit on every start
   handle = PRTF(FileOpen(fileraw, FILE_BIN | FILE_WRITE | FILE_READ));
   Print(FileState(handle));
   // moving file cursor to the end ensures that we will append data,
   // not overwrite it from very beginning
   PRTF(FileSeek(handle, 0, SEEK_END));
   Print(FileState(handle));

   // if file isn't empty, we can read existing data
   // at specific position (relative or absolute)
   // here we try to move for 1 int value back
   if(PRTF(FileSeek(handle, -1 * sizeof(int), SEEK_CUR)))
   {
      Print(FileState(handle));
      // read day_of_year from MqlDateStruct previously stored in the file
      PRTF(FileReadInteger(handle));
   }

   // write current timestamp (datetime is 8 bytes long)
   datetime now = TimeLocal();
   PRTF(FileWriteLong(handle, now));
   Print(FileState(handle));
   // try to step back for 4 bytes and read (this will fail)
   PRTF(FileSeek(handle, -4, SEEK_CUR));
   long x = PRTF(FileReadLong(handle));
   Print(FileState(handle));
   // if step back for 8 bytes, another read will succeed
   PRTF(FileSeek(handle, -8, SEEK_CUR));
   Print(FileState(handle));
   x = PRTF(FileReadLong(handle));
   PRTF((now == x));

   // now write MqlDateTime structure (note how position will change)
   MqlDateTime mdt;
   TimeToStruct(now, mdt);
   StructPrint(mdt);
   PRTF(FileWriteStruct(handle, mdt)); // 32 = sizeof(MqlDateTime)
   Print(FileState(handle));
   FileClose(handle);

   Print(" * Phase II. Text file");
   srand(GetTickCount());
   // create new or open existing text file for writing (re-writing) it
   // from very beginning with CSV-data (Unicode) and then reading
   handle = PRTF(FileOpen(filetxt, FILE_CSV | FILE_WRITE | FILE_READ, ','));
   // '\n' will be replaced with '\r\n' automatically,
   // note that the last element doesn't have a '\n'
   string content = StringFormat(
      "%02d,abc\n%02d,def\n%02d,ghi",
      rand() % 100, rand() % 100, rand() % 100);
   PRTF(FileWriteString(handle, content));
   PRTF(FileSeek(handle, 0, SEEK_SET));
   Print(FileState(handle));
   // let's count lines by FileIsLineEnding flag
   int lineCount = 0;
   while(!FileIsEnding(handle))
   {
      PRTF(FileReadString(handle));
      Print(FileState(handle));
      // FileIsLineEnding is also true if FileIsEnding is true and
      // there is no trailing '\n'
      if(FileIsLineEnding(handle)) lineCount++;
   }
   FileClose(handle);
   PRTF(lineCount);

   Print(" * Phase III. Allocate large file");
   // create new or reset existing file and expand it to 1Mbyte
   handle = PRTF(FileOpen(file100, FILE_BIN | FILE_WRITE));
   PRTF(FileSeek(handle, 1000000, SEEK_END));
   // we need to write something at current position
   PRTF(FileWriteInteger(handle, 0xFF, 1));
   PRTF(FileTell(handle));
   FileClose(handle);
}
//+------------------------------------------------------------------+
/*
   Example outputs
  +-----------------------+
   First run:
   
 * Phase I. Binary file
FileOpen(fileraw,FILE_BIN|FILE_WRITE|FILE_READ)=1 / ok
P:0, F:true, L:false
FileSeek(handle,0,SEEK_END)=true / ok
P:0, F:true, L:false
FileSeek(handle,-1*sizeof(int),SEEK_CUR)=false / INVALID_PARAMETER(4003)
FileWriteLong(handle,now)=8 / ok
P:8, F:true, L:false
FileSeek(handle,-4,SEEK_CUR)=true / ok
FileReadLong(handle)=0 / FILE_READERROR(5015)
P:8, F:true, L:false
FileSeek(handle,-8,SEEK_CUR)=true / ok
P:0, F:false, L:false
FileReadLong(handle)=1629683392 / ok
(now==x)=true / ok
  2021     8    23      1    49    52             1           234
FileWriteStruct(handle,mdt)=32 / ok
P:40, F:true, L:false
 * Phase II. Text file
FileOpen(filetxt,FILE_CSV|FILE_WRITE|FILE_READ,',')=1 / ok
FileWriteString(handle,content)=44 / ok
FileSeek(handle,0,SEEK_SET)=true / ok
P:0, F:false, L:false
FileReadString(handle)=08 / ok
P:8, F:false, L:false
FileReadString(handle)=abc / ok
P:18, F:false, L:true
FileReadString(handle)=37 / ok
P:24, F:false, L:false
FileReadString(handle)=def / ok
P:34, F:false, L:true
FileReadString(handle)=96 / ok
P:40, F:false, L:false
FileReadString(handle)=ghi / ok
P:46, F:true, L:true
lineCount=3 / ok
 * Phase III. Allocate large file
FileOpen(file100,FILE_BIN|FILE_WRITE)=1 / ok
FileSeek(handle,1000000,SEEK_END)=true / ok
FileWriteInteger(handle,0xFF,1)=1 / ok
FileTell(handle)=1000001 / ok

  +-----------------------+
   Second run:

 * Phase I. Binary file
FileOpen(fileraw,FILE_BIN|FILE_WRITE|FILE_READ)=1 / ok
P:0, F:false, L:false
FileSeek(handle,0,SEEK_END)=true / ok
P:40, F:true, L:false
FileSeek(handle,-1*sizeof(int),SEEK_CUR)=true / ok
P:36, F:false, L:false
FileReadInteger(handle)=234 / ok
FileWriteLong(handle,now)=8 / ok
P:48, F:true, L:false
FileSeek(handle,-4,SEEK_CUR)=true / ok
FileReadLong(handle)=0 / FILE_READERROR(5015)
P:48, F:true, L:false
FileSeek(handle,-8,SEEK_CUR)=true / ok
P:40, F:false, L:false
FileReadLong(handle)=1629683397 / ok
(now==x)=true / ok
  2021     8    23      1    49    57             1           234
FileWriteStruct(handle,mdt)=32 / ok
P:80, F:true, L:false
 * Phase II. Text file
FileOpen(filetxt,FILE_CSV|FILE_WRITE|FILE_READ,',')=1 / ok
FileWriteString(handle,content)=44 / ok
FileSeek(handle,0,SEEK_SET)=true / ok
P:0, F:false, L:false
FileReadString(handle)=34 / ok
P:8, F:false, L:false
FileReadString(handle)=abc / ok
P:18, F:false, L:true
FileReadString(handle)=20 / ok
P:24, F:false, L:false
FileReadString(handle)=def / ok
P:34, F:false, L:true
FileReadString(handle)=02 / ok
P:40, F:false, L:false
FileReadString(handle)=ghi / ok
P:46, F:true, L:true
lineCount=3 / ok
 * Phase III. Allocate large file
FileOpen(file100,FILE_BIN|FILE_WRITE)=1 / ok
FileSeek(handle,1000000,SEEK_END)=true / ok
FileWriteInteger(handle,0xFF,1)=1 / ok
FileTell(handle)=1000001 / ok

*/
//+------------------------------------------------------------------+
