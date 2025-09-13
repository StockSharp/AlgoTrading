//+------------------------------------------------------------------+
//|                                                DBcreateTable.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Creates a table in a given database. If the database doesn't exist, it creates it beforehand."
#property script_show_inputs

input string Database = "MQL5Book/DB/Example1";
input string Table = "table1";

#include <MQL5Book/DBSQLite.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   DBSQLite db(Database);
   if(db.isOpen())
   {
      PRTF(db.execute(StringFormat("CREATE TABLE %s (msg text)", Table)));
      // the following modification will not throw error
      // on attempt of creating already existing table
      // PRTF(db.execute(StringFormat("CREATE TABLE IF NOT EXISTS %s (msg text)", Table)));
      PRTF(db.hasTable(Table));
   }
}
//+------------------------------------------------------------------+
/*
   1-st run with default inputs
   
   db.execute(StringFormat(CREATE TABLE %s (msg text),Table))=true / ok
   db.hasTable(Table)=true / ok
   
   2-nd run with default inputs
   
   database error, table table1 already exists
   db.execute(StringFormat(CREATE TABLE %s (msg text),Table))=false / DATABASE_ERROR(5601)
   db.hasTable(Table)=true / ok
*/
//+------------------------------------------------------------------+
