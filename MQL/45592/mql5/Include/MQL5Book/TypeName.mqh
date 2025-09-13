//+------------------------------------------------------------------+
//|                                                     TypeName.mqh |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                            https://www.mql5.com/ |
//|                                                                  |
//| Provides stripped down type name to mimic legacy typename()      |
//+------------------------------------------------------------------+

string _typename(const string t)
{
   static const string keywords[] = {"class", "struct", "enum", "union", "interface"};
   static const int n = sizeof(keywords) / sizeof(keywords[0]);
   static string tokens[];

   if(StringSplit(t, ' ', tokens) <= 1) return t;
   
   for(int i = 0; i < n; ++i)
   {
      if(keywords[i] == tokens[0]) return tokens[1];
   }
   
   return tokens[0];
}

#define TYPENAME(T) _typename(typename(T))

//+------------------------------------------------------------------+
