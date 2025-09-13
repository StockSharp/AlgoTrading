//+------------------------------------------------------------------+
//|                                                      EnvKeys.mq5 |
//|                             Copyright 2021-2024, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   for(ENUM_TERMINAL_INFO_INTEGER i = TERMINAL_KEYSTATE_TAB;
      i <= TERMINAL_KEYSTATE_SCRLOCK; ++i)
   {
      const string e = EnumToString(i);
      // skip values not presenting elements of the enumeration
      if(StringFind(e, "ENUM_TERMINAL_INFO_INTEGER") == 0) continue;
      PrintFormat("%s=%4X", e, (ushort)TerminalInfoInteger(i));
   }
}
//+------------------------------------------------------------------+
