//+------------------------------------------------------------------+
//|                                      DBcreateTableFromStruct.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Creates a table based on struct declaration. If the database doesn't exist, it creates it beforehand."
#property script_show_inputs

#include <MQL5Book/DBSQLite.mqh>

input string Database = "MQL5Book/DB/Example1";

//+------------------------------------------------------------------+
//| Example struct with common field types                           |
//+------------------------------------------------------------------+
struct Struct
{
   long id;
   string name;
   double income;
   datetime time;
};

// Unfortunately we can't declare the fields once (inside the struct)
// because every macro (see below) creates a templated struct underneath,
// which is, because of nesting, makes the holding struct
// complex and incompatible with DB-binding.

// NB: if PRIMARY_KEY constaint is not specified for one of integer fields,
// implicit 'rowid' column will be added as primary key automatically by SQL

DB_FIELD_C1(Struct, long, id, DB_CONSTRAINT::PRIMARY_KEY);
DB_FIELD(Struct, string, name);
DB_FIELD(Struct, double, income);
DB_FIELD(Struct, string, time);

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   DBSQLite db(Database);
   if(db.isOpen())
   {
      PRTF(db.createTable<Struct>());
      // the following modification will not throw error
      // on attempt of creating already existing table
      // PRTF(db.createTable<Struct>(true));
      PRTF(db.hasTable(TYPENAME(Struct)));
   }
}
//+------------------------------------------------------------------+
/*
   1-st run with default inputs
   
   sql=CREATE TABLE  Struct (id INTEGER PRIMARY KEY,
   name TEXT ,
   income REAL ,
   time TEXT ); / ok
   db.createTable<Struct>()=true / ok
   db.hasTable(typename(Struct))=true / ok
   
   2-nd run with default inputs
   
   sql=CREATE TABLE  Struct (id INTEGER PRIMARY KEY,
   name TEXT ,
   income REAL ,
   time TEXT ); / ok
   database error, table Struct already exists
   db.createTable<Struct>()=false / DATABASE_ERROR(5601)
   db.hasTable(typename(Struct))=true / ok
   
*/
//+------------------------------------------------------------------+
