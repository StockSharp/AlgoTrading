//+------------------------------------------------------------------+
//|                                         ConversionTimeStruct.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/DateTime.mqh>

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| ArrayInitialize overload for MqlDateTime                         |
//+------------------------------------------------------------------+
int ArrayInitialize(MqlDateTime &mdt[], MqlDateTime &init)
{
   const int n = ArraySize(mdt);
   for(int i = 0; i < n; ++i)
   {
      mdt[i] = init;
   }
   return n;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print("*** TimeToStruct ***");
   
   // fill array with test cases
   datetime time[] =
   {
      D'2021.01.28 23:00:15', // correct datetime
      D'3000.12.31 23:59:59', // last datetime
      LONG_MAX // invalid value: will raise ERR_INVALID_DATETIME (4010) error
   };

   // calculate array size at compile time
   const int n = sizeof(time) / sizeof(datetime);

   MqlDateTime null = {};
   MqlDateTime mdt[];

   // allocate array of structs for results
   ArrayResize(mdt, n);

   // call our overload
   ArrayInitialize(mdt, null);

   // run test cases
   for(int i = 0; i < n; ++i)
   {
      PRT(time[i]); // output original data

      if(!TimeToStruct(time[i], mdt[i])) // on error output its code
      {
         Print("error: ", _LastError);
         mdt[i].year = _LastError;
      }
   }

   // show results in the log
   ArrayPrint(mdt);

   // now utilize DateTime helper class from DateTime.mqh
   // first extract day of week from the given datetime
   PRT(EnumToString(TimeDayOfWeek(time[0])));
   // then extract year, month, day from the same value
   PRT(_TimeYear());
   PRT(_TimeMonth());
   PRT(_TimeDay());

   /* will output:
   time[i]=2021.01.28 23:00:15
   time[i]=3000.12.31 23:59:59
   time[i]=wrong datetime
   error: 4010
       [year] [mon] [day] [hour] [min] [sec] [day_of_week] [day_of_year]
   [0]   2021     1    28     23     0    15             4            27
   [1]   3000    12    31     23    59    59             3           364
   [2]   4010     0     0      0     0     0             0             0
   EnumToString(DateTime::_DateTime.assign(time[0]).__TimeDayOfWeek())=THURSDAY
   DateTime::_DateTime.__TimeYear()=2021
   DateTime::_DateTime.__TimeMonth()=1
   DateTime::_DateTime.__TimeDay()=28
   */
   
   Print("*** StructToTime ***");
   // fill test array of input structs
   MqlDateTime parts[] =
   {
      {0, 0, 0, 0, 0, 0, 0, 0},
      {100, 0, 0, 0, 0, 0, 0, 0},
      {2021, 2, 30, 0, 0, 0, 0, 0},
      {2021, 13, -5, 0, 0, 0, 0, 0},
      {2021, 50, 100, 0, 0, 0, 0, 0},
      {2021, 10, 20, 15, 30, 155, 0, 0},
      {2021, 10, 20, 15, 30, 55, 0, 0},
   };
   ArrayPrint(parts);
   Print("");
   
   // convert all elements in the loop
   for(int i = 0; i < sizeof(parts) / sizeof(MqlDateTime); ++i)
   {
      ResetLastError();
      datetime result = StructToTime(parts[i]);
      Print("[", i, "] ", (long)result, " ", result, " ", _LastError);
   }

   /* will output:
       [year] [mon] [day] [hour] [min] [sec] [day_of_week] [day_of_year]
   [0]      0     0     0      0     0     0             0             0
   [1]    100     0     0      0     0     0             0             0
   [2]   2021     2    30      0     0     0             0             0
   [3]   2021    13    -5      0     0     0             0             0
   [4]   2021    50   100      0     0     0             0             0
   [5]   2021    10    20     15    30   155             0             0
   [6]   2021    10    20     15    30    55             0             0
   
   [0] -1 wrong datetime 4010
   [1] 946684800 2000.01.01 00:00:00 4010
   [2] 1614643200 2021.03.02 00:00:00 0
   [3] 1638316800 2021.12.01 00:00:00 4010
   [4] 1640908800 2021.12.31 00:00:00 4010
   [5] 1634743859 2021.10.20 15:30:59 4010
   [6] 1634743855 2021.10.20 15:30:55 0
   */
}
//+------------------------------------------------------------------+
