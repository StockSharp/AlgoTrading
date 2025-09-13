//+------------------------------------------------------------------+
//|                                                 EnvProvision.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(MQLInfoInteger(MQL_MEMORY_LIMIT)); // Kb!
   PRTF(MQLInfoInteger(MQL_MEMORY_USED));
   PRTF(TerminalInfoInteger(TERMINAL_MEMORY_PHYSICAL));
   PRTF(TerminalInfoInteger(TERMINAL_MEMORY_TOTAL));
   PRTF(TerminalInfoInteger(TERMINAL_MEMORY_AVAILABLE));
   PRTF(TerminalInfoInteger(TERMINAL_MEMORY_USED));
   PRTF(TerminalInfoInteger(TERMINAL_DISK_SPACE));
   PRTF(TerminalInfoInteger(TERMINAL_CPU_CORES));
   PRTF(TerminalInfoInteger(TERMINAL_OPENCL_SUPPORT));

   uchar array[];
   PRTF(ArrayResize(array, 1024 * 1024 * 10)); // allocate 10 Mb
   PRTF(MQLInfoInteger(MQL_MEMORY_USED));
   PRTF(TerminalInfoInteger(TERMINAL_MEMORY_AVAILABLE));
   PRTF(TerminalInfoInteger(TERMINAL_MEMORY_USED));
   /*
      example output
      
      MQLInfoInteger(MQL_MEMORY_LIMIT)=8388608 / ok
      MQLInfoInteger(MQL_MEMORY_USED)=1 / ok
      TerminalInfoInteger(TERMINAL_MEMORY_PHYSICAL)=4095 / ok
      TerminalInfoInteger(TERMINAL_MEMORY_TOTAL)=8190 / ok
      TerminalInfoInteger(TERMINAL_MEMORY_AVAILABLE)=7842 / ok
      TerminalInfoInteger(TERMINAL_MEMORY_USED)=348 / ok
      TerminalInfoInteger(TERMINAL_DISK_SPACE)=4528 / ok
      TerminalInfoInteger(TERMINAL_CPU_CORES)=2 / ok
      TerminalInfoInteger(TERMINAL_OPENCL_SUPPORT)=0 / ok
      ArrayResize(array,1024*1024*10)=10485760 / ok
      MQLInfoInteger(MQL_MEMORY_USED)=11 / ok
      TerminalInfoInteger(TERMINAL_MEMORY_AVAILABLE)=7837 / ok
      TerminalInfoInteger(TERMINAL_MEMORY_USED)=353 / ok
   */
}
//+------------------------------------------------------------------+
