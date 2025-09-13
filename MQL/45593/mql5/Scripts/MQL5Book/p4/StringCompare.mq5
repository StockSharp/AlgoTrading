//+------------------------------------------------------------------+
//|                                                StringCompare.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/QuickSortT.mqh>
#include <MQL5Book/PermutationGenerator.mqh>

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Helper class to sort strings by StringCompare w/w/o case         |
//+------------------------------------------------------------------+
class SortingStringCompare : public QuickSortT<string>
{
   const bool caseEnabled;
public:
   SortingStringCompare(const bool sensitivity = true) :
      caseEnabled(sensitivity) { }
      
   virtual int Compare(string &a, string &b) override
   {
      return StringCompare(a, b, caseEnabled);
   }
};

//+------------------------------------------------------------------+
//| Create all possible strings of length len from given symbols     |
//+------------------------------------------------------------------+
void GenerateStringList(const string symbols, const int len, string &result[])
{
   const int n = StringLen(symbols); // alphabet size, unique chars assumed
   PermutationGenerator g(len, n);
   SimpleArray<PermutationGenerator::Result> *r = g.run();
   ArrayResize(result, r.size());
   // loop through all possible premutations of chars
   for(int i = 0; i < r.size(); ++i)
   {
      string element;
      // loop throught all places in the string
      for(int j = 0; j < len; ++j)
      {
         // append a char (by its index in alphabet) to the string
         element += ShortToString(symbols[r[i].indices[j]]);
      }
      result[i] = element;
   }
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRT(StringCompare("A", "a"));        // 1, means "A" > "a" (!)
   PRT(StringCompare("A", "a", false)); // 0, means "A" == "a"
   PRT("A" > "a");                      // false,   "A" < "a"

   PRT(StringCompare("x","y"));         // -1, means "x" < "y"
   PRT("x" > "y");                      // false,    "x" < "y"

   string messages[];
   GenerateStringList("abcABC", 2, messages);
   Print("Original data[", ArraySize(messages), "]:");
   ArrayPrint(messages);

   Print("Default case-sensitive sorting:");
   QuickSortT<string> sorting;
   sorting.QuickSort(messages);
   ArrayPrint(messages);

   Print("StringCompare case-insensitive sorting:");
   SortingStringCompare caseOff(false);
   caseOff.QuickSort(messages);
   ArrayPrint(messages);

   Print("StringCompare case-sensitive sorting:");
   SortingStringCompare caseOn(true);
   caseOn.QuickSort(messages);
   ArrayPrint(messages);
   
   /*
      output:
   
   Original data[36]:
   [ 0] "aa" "ab" "ac" "aA" "aB" "aC" "ba" "bb" "bc" "bA" "bB" "bC" "ca" "cb" "cc" "cA" "cB" "cC"
   [18] "Aa" "Ab" "Ac" "AA" "AB" "AC" "Ba" "Bb" "Bc" "BA" "BB" "BC" "Ca" "Cb" "Cc" "CA" "CB" "CC"
   Default case-sensitive sorting:
   [ 0] "AA" "AB" "AC" "Aa" "Ab" "Ac" "BA" "BB" "BC" "Ba" "Bb" "Bc" "CA" "CB" "CC" "Ca" "Cb" "Cc"
   [18] "aA" "aB" "aC" "aa" "ab" "ac" "bA" "bB" "bC" "ba" "bb" "bc" "cA" "cB" "cC" "ca" "cb" "cc"
   StringCompare case-insensitive sorting:
   [ 0] "AA" "Aa" "aA" "aa" "AB" "aB" "Ab" "ab" "aC" "AC" "Ac" "ac" "BA" "Ba" "bA" "ba" "BB" "bB"
   [18] "Bb" "bb" "bC" "BC" "Bc" "bc" "CA" "Ca" "cA" "ca" "CB" "cB" "Cb" "cb" "cC" "CC" "Cc" "cc"
   StringCompare case-sensitive sorting:
   [ 0] "aa" "aA" "Aa" "AA" "ab" "aB" "Ab" "AB" "ac" "aC" "Ac" "AC" "ba" "bA" "Ba" "BA" "bb" "bB"
   [18] "Bb" "BB" "bc" "bC" "Bc" "BC" "ca" "cA" "Ca" "CA" "cb" "cB" "Cb" "CB" "cc" "cC" "Cc" "CC"
   
   */
}
//+------------------------------------------------------------------+