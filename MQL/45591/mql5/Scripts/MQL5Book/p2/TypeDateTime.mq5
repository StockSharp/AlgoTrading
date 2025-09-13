//+------------------------------------------------------------------+
//|                                                 TypeDateTime.mq5 |
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
   // WARNINGS: invalid date
   datetime blank = D'';           // blank = day of compilation
   datetime noday = D'15:45:00';   // noday = day of compilation + 15:45
   datetime feb30 = D'2021.02.30'; // feb30 = 2021.03.02 00:00:00
   datetime mon22 = D'2021.22.01'; // mon22 = 2022.10.01 00:00:00

   // OK
   datetime dt0 = 0;                      // 1970.01.01 00:00:00
   datetime all = D'2021.01.01 10:10:30'; // 2021.01.01 10:10:30
   datetime day = D'2025.12.12 12';       // 2025.12.12 12:00:00

   PRT(blank);
   PRT(noday);
   PRT(feb30);
   PRT(mon22);

   PRT(dt0);
   PRT(all);
   PRT(day);
}

//+------------------------------------------------------------------+
