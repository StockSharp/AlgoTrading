//+------------------------------------------------------------------+
//|                                               EnvDescription.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(MQLInfoString(MQL_PROGRAM_NAME));
   PRTF(MQLInfoString(MQL_PROGRAM_PATH));
   PRTF(TerminalInfoString(TERMINAL_LANGUAGE));
   PRTF(TerminalInfoString(TERMINAL_COMPANY));
   PRTF(TerminalInfoString(TERMINAL_NAME));
   PRTF(TerminalInfoString(TERMINAL_PATH));
   PRTF(TerminalInfoString(TERMINAL_DATA_PATH));
   PRTF(TerminalInfoString(TERMINAL_COMMONDATA_PATH));
   /*
      example output
      
      MQLInfoString(MQL_PROGRAM_NAME)=EnvDescription / ok
      MQLInfoString(MQL_PROGRAM_PATH)=C:\Program Files\MT5East\MQL5\Scripts\MQL5Book\p4\EnvDescription.ex5 / ok
      TerminalInfoString(TERMINAL_LANGUAGE)=Russian / ok
      TerminalInfoString(TERMINAL_COMPANY)=MetaQuotes Ltd. / ok
      TerminalInfoString(TERMINAL_NAME)=MetaTrader 5 / ok
      TerminalInfoString(TERMINAL_PATH)=C:\Program Files\MT5East / ok
      TerminalInfoString(TERMINAL_DATA_PATH)=C:\Program Files\MT5East / ok
      TerminalInfoString(TERMINAL_COMMONDATA_PATH)=C:\Users\User\AppData\Roaming\MetaQuotes\Terminal\Common / ok
   */
}
//+------------------------------------------------------------------+
