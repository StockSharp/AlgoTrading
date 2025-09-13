//+------------------------------------------------------------------+
//|                                                EnvConnection.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(TerminalInfoInteger(TERMINAL_CONNECTED));
   PRTF(TerminalInfoInteger(TERMINAL_PING_LAST));
   PRTF(TerminalInfoInteger(TERMINAL_COMMUNITY_ACCOUNT));
   PRTF(TerminalInfoInteger(TERMINAL_COMMUNITY_CONNECTION));
   PRTF(TerminalInfoInteger(TERMINAL_MQID));
   PRTF(TerminalInfoDouble(TERMINAL_RETRANSMISSION));
   PRTF(TerminalInfoDouble(TERMINAL_COMMUNITY_BALANCE));
   /*
      example output
      
      TerminalInfoInteger(TERMINAL_CONNECTED)=1 / ok
      TerminalInfoInteger(TERMINAL_PING_LAST)=49082 / ok
      TerminalInfoInteger(TERMINAL_COMMUNITY_ACCOUNT)=0 / ok
      TerminalInfoInteger(TERMINAL_COMMUNITY_CONNECTION)=0 / ok
      TerminalInfoInteger(TERMINAL_MQID)=0 / ok
      TerminalInfoDouble(TERMINAL_RETRANSMISSION)=0.0 / ok
      TerminalInfoDouble(TERMINAL_COMMUNITY_BALANCE)=0.0 / ok
   */
}
//+------------------------------------------------------------------+
