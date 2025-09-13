//+------------------------------------------------------------------+
//|                                                      EnvMode.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(MQLInfoInteger(MQL_TESTER));
   PRTF(MQLInfoInteger(MQL_DEBUG));
   PRTF(MQLInfoInteger(MQL_PROFILER));
   PRTF(MQLInfoInteger(MQL_VISUAL_MODE));
   PRTF(MQLInfoInteger(MQL_OPTIMIZATION));
   PRTF(MQLInfoInteger(MQL_FORWARD));
   PRTF(MQLInfoInteger(MQL_FRAME_MODE));
   /*
      example output
      
      MQLInfoInteger(MQL_TESTER)=0 / ok
      MQLInfoInteger(MQL_DEBUG)=1 / ok
      MQLInfoInteger(MQL_PROFILER)=0 / ok
      MQLInfoInteger(MQL_VISUAL_MODE)=0 / ok
      MQLInfoInteger(MQL_OPTIMIZATION)=0 / ok
      MQLInfoInteger(MQL_FORWARD)=0 / ok
      MQLInfoInteger(MQL_FRAME_MODE)=0 / ok
   */
}
//+------------------------------------------------------------------+
