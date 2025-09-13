//+------------------------------------------------------------------+
//|                                                   FileAtomic.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"
#include <MQL5Book/Periods.mqh>
#include <MQL5Book/FileHandle.mqh>

#define BARLIMIT 10  // number of bars to write into test file
#define TFLEN    3   // length of timeframe name in the file

const string filename = "MQL5Book/atomic.raw";

//+------------------------------------------------------------------+
//| Interface for writing/reading objects to/from files              |
//+------------------------------------------------------------------+
interface Persistent
{
   bool write(int handle);
   bool read(int handle);
};

//+------------------------------------------------------------------+
//| Class with specific file format header                           |
//+------------------------------------------------------------------+
class FileHeader : public Persistent
{
   const string signature;
public:
   FileHeader() : signature("CANDLES/1.1") { }
   bool write(int handle) override
   {
      PRTF(FileWriteString(handle, signature, StringLen(signature))); // 11
      PRTF(FileWriteInteger(handle, StringLen(_Symbol), CHAR_VALUE)); // 1
      PRTF(FileWriteString(handle, _Symbol)); // 6, for XAUUSD
      PRTF(FileWriteString(handle, PeriodToString(), TFLEN)); // 3
      // TODO: check _LastError
      return true;
   }
   bool read(int handle) override
   {
      const string sig = PRTF(FileReadString(handle, StringLen(signature))); // CANDLES/1.1
      if(sig != signature)
      {
         PrintFormat("Wrong file format, header is missing: want=%s vs got %s",
            signature, sig);
         return false;
      }
      const int len = PRTF(FileReadInteger(handle, CHAR_VALUE)); // 6
      const string sym = PRTF(FileReadString(handle, len)); // XAUUSD (example)
      if(_Symbol != sym)
      {
         PrintFormat("Wrong symbol: file=%s vs chart=%s", sym, _Symbol);
         return false;
      }
      const string stf = PRTF(FileReadString(handle, TFLEN)); // H1 (example)
      if(_Period != StringToPeriod(stf))
      {
         PrintFormat("Wrong timeframe: file=%s(%s) vs chart=%s",
            stf, EnumToString(StringToPeriod(stf)), EnumToString(_Period));
         return false;
      }
      return true;
   }
};

//+------------------------------------------------------------------+
//| Simple class to write and read partial rates using files         |
//+------------------------------------------------------------------+
class Candles : public Persistent
{
   FileHeader header;
   int limit;
   MqlRates rates[];
public:
   Candles(const int size = 0) : limit(size)
   {
      if(size == 0) return;
      int n = PRTF(CopyRates(_Symbol, _Period, 0, limit, rates)); // 10
      if(n < 1)
      {
         limit = 0; // init failed
      }
      limit = n; // can be less than requested
   }
   
   bool write(int handle) override
   {
      if(!limit) return false; // no data
      // delegate header writing to the header itself
      if(!header.write(handle)) return false;
      // save record count
      PRTF(FileWriteInteger(handle, limit)); // 4
      // save date range (last, first)
      PRTF(FileWriteLong(handle, rates[0].time)); // 8
      PRTF(FileWriteLong(handle, rates[limit - 1].time)); // 8
      // save array of rates
      for(int i = 0; i < limit; ++i)
      {
         FileWriteStruct(handle, rates[i], offsetof(MqlRates, tick_volume));
      }
      return true;
   }
   
   bool read(int handle) override
   {
      if(!header.read(handle))
      {
         return false;
      }
      limit = PRTF(FileReadInteger(handle)); // 10
      ArrayResize(rates, limit);
      ZeroMemory(rates);
      // dates must be read but are not used so far
      datetime dt0 = (datetime)PRTF(FileReadLong(handle)); // 1629212400 (example)
      datetime dt1 = (datetime)PRTF(FileReadLong(handle)); // 1629248400 (example)

      for(int i = 0; i < limit; ++i)
      {
         FileReadStruct(handle, rates[i], offsetof(MqlRates, tick_volume));
      }
      return true;
   }
   
   void print() const
   {
      ArrayPrint(rates);
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // Part I. Writing
   // create new file or open existing one and empty it
   FileHandle handle(PRTF(FileOpen(filename,
      FILE_BIN | FILE_WRITE | FILE_ANSI | FILE_SHARE_READ))); // 1
   // request rates
   Candles output(BARLIMIT);
   // fill the file with data by persistent object
   if(!output.write(~handle))
   {
      Print("Can't write file");
      return;
   }
   output.print();
   /*
   example output for XAUUSD,H1
                    [time]  [open]  [high]   [low] [close] [tick_volume] [spread] [real_volume]
   [0] 2021.08.17 15:00:00 1791.40 1794.57 1788.04 1789.46          8157        5             0
   [1] 2021.08.17 16:00:00 1789.46 1792.99 1786.69 1789.69          9285        5             0
   [2] 2021.08.17 17:00:00 1789.76 1790.45 1780.95 1783.30          8165        5             0
   [3] 2021.08.17 18:00:00 1783.30 1783.98 1780.53 1782.73          5114        5             0
   [4] 2021.08.17 19:00:00 1782.69 1784.16 1782.09 1782.49          3586        6             0
   [5] 2021.08.17 20:00:00 1782.49 1786.23 1782.17 1784.23          3515        5             0
   [6] 2021.08.17 21:00:00 1784.20 1784.85 1782.73 1783.12          2627        6             0
   [7] 2021.08.17 22:00:00 1783.10 1785.52 1782.37 1785.16          2114        5             0
   [8] 2021.08.17 23:00:00 1785.11 1785.84 1784.71 1785.80           922        5             0
   [9] 2021.08.18 01:00:00 1786.30 1786.34 1786.18 1786.20            13        5             0
   */

   // Part II. Reading
   // open existing file and read it out by persistent object
   // meta-data from leading header drives the following process of reading actual data
   handle = PRTF(FileOpen(filename,
      FILE_BIN | FILE_READ | FILE_ANSI | FILE_SHARE_READ | FILE_SHARE_WRITE)); // 2
   // empty object
   Candles inputs;
   // fill it with data from the file
   if(!inputs.read(~handle))
   {
      Print("Can't read file");
   }
   else
   {
      inputs.print();
   }
   /*
   example output for XAUUSD,H1
                    [time]  [open]  [high]   [low] [close] [tick_volume] [spread] [real_volume]
   [0] 2021.08.17 15:00:00 1791.40 1794.57 1788.04 1789.46             0        0             0
   [1] 2021.08.17 16:00:00 1789.46 1792.99 1786.69 1789.69             0        0             0
   [2] 2021.08.17 17:00:00 1789.76 1790.45 1780.95 1783.30             0        0             0
   [3] 2021.08.17 18:00:00 1783.30 1783.98 1780.53 1782.73             0        0             0
   [4] 2021.08.17 19:00:00 1782.69 1784.16 1782.09 1782.49             0        0             0
   [5] 2021.08.17 20:00:00 1782.49 1786.23 1782.17 1784.23             0        0             0
   [6] 2021.08.17 21:00:00 1784.20 1784.85 1782.73 1783.12             0        0             0
   [7] 2021.08.17 22:00:00 1783.10 1785.52 1782.37 1785.16             0        0             0
   [8] 2021.08.17 23:00:00 1785.11 1785.84 1784.71 1785.80             0        0             0
   [9] 2021.08.18 01:00:00 1786.30 1786.34 1786.18 1786.20             0        0             0
   */
}
//+------------------------------------------------------------------+
