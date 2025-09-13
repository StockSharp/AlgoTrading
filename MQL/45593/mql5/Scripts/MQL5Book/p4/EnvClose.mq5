//+------------------------------------------------------------------+
//|                                                     EnvClose.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

input int ReturnCode = 0;
input bool CloseTerminalNow = false;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   if(CloseTerminalNow)
   {
      TerminalClose(ReturnCode);
   }
   else
   {
      SetReturnError(ReturnCode);
   }
}
//+------------------------------------------------------------------+
