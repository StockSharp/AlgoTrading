//+------------------------------------------------------------------+
//|                                                    TimeCount.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const uint startMs = GetTickCount();
   const ulong startMcs =  GetMicrosecondCount();
   
   // loop for 5 seconds
   while(PRTF(GetTickCount()) < startMs + 5000)
   {
      PRTF(GetMicrosecondCount());
      Sleep(1000);
   }
   
   PRTF(GetTickCount() - startMs);
   PRTF(GetMicrosecondCount() - startMcs);
}
//+------------------------------------------------------------------+
