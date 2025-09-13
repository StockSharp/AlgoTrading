//+------------------------------------------------------------------+
//|                                               DBquotesImport.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Creates and populates a database table with specific quotes."
#property script_show_inputs

#define PRTF                      // disable bulk logging
#include <MQL5Book/DBSQLite.mqh>
#undef PRTF
#include <MQL5Book/Periods.mqh>
#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input string Database = "MQL5Book/DB/Quotes";
input int TransactionSize = 1000;

//+------------------------------------------------------------------+
//| Example struct with common field types                           |
//+------------------------------------------------------------------+
struct MqlRatesDB: public MqlRates
{
   /* reference:
   
      datetime time;
      double   open;
      double   high;
      double   low;
      double   close;
      long     tick_volume;
      int      spread;
      long     real_volume;
   */

   bool bindAll(DBQuery &q) const
   {
      return q.bind(0, time)
         && q.bind(1, open)
         && q.bind(2, high)
         && q.bind(3, low)
         && q.bind(4, close)
         && q.bind(5, tick_volume)
         && q.bind(6, spread)
         && q.bind(7, real_volume);
   }
   
   long rowid(const long setter = 0)
   {
      // rowid is assigned by the time
      return time;
   }
};

DB_FIELD_C1(MqlRatesDB, datetime, time, DB_CONSTRAINT::PRIMARY_KEY);
DB_FIELD(MqlRatesDB, double, open);
DB_FIELD(MqlRatesDB, double, high);
DB_FIELD(MqlRatesDB, double, low);
DB_FIELD(MqlRatesDB, double, close);
DB_FIELD(MqlRatesDB, long, tick_volume);
DB_FIELD(MqlRatesDB, int, spread);
DB_FIELD(MqlRatesDB, long, real_volume);


//+------------------------------------------------------------------+
//| Read a bunch of MqlRates[] and write to the database             |
//+------------------------------------------------------------------+
bool ReadChunk(DBSQLite &db, const int offset, const int size)
{
   MqlRates rates[];
   MqlRatesDB ratesDB[];
   const int n = CopyRates(_Symbol, PERIOD_CURRENT, offset, size, rates);
   if(n > 0)
   {
      DBTransaction tr(db, true);
      Print(rates[0].time);
      ArrayResize(ratesDB, n);
      for(int i = 0; i < n; ++i)
      {
         ratesDB[i] = rates[i];
      }
      
      return db.insert(ratesDB);
   }
   else
   {
      // normally finishes with HISTORY_NOT_FOUND
      // when maximal number of bars loaded
      // according to terminal settings
      Print("CopyRates failed: ", _LastError, " ", E2S(_LastError));
   }
   return false;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print("");
   DBSQLite db(Database + _Symbol + PeriodToString());
   if(!PRTF(db.isOpen())) return;
   
   // remove the table (if exists)
   PRTF(db.deleteTable(TYPENAME(MqlRatesDB)));
   
   // create empty table
   if(!PRTF(db.createTable<MqlRatesDB>(true))) return;
   
   int offset = 0;
   while(ReadChunk(db, offset, TransactionSize) && !IsStopped())
   {
      offset += TransactionSize;
   }
   
   DBRow *rows[];
   if(db.prepare(StringFormat("SELECT COUNT(*) FROM %s",
      TYPENAME(MqlRatesDB))).readAll(rows))
   {
      Print("Records added: ", rows[0][0].integer_value);
   }
}
//+------------------------------------------------------------------+
/*

   db.isOpen()=true / ok
   db.deleteTable(typename(MqlRatesDB))=true / ok
   db.createTable<MqlRatesDB>(true)=true / ok
   2022.06.29 20:00:00
   2022.05.03 04:00:00
   2022.03.04 10:00:00
   ...
   CopyRates failed: 4401 HISTORY_NOT_FOUND
   Records added: 100000
   
*/
//+------------------------------------------------------------------+
