//+------------------------------------------------------------------+
//|                                                 SymbolExists.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Check given symbol name for existence.                           |
//+------------------------------------------------------------------+
#property script_show_inputs

input string SymbolToCheck = "XYZ";

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string _SymbolToCheck = SymbolToCheck == "" ? _Symbol : SymbolToCheck;
   bool custom = false;
   PrintFormat("Symbol '%s' is %s", _SymbolToCheck,
      (SymbolExist(_SymbolToCheck, custom) ? (custom ? "custom" : "standard") : "missing"));
}
//+------------------------------------------------------------------+
