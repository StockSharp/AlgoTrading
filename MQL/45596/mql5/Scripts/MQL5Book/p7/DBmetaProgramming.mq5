//+------------------------------------------------------------------+
//|                                            DBmetaProgramming.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Example of extraction of data type meta-information. Unfortunately it's not compatible with SQL binding facilities in MQL5."

#include <MQL5Book/Defines.mqh>
#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/TypeName.mqh>

//+------------------------------------------------------------------+
//| Meta-information about DB table record based on object prototype |
//+------------------------------------------------------------------+
template<typename S>
struct DBEntity
{
   template<typename T>
   struct DBField
   {
      T f;
      DBField(const string name, const string constraints = "")
      {
         Print(__FUNCSIG__);
         Print(TYPENAME(T), " ", name);
         const int n = EXPAND(prototype);
         prototype[n][0] = TYPENAME(T);
         prototype[n][1] = name;
         prototype[n][2] = constraints;
      }
   };
   
   static string prototype[][3]; // 0 - type, 1 - name, 2 - constraint
};

template<typename S>
static string DBEntity::prototype[][3];

#define DB_FIELD(T,N)    struct T##_##N: DBField<T> { T##_##N() : DBField<T>(#N) { } } _##T##_##N;
/*
   DB_FIELD expands to:
   
   struct Type: DBField<Type>
   {
      Type_Name() : DBField<Type>(Name) { }
   } _Type_Name;
   
*/

//+------------------------------------------------------------------+
//| Original test struct                                             |
//+------------------------------------------------------------------+
struct Data
{
   long id;
   string name;
   datetime timestamp;
   double income;
};

//+------------------------------------------------------------------+
//| Original test struct                                             |
//+------------------------------------------------------------------+
struct DataDB: public DBEntity<DataDB>
{
   DB_FIELD(long, id);
   DB_FIELD(string, name);
   DB_FIELD(datetime, timestamp);
   DB_FIELD(double, income);
} proto;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(sizeof(Data));
   PRTF(sizeof(DataDB));
   ArrayPrint(DataDB::prototype);
}
//+------------------------------------------------------------------+
/*

   DBEntity<DataDB>::DBField<long>::DBField<long>(const string,const string)
   long id
   DBEntity<DataDB>::DBField<string>::DBField<string>(const string,const string)
   string name
   DBEntity<DataDB>::DBField<datetime>::DBField<datetime>(const string,const string)
   datetime timestamp
   DBEntity<DataDB>::DBField<double>::DBField<double>(const string,const string)
   double income
   sizeof(Data)=36 / ok
   sizeof(DataDB)=36 / ok
               [,0]        [,1]        [,2]
   [0,] "long"      "id"        ""         
   [1,] "string"    "name"      ""         
   [2,] "datetime"  "timestamp" ""         
   [3,] "double"    "income"    ""         

*/
//+------------------------------------------------------------------+
