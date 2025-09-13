//+------------------------------------------------------------------+
//|                                                     TypeEnum.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ENUM_DAY_OF_WEEK sun = SUNDAY;     // sun = 0
   ENUM_DAY_OF_WEEK mon = MONDAY;     // mon = 1
   ENUM_DAY_OF_WEEK tue = TUESDAY;    // tue = 2
   ENUM_DAY_OF_WEEK wed = WEDNESDAY;  // wed = 3
   ENUM_DAY_OF_WEEK thu = THURSDAY;   // thu = 4
   ENUM_DAY_OF_WEEK fri = FRIDAY;     // fri = 5
   ENUM_DAY_OF_WEEK sat = SATURDAY;   // sat = 6

   int i = 0;
   ENUM_DAY_OF_WEEK x = i; // warning: implicit enum conversion
   ENUM_DAY_OF_WEEK y = 1; // ok, equals to MONDAY

   ENUM_ORDER_TYPE buy = ORDER_TYPE_BUY;   // buy = 0
   ENUM_ORDER_TYPE sell = ORDER_TYPE_SELL; // sell = 1
   // ...

   // warning: implicit conversion...
   //          from 'enum ENUM_DAY_OF_WEEK' to 'enum ENUM_ORDER_TYPE'

   //          'ENUM_ORDER_TYPE::ORDER_TYPE_SELL' will be used...
   //          instead of 'ENUM_DAY_OF_WEEK::MONDAY'
   ENUM_ORDER_TYPE type = MONDAY;

   // compilation errors: uncomment to reproduce
   // ENUM_DAY_OF_WEEK z = 10; // error: '10' - cannot convert enum
   // ENUM_DAY_OF_WEEK day = ORDER_TYPE_CLOSE_BY;

   PRT(sun);
   PRT(mon);
   PRT(tue);
   PRT(wed);
   PRT(thu);
   PRT(fri);
   PRT(sat);
   PRT(x);
   PRT(y);

   PRT(buy);
   PRT(sell);
}

//+------------------------------------------------------------------+
