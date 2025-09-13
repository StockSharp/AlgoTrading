//+------------------------------------------------------------------+
//|                                                ExternHeader1.mqh |
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

// the following line would throw the error "unresolved extern variable 'y'",
// if 'y' is not defined anywhere else
extern long y;

short z;

//+------------------------------------------------------------------+
//| Mockup increment function using 'x'                              |
//+------------------------------------------------------------------+
void inc()
{
   x++;
}
//+------------------------------------------------------------------+
