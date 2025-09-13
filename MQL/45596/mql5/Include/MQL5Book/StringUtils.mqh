//+------------------------------------------------------------------+
//|                                                  StringUtils.mqh |
//|                         Copyright (c) 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Concatenate all strings from array using glue symbol             |
//+------------------------------------------------------------------+
string StringCombine(const string &array[], const ushort glue)
{
   const int n = ArraySize(array);
   if(n == 0) return "";
   
   string result = array[0];
   
   for(int i = 1; i < n; ++i)
   {
      result += ShortToString(glue) + array[i];
   }
   return result;
}

//+------------------------------------------------------------------+
//| Array or subarray of strings concatenation                       |
//+------------------------------------------------------------------+
string SubArrayCombine(const string &array[], const string glue = "",
   const uint start = 0, uint count = -1)
{
   const uint n = ArraySize(array);
   if(start >= n) return "";
   if(count == (uint)-1) count = n - start;
   
   string result = array[start];
   
   for(uint i = 1; i < count && start + i < n; ++i)
   {
      result += glue + array[start + i];
   }
   return result;
}

//+------------------------------------------------------------------+
//| Return a string with characters in reversed order                |
//+------------------------------------------------------------------+
string StringReverse(const string source)
{
   ushort chars[];
   return ArrayReverse(chars, 0, StringToShortArray(source, chars) - 1) ?
      ShortArrayToString(chars) : NULL;
}

//+------------------------------------------------------------------+
