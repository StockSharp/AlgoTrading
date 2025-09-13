//+------------------------------------------------------------------+
//|                                                       DBinit.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Creates a new or opens existing database."
#property script_show_inputs

input string Database = "MQL5Book/DB/Example1";

#include <MQL5Book/DBSQLite.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   DBSQLite db(Database);
   PRTF(db.getHandle());                    // 65537 / ok
   PRTF(FileIsExist(Database + ".sqlite")); // true / ok
}
//+------------------------------------------------------------------+
