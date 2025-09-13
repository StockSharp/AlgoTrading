//+------------------------------------------------------------------+
//|                                                         RTTI.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TypeName.mqh>

//+------------------------------------------------------------------+
//| Run-Time Type Information stub (via strings)                     |
//+------------------------------------------------------------------+
template<typename T>
ENUM_DATATYPE rtti(T v = (T)NULL)
{
   static string types[] =
   {
      "null",     //               (0)
      "bool",     // 0 TYPE_BOOL=1 (1)
      "char",     // 1 TYPE_CHAR=2 (2)
      "uchar",    // 2 TYPE_UCHAR=3 (3)
      "short",    // 3 TYPE_SHORT=4 (4)
      "ushort",   // 4 TYPE_USHORT=5 (5)
      "color",    // 5 TYPE_COLOR=6 (6)
      "int",      // 6 TYPE_INT=7 (7)
      "uint",     // 7 TYPE_UINT=8 (8)
      "datetime", // 8 TYPE_DATETIME=9 (9)
      "long",     // 9 TYPE_LONG=10 (A)
      "ulong",    // 10 TYPE_ULONG=11 (B)
      "float",    // 11 TYPE_FLOAT=12 (C)
      "double",   // 12 TYPE_DOUBLE=13 (D)
      "string",   // 13 TYPE_STRING=14 (E)
   };
   const string t = TYPENAME(T);
   for(int i = 0; i < ArraySize(types); ++i)
   {
      if(types[i] == t)
      {
         return (ENUM_DATATYPE)i;
      }
   }
   return (ENUM_DATATYPE)0;
}
//+------------------------------------------------------------------+
