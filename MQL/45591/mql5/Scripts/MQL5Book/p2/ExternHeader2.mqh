//+------------------------------------------------------------------+
//|                                                ExternHeader2.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#ifdef USE_INCLUDE_WORKAROUND
   // a replacement for extern variable could be:

   #include "ExternCommon.mqh" // which contains:
   //+------------------------------------------+
   //|                  /* ExternCommon.mqh */  |
   //| int x;                                   |
   //+------------------------------------------+

#else

   extern int x;

#endif

// ERROR:
// extern short y; // variable 'y' already defined with different type

//+------------------------------------------------------------------+
//| Mockup decrement function using 'x'                              |
//+------------------------------------------------------------------+
void dec()
{
   x--;
}
//+------------------------------------------------------------------+
