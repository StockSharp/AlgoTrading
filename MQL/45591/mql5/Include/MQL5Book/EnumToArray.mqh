//+------------------------------------------------------------------+
//|                                                  EnumToArray.mqh |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                            https://www.mql5.com/ |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Convert enum E elements into array of ints (correponding to IDs) |
//| Return number of elements in the enum E                          |
//+------------------------------------------------------------------+
template<typename E>
int EnumToArray(E /*dummy*/, int &values[],
   const int start = INT_MIN, const int stop = INT_MAX)
{
   const static string t = "::";

   ArrayResize(values, 0);
   int count = 0;

   for(int i = start; i < stop && !IsStopped(); i++)
   {
      E e = (E)i;
      if(StringFind(EnumToString(e), t) == -1)
      {
         ArrayResize(values, count + 1);
         values[count++] = i;
      }
   }
   return count;
}

//+------------------------------------------------------------------+
//| Shorthand version of enum E elements detection as array of ints  |
//+------------------------------------------------------------------+
template<typename E>
int EnumToArray(int &values[],
   const int start = INT_MIN, const int stop = INT_MAX)
{
   static E e;
   return EnumToArray(e, values, start, stop);
}

//+------------------------------------------------------------------+
