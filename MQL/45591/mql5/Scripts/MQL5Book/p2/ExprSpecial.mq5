//+------------------------------------------------------------------+
//|                                                  ExprSpecial.mq5 |
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
   double array[2][2];
   double dynamic1[][1];
   double dynamic2[][2];
   
   PRT(sizeof(double));                           // 8
   PRT(sizeof(string));                           // 12
   PRT(sizeof("This string is 29 bytes long!"));  // 12
   PRT(sizeof(array));                            // 32
   PRT(sizeof(array) / sizeof(double));           // 4 (elements)
   PRT(sizeof(dynamic1));                         // 52
   PRT(sizeof(dynamic2));                         // 52

   PRT(typename(double));                         // double
   PRT(typename(array));                          // double [2][2]
   PRT(typename(dynamic1));                       // double [][1]
   PRT(typename(1 + 2));                          // int
}
//+------------------------------------------------------------------+
