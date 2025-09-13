//+------------------------------------------------------------------+
//|                                            StringFindReplace.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Count how many times a substring 'wanted' occurs in a string     |
//+------------------------------------------------------------------+
int CountSubstring(const string value, const string wanted)
{
   // step back for unconditional increments in the loop beginning
   int cursor = -1;
   int count = -1;
   do
   {
      ++count;
      ++cursor; // make sure to search from next position
      // get next position or -1 if not found
      cursor = StringFind(value, wanted, cursor);
   }
   while(cursor > -1);
   return count;
}

//+------------------------------------------------------------------+
//| Replace all successive separators with single ones/StringReplace |
//+------------------------------------------------------------------+
int NormalizeSeparatorsByReplace(string &value, const ushort separator = ' ')
{
   const string single = ShortToString(separator);
   const string twin = single + single;
   int count = 0;
   int replaced = 0;
   do
   {
      replaced = StringReplace(value, twin, single);
      if(replaced > 0) count += replaced;
   }
   while(replaced > 0);
   return count;
}

//+------------------------------------------------------------------+
//| Replace all successive separators with single ones / StringSplit |
//+------------------------------------------------------------------+
int NormalizeSeparatorsBySplit(string &value, const ushort separator = ' ')
{
   const string single = ShortToString(separator);

   string elements[];
   const int n = StringSplit(value, separator, elements);
   ArrayPrint(elements); // debug

   StringFill(value, 0); // resulting string will replace original one
   
   for(int i = 0; i < n; ++i)
   {
      // empty string means separator, but we should output it,
      // only if previous string was not also empty (adjacent separator)
      if(elements[i] == "" && (i == 0 || elements[i - 1] != ""))
      {
         value += single;
      }
      else // all other strings are combined 'as is'
      {
         value += elements[i];
      }
   }
   
   return n;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   string abracadabra = "ABRACADABRA";
   
   // StringFind demo
   PRT(CountSubstring(abracadabra, "A"));    // 5
   PRT(CountSubstring(abracadabra, "D"));    // 1
   PRT(CountSubstring(abracadabra, "E"));    // 0
   PRT(CountSubstring(abracadabra, "ABRA")); // 2
   
   // StringReplace demo
   PRT(StringReplace(abracadabra, "ABRA", "-ABRA-")); // 2
   PRT(StringReplace(abracadabra, "CAD", "-"));      // 1
   PRT(StringReplace(abracadabra, "", "XYZ"));      // -1, error
   PRT(GetLastError());      // 5040, ERR_WRONG_STRING_PARAMETER
   PRT(abracadabra);                              // '-ABRA---ABRA-'
   
   string copy1 = "-" + abracadabra + "-";
   string copy2 = copy1;
   PRT(copy1);                                    // '--ABRA---ABRA--'
   PRT(NormalizeSeparatorsByReplace(copy1, '-')); // 4
   PRT(copy1);                                    // '-ABRA-ABRA-'
   PRT(StringReplace(copy1, "-", ""));            // 1
   PRT(copy1);                                    // 'ABRAABRA'
   
   // StringSplit demo
   PRT(NormalizeSeparatorsBySplit(copy2, '-'));   // 8
   // array debug print will output:
   // ""     ""     "ABRA" ""     ""     "ABRA" ""     ""
   PRT(copy2);                                    // '-ABRA-ABRA-'
   
   // StringSubstr demo
   PRT(StringSubstr("ABRACADABRA", 4, 3));        // 'CAD'
   PRT(StringSubstr("ABRACADABRA", 4, 100));      // 'CADABRA'
   PRT(StringSubstr("ABRACADABRA", 4));           // 'CADABRA'
   PRT(StringSubstr("ABRACADABRA", 100));         // ''
}
//+------------------------------------------------------------------+