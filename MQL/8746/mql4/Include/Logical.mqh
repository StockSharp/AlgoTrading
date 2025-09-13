//+------------------------------------------------------------------+
//|                                                      Logical.mqh |
//|                                       Copyright © 2009, Tinytjan |
//|                                                 tinytjan@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, Tinytjan"
#property link      "tinytjan@mail.ru"

// Logical imports
#import "Logical.ex4"
   double DoubleIf(bool Condition, double IfTrue, double IfFalse);
   int IntIf(bool Condition, int IfTrue, int IfFalse);
   string StringIf(bool Condition, string IfTrue, string IfFalse);
   void Stuck();
#import