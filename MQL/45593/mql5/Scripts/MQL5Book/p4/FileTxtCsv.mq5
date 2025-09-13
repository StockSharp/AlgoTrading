//+------------------------------------------------------------------+
//|                                                   FileTxtCsv.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"
#include <MQL5Book/StringUtils.mqh>

#define LIMIT 10

const string txtfile = "MQL5Book/atomic.txt";
const string csvfile = "MQL5Book/atomic.csv";
const short delimiter = ',';

//+------------------------------------------------------------------+
//| Simple MqlRates-wrapper for setting data by field number         |
//+------------------------------------------------------------------+
struct MqlRatesM : public MqlRates
{
   template<typename T>
   void set(int field, T v)
   {
      switch(field)
      {
         case 0:
           this.time = (datetime)v;
           break;
         case 1:
           this.open = (double)v;
           break;
         case 2:
           this.high = (double)v;
           break;
         case 3:
           this.low = (double)v;
           break;
         case 4:
           this.close = (double)v;
           break;
         case 5:
           this.tick_volume = (long)v;
           break;
         case 6:
           this.spread = (int)v;
           break;
         case 7:
           this.real_volume = (long)v;
           break;
      }
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   MqlRates rates[];   
   int n = PRTF(CopyRates(_Symbol, _Period, 0, LIMIT, rates)); // 10

   const string columns[] = {"DateTime", "Open", "High", "Low", "Close", "Ticks", "Spread", "True"};
   // last column name can be interpreted as "True Volume" and is used for FileReadBool
   const string caption = StringCombine(columns, delimiter) + "\r\n";

   // Part I. Writing TXT and CSV
   // create new file or open existing one and empty it
   int fh1 = PRTF(FileOpen(txtfile, FILE_TXT | FILE_ANSI | FILE_WRITE, delimiter)); // 1
   int fh2 = PRTF(FileOpen(csvfile, FILE_CSV | FILE_ANSI | FILE_WRITE, delimiter)); // 2

   // PRTF(FileWriteArray(fh1, rates)); // FILE_NOTBIN(5011)
   
   PRTF(FileWriteString(fh1, caption));
   PRTF(FileWriteString(fh2, caption));

   for(int i = 0; i < n; ++i)
   {
      FileWrite(fh1, rates[i].time,
         rates[i].open, rates[i].high, rates[i].low, rates[i].close,
         rates[i].tick_volume, rates[i].spread, rates[i].real_volume);
      FileWrite(fh2, rates[i].time,
         rates[i].open, rates[i].high, rates[i].low, rates[i].close,
         rates[i].tick_volume, rates[i].spread, rates[i].real_volume);
   }
   
   FileClose(fh1);
   FileClose(fh2);

   // Part II. Reading TXT and CSV with FileReadString
   // open just written files for reading and testing
   string read;
   fh1 = PRTF(FileOpen(txtfile, FILE_TXT | FILE_ANSI | FILE_READ, delimiter)); // 1
   fh2 = PRTF(FileOpen(csvfile, FILE_CSV | FILE_ANSI | FILE_READ, delimiter)); // 2

   Print("===== Reading TXT");
   do
   {
      read = PRTF(FileReadString(fh1));
   }
   while(StringLen(read) > 0);
   
   Print("===== Reading CSV");
   do
   {
      read = PRTF(FileReadString(fh2));
   }
   while(StringLen(read) > 0);
   
   FileClose(fh1);
   FileClose(fh2);

   // Part III. Reading CSV with specific functions
   // FileReadDatetime, FileReadNumber, FileReadBool
   Print("===== Reading CSV (alternative)");
   MqlRatesM r[1];// declare as array to print the struct easily by ArrayPrint
   int count = 0; // number of records read
   int column = 0;// monitor current column to change convertion types
   const int maxColumn = ArraySize(columns);
   fh2 = PRTF(FileOpen(csvfile, FILE_CSV | FILE_ANSI | FILE_READ, delimiter)); // 1
   do
   {
      // Note: first record is not a data, it holds column titles
      if(column)
      {
         if(count == 1) // demonstrate FileReadBool on 1-st record with titles
         {
            r[0].set(column, PRTF(FileReadBool(fh2)));
         }
         else
         {
            r[0].set(column, FileReadNumber(fh2));
         }
      }
      else // 0-th column is a datetime
      {
         ++count;
         if(count > 1) // struct from previous line is ready
         {
            ArrayPrint(r, _Digits, NULL, 0, 1, 0);
         }
         r[0].time = FileReadDatetime(fh2);
      }
      column = (column + 1) % maxColumn;
   }
   while(_LastError == 0); // normal termination implies 5027 (FILE_ENDOFFILE)
   
   // last struct print
   if(column == maxColumn - 1)
   {
      ArrayPrint(r, _Digits, NULL, 0, 1, 0);
   }
}
//+------------------------------------------------------------------+
