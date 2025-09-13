//+------------------------------------------------------------------+
//|                                                  StructPrint.mqh |
//|                             Copyright 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Helper function to print a single struct via ArrayPrint          |
//+------------------------------------------------------------------+
template<typename S>
void StructPrint(const S &s, const ulong flags = 0, const int digits = INT_MAX)
{
   static S temp[1];
   temp[0] = s;
   ArrayPrint(temp, digits == INT_MAX ? _Digits : digits, NULL, 0, 1, flags);
}
//+------------------------------------------------------------------+