//+------------------------------------------------------------------+
//|                                                   FileStruct.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

#define BARLIMIT 10
#define HEADSIZE 10

const string filename = "MQL5Book/struct.raw";

//+------------------------------------------------------------------+
//| Struct with specific file format header                          |
//+------------------------------------------------------------------+
struct FileHeader
{
   uchar signature[HEADSIZE];
   int n;
   FileHeader(const int size = 0) : n(size)
   {
      static uchar s[HEADSIZE] = {'C','A','N','D','L','E','S','1','.','0'};
      ArrayCopy(signature, s);
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   MqlRates rates[], candles[];
   int n = PRTF(CopyRates(_Symbol, _Period, 0, BARLIMIT, rates)); // 10 / ok
   if(n < 1) return;

   // Part I. Writing
   // create new file or open existing one and empty it
   int handle = PRTF(FileOpen(filename, FILE_BIN | FILE_WRITE)); // 1 / ok
   
   FileHeader fh(n);// prepare header with actual data counter

   // write the header first
   PRTF(FileWriteStruct(handle, fh)); // 14 / ok

   // write the data next
   for(int i = 0; i < n; ++i)
   {
      FileWriteStruct(handle, rates[i], offsetof(MqlRates, tick_volume));
   }
   FileClose(handle);
   ArrayPrint(rates);
   /*
   output for XAUUSD,H1:
                    [time]  [open]  [high]   [low] [close] [tick_volume] [spread] [real_volume]
   [0] 2021.08.16 03:00:00 1778.86 1780.58 1778.12 1780.56          3049        5             0
   [1] 2021.08.16 04:00:00 1780.61 1782.58 1777.10 1777.13          4633        5             0
   [2] 2021.08.16 05:00:00 1777.13 1780.25 1776.99 1779.21          3592        5             0
   [3] 2021.08.16 06:00:00 1779.26 1779.26 1776.67 1776.79          2535        5             0
   [4] 2021.08.16 07:00:00 1776.79 1777.59 1775.50 1777.05          2052        6             0
   [5] 2021.08.16 08:00:00 1777.03 1777.19 1772.93 1774.35          3213        5             0
   [6] 2021.08.16 09:00:00 1774.38 1775.41 1771.84 1773.33          4527        5             0
   [7] 2021.08.16 10:00:00 1773.26 1777.42 1772.84 1774.57          4514        5             0
   [8] 2021.08.16 11:00:00 1774.61 1776.67 1773.69 1775.95          3500        5             0
   [9] 2021.08.16 12:00:00 1775.96 1776.12 1773.68 1774.44          2425        5             0
   */
   
   // Part II. Reading
   handle = PRTF(FileOpen(filename, FILE_BIN | FILE_READ)); // 1 / ok
   FileHeader reference, reader;
   PRTF(FileReadStruct(handle, reader)); // 14 / ok
   // headers must match, otherwise it's not our data
   if(ArrayCompare(reader.signature, reference.signature))
   {
      Print("Wrong file format; 'CANDLES' header is missing");
      return;
   }
   
   PrintFormat("Reading %d candles...", reader.n);
   ArrayResize(candles, reader.n); // allocate memory for reading
   // since we'll read the structs partially,
   // we need to prefill them by zeros
   ZeroMemory(candles);
   
   for(int i = 0; i < reader.n; ++i)
   {
      FileReadStruct(handle, candles[i], offsetof(MqlRates, tick_volume));
   }
   FileClose(handle);
   ArrayPrint(candles);
   /*
   output for XAUUSD,H1:
                    [time]  [open]  [high]   [low] [close] [tick_volume] [spread] [real_volume]
   [0] 2021.08.16 03:00:00 1778.86 1780.58 1778.12 1780.56             0        0             0
   [1] 2021.08.16 04:00:00 1780.61 1782.58 1777.10 1777.13             0        0             0
   [2] 2021.08.16 05:00:00 1777.13 1780.25 1776.99 1779.21             0        0             0
   [3] 2021.08.16 06:00:00 1779.26 1779.26 1776.67 1776.79             0        0             0
   [4] 2021.08.16 07:00:00 1776.79 1777.59 1775.50 1777.05             0        0             0
   [5] 2021.08.16 08:00:00 1777.03 1777.19 1772.93 1774.35             0        0             0
   [6] 2021.08.16 09:00:00 1774.38 1775.41 1771.84 1773.33             0        0             0
   [7] 2021.08.16 10:00:00 1773.26 1777.42 1772.84 1774.57             0        0             0
   [8] 2021.08.16 11:00:00 1774.61 1776.67 1773.69 1775.95             0        0             0
   [9] 2021.08.16 12:00:00 1775.96 1776.12 1773.68 1774.44             0        0             0
   */
}
//+------------------------------------------------------------------+
