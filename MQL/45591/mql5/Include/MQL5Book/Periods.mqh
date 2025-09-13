//+------------------------------------------------------------------+
//|                                                      Periods.mqh |
//|                                    Copyright (c) 2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#define PERIOD_PREFIX_LENGTH 7 // StringLen("PERIOD_")

//+------------------------------------------------------------------+
//| Convert timeframe to short name (without "PERIOD_" prefix)       |
//+------------------------------------------------------------------+
string PeriodToString(const ENUM_TIMEFRAMES tf = PERIOD_CURRENT)
{
   return StringSubstr(EnumToString(tf == PERIOD_CURRENT ? _Period : tf),
      PERIOD_PREFIX_LENGTH);
}

//+------------------------------------------------------------------+
//| Convert a name of timeframe to its value as ENUM_TIMEFRAMES      |
//| Both full form (as PERIOD_H4) and short form (H4) are supported  |
//+------------------------------------------------------------------+
ENUM_TIMEFRAMES StringToPeriod(string name)
{
   if(StringLen(name) < 2) return 0;
   // if a full name is specified "PERIOD_TN", convert it to short form "TN"
   if(StringLen(name) > PERIOD_PREFIX_LENGTH)
   {
     name = StringSubstr(name, PERIOD_PREFIX_LENGTH);
   }
   // convert trailing part of name with digits ("N") to number, skip "T"
   const int count = (int)StringToInteger(StringSubstr(name, 1));
   // clear possible error WRONG_STRING_PARAMETER(5040) from StringToInteger
   ResetLastError();
   switch(name[0])
   {
      case 'M':
         if(!count) return PERIOD_MN1;
         return (ENUM_TIMEFRAMES)count;
      case 'H':
         return (ENUM_TIMEFRAMES)(0x4000 + count);
      case 'D':
         return PERIOD_D1;
      case 'W':
         return PERIOD_W1;
   }
   return 0;
}
//+------------------------------------------------------------------+
