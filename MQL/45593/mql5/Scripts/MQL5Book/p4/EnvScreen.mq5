//+------------------------------------------------------------------+
//|                                                    EnvScreen.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(TerminalInfoInteger(TERMINAL_SCREEN_DPI));
   PRTF(TerminalInfoInteger(TERMINAL_SCREEN_LEFT));
   PRTF(TerminalInfoInteger(TERMINAL_SCREEN_TOP));
   PRTF(TerminalInfoInteger(TERMINAL_SCREEN_WIDTH));
   PRTF(TerminalInfoInteger(TERMINAL_SCREEN_HEIGHT));
   PRTF(TerminalInfoInteger(TERMINAL_LEFT));
   PRTF(TerminalInfoInteger(TERMINAL_TOP));
   PRTF(TerminalInfoInteger(TERMINAL_RIGHT));
   PRTF(TerminalInfoInteger(TERMINAL_BOTTOM));
   /*
      example output
      
      TerminalInfoInteger(TERMINAL_SCREEN_DPI)=96 / ok
      TerminalInfoInteger(TERMINAL_SCREEN_LEFT)=0 / ok
      TerminalInfoInteger(TERMINAL_SCREEN_TOP)=0 / ok
      TerminalInfoInteger(TERMINAL_SCREEN_WIDTH)=1440 / ok
      TerminalInfoInteger(TERMINAL_SCREEN_HEIGHT)=900 / ok
      TerminalInfoInteger(TERMINAL_LEFT)=126 / ok
      TerminalInfoInteger(TERMINAL_TOP)=41 / ok
      TerminalInfoInteger(TERMINAL_RIGHT)=1334 / ok
      TerminalInfoInteger(TERMINAL_BOTTOM)=836 / ok
   */
}
//+------------------------------------------------------------------+
