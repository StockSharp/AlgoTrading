//+------------------------------------------------------------------+
//|                                   DBfillTableFromStructArray.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Creates and populates a table based on struct declaration. If the database doesn't exist, it creates it beforehand."
#property script_show_inputs

#resource "\\Images\\euro.bmp"
#resource "\\Images\\dollar.bmp"

#include <MQL5Book/DBSQLite.mqh>

input string Database = "MQL5Book/DB/Example2";
input string EURUSD = "EURUSD";
input string USDCNH = "USDCNH";
input string USDJPY = "USDJPY";

//+------------------------------------------------------------------+
//| Example struct with common field types                           |
//+------------------------------------------------------------------+
struct Struct
{
   long id;
   string name;
   double number;
   datetime timestamp;
   string image;   // assume filename or resource name on input
   // this will be defined as BLOB in the table,
   // on output we use string only for partial logging;
   // arrays are not supported in DB-bound structs,
   // so string is only chance to detect non-null blobs,
   // to get actual blob data call DBRow::getBlob

   // used to insert/update table record based on the object
   bool bindAll(DBQuery &q) const
   {
      uint pixels[] = {};
      uint w, h;
      if(StringLen(image))
      {
         if(StringFind(image, "::") == 0)
         {
            ResourceReadImage(image, pixels, w, h);
            FileSave(StringSubstr(image, 2) + ".raw", pixels); // debug output (not a BMP, no header)
         }
         else
         {
            const string res = "::" + image;
            ResourceCreate(res, image);
            ResourceReadImage(res, pixels, w, h);
            ResourceFree(res);
         }
      }
      return (id == 0 ? q.bindNull(0) : q.bind(0, id)) // if id is NULL, it will get new rowid
         && q.bind(1, name)
         && q.bind(2, number)
         // && q.bind(3, timestamp) // this is handled by CURRENT_TIMESTAMP
         && q.bindBlob(4, pixels);
   }
   
   // used for single record selection in update/delete, and array insertion
   long rowid(const long setter = 0)
   {
      if(setter) id = setter;
      return id;
   }
};

// NB: if PRIMARY_KEY constraint is not specified for one of integer fields,
// implicit 'rowid' column will be added as primary key automatically by SQL;
// this does even spare 1 byte per record!

DB_FIELD_C1(Struct, long, id, DB_CONSTRAINT::PRIMARY_KEY);
DB_FIELD(Struct, string, name);
DB_FIELD(Struct, double, number);
DB_FIELD_C1(Struct, datetime, timestamp, DB_CONSTRAINT::CURRENT_TIMESTAMP);
DB_FIELD(Struct, blob, image);

Struct demo[] =
{
   {0, "dollar", 1.0, 0, "::Images\\dollar.bmp"},
   {0, "euro", SymbolInfoDouble(EURUSD, SYMBOL_ASK), 0, "::Images\\euro.bmp"},
   {0, "yuan", 1.0 / SymbolInfoDouble(USDCNH, SYMBOL_BID), 0, NULL},
   {0, "yen", 1.0 / SymbolInfoDouble(USDJPY, SYMBOL_BID), 0, NULL},
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print("");
   DBSQLite db(Database);
   if(!PRTF(db.isOpen())) return;
   
   // remove the table (if exists from previous run)
   PRTF(db.deleteTable(TYPENAME(Struct)));
   
   // create empty table
   if(!PRTF(db.createTable<Struct>(true))) return;

   // insertion of array of objects
   db.insert(demo); // this assigns rowid's inside objects
   /*
   // alternative with separate calls per object
   for(int i = 0; i < ArraySize(demo); ++i)
   {
      PRTF(db.insert(demo[i])); // this returns rowid on every call
   }
   */
   ArrayPrint(demo);
   
   // retrieve a couple of records with no images
   DBQuery *query = db.prepare(StringFormat("SELECT * FROM %s WHERE image IS NULL",
      TYPENAME(Struct)));

   // approach 1: via specific struct
   Struct result[];
   PRTF(query.readAll(result));
   ArrayPrint(result);
   
   query.reset();
   
   // approach 2: via universal DBRow container
   DBRow *rows[];
   query.readAll(rows);
   for(int i = 0; i < ArraySize(rows); ++i)
   {
      Print(i);
      MqlParam fields[];
      rows[i].readAll(fields);
      ArrayPrint(fields);
   }
   
   Print("Pause...");
   Sleep(1000);       // wait to make it clear how timestamp changes on update
   
   // update both records: apply new image
   for(int i = 0; i < ArraySize(result); ++i)
   {
      result[i].image = "yuan.bmp";
      db.update(result[i]);
   }
   
   // demonstrate blob reading for specific record
   // first, show how Blob is mapped into standard string field in object
   // (binary content causes problems)
   const long id1 = 1;
   Struct s;
   if(db.read(id1, s))
   {
      Print("Length of string with Blob: ", StringLen(s.image));
      Print(s.image); // just a demo - don't print binary data into the log
   }
   
   // second, show how exact binary content is retrieved from DB table
   DBRow *r;
   if(db.read(id1, r, "Struct"))
   {
      uchar bytes[];
      Print("Actual size of Blob: ", r.getBlob("image", bytes));
      FileSave("temp.bmp.raw", bytes); // not a BMP, no header
      // you should get temp.bmp.raw which is equal to MQL5/Files/Images/dollar.bmp.raw
   }
   
   // uncomment this to test "DELETE FROM TABLE" query for specific object/record
   // db.remove(s);
}
//+------------------------------------------------------------------+
/*
   1-st run with default inputs
   
   db.isOpen()=true / ok
   db.deleteTable(typename(Struct))=true / ok
   sql=CREATE TABLE IF NOT EXISTS Struct (id INTEGER PRIMARY KEY,
   name TEXT ,
   number REAL ,
   timestamp INTEGER CURRENT_TIMESTAMP,
   image BLOB ); / ok
   db.createTable<Struct>(true)=true / ok
   sql=INSERT INTO 'Struct' VALUES(?1,?2,?3,STRFTIME('%s'),?5) RETURNING rowid; / ok
   DatabasePrepare(db,sql)=131073 / ok
   DatabaseBindArray(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBindArray(handle,index,value)=true / ok
   DatabaseRead(query)=true / ok
   DatabaseBindArray(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBindArray(handle,index,value)=true / ok
   DatabaseRead(query)=true / ok
   DatabaseBindArray(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBindArray(handle,index,value)=true / ok
   DatabaseRead(query)=true / ok
   DatabaseBindArray(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBindArray(handle,index,value)=true / ok
   DatabaseRead(query)=true / ok
       [id]   [name] [number]         [timestamp]               [image]
   [0]    1 "dollar"  1.00000 1970.01.01 00:00:00 "::Images\dollar.bmp"
   [1]    2 "euro"    1.00402 1970.01.01 00:00:00 "::Images\euro.bmp"  
   [2]    3 "yuan"    0.14635 1970.01.01 00:00:00 null                 
   [3]    4 "yen"     0.00731 1970.01.01 00:00:00 null                 
   DatabasePrepare(db,sql)=196609 / ok
   DatabaseReadBind(query,object)=true / ok
   DatabaseReadBind(query,object)=true / ok
   DatabaseReadBind(query,object)=false / DATABASE_NO_MORE_DATA(5126)
   query.readAll(result)=true / ok
       [id] [name] [number]         [timestamp] [image]
   [0]    3 "yuan"  0.14635 2022.08.20 13:14:38 null   
   [1]    4 "yen"   0.00731 2022.08.20 13:14:38 null   
   DatabaseRead(query)=true / ok
   DatabaseRead(query)=true / ok
   DatabaseRead(query)=false / DATABASE_NO_MORE_DATA(5126)
   0
       [type] [integer_value] [double_value] [string_value]
   [0]      4               3        0.00000 null          
   [1]     14               0        0.00000 "yuan"        
   [2]     13               0        0.14635 null          
   [3]     10      1661001278        0.00000 null          
   [4]      0               0        0.00000 null          
   1
       [type] [integer_value] [double_value] [string_value]
   [0]      4               4        0.00000 null          
   [1]     14               0        0.00000 "yen"         
   [2]     13               0        0.00731 null          
   [3]     10      1661001278        0.00000 null          
   [4]      0               0        0.00000 null          
   Pause...
   sql=UPDATE 'Struct' SET (id,name,number,timestamp,image)=(?1,?2,?3,STRFTIME('%s'),?5) WHERE rowid=3; / ok
   DatabasePrepare(db,sql)=262145 / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBindArray(handle,index,value)=true / ok
   DatabaseRead(handle)=false / DATABASE_NO_MORE_DATA(5126)
   sql=UPDATE 'Struct' SET (id,name,number,timestamp,image)=(?1,?2,?3,STRFTIME('%s'),?5) WHERE rowid=4; / ok
   DatabasePrepare(db,sql)=327681 / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBind(handle,index,value)=true / ok
   DatabaseBindArray(handle,index,value)=true / ok
   DatabaseRead(handle)=false / DATABASE_NO_MORE_DATA(5126)
   sql=SELECT * FROM 'Struct' WHERE rowid=1; / ok
   DatabasePrepare(db,sql)=393217 / ok
   DatabaseReadBind(query,object)=true / ok
   Length of string with Blob: 922
   ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ɬ7�ȫ6�ũ6�Ĩ5���5�¦5�Ĩ5�ƪ6�ȫ6�Ȭ7�ɬ7�ɬ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7�ʭ7��҉��֒��ٛ��ܣ�...
   sql=SELECT * FROM 'Struct' WHERE rowid=1; / ok
   DatabasePrepare(db,sql)=458753 / ok
   DatabaseRead(query)=true / ok
   Actual size of Blob: 4096
   
   [// option
      sql=DELETE FROM 'Struct' WHERE rowid=1; / ok
      DatabasePrepare(db,sql)=524289 / ok
      DatabaseRead(handle)=false / DATABASE_NO_MORE_DATA(5126)
   ]
   
*/
//+------------------------------------------------------------------+
