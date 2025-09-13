//+------------------------------------------------------------------+
//|                                                       Unions.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define MAX_LONG_IN_DOUBLE       9007199254740992
// FYI: ULONG_MAX            18446744073709551615

union ulong2double
{
   ulong U;   // 8 bytes
   double D;  // 8 bytes
};

ulong2double converter;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print(sizeof(ulong2double)); // 8
   
   const ulong value = MAX_LONG_IN_DOUBLE + 1;
   
   double d = value; // possible loss of data due to type conversion
   ulong result = d; // possible loss of data due to type conversion
   
   Print(d, " / ", value, " -> ", result);
   // 9007199254740992.0 / 9007199254740993 -> 9007199254740992
   
   converter.U = value;
   double r = converter.D; // no conversion
   Print(r);               // 4.450147717014403e-308
   Print(offsetof(ulong2double, U), " ", offsetof(ulong2double, D));
}
//+------------------------------------------------------------------+