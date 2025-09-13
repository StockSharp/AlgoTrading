//+------------------------------------------------------------------+
//|                                                  LibRandTest.mq5 |
//|                             Copyright 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input int N = 10000;
input double Mean = 0.0;
input double Sigma = 1.0;
input double HistogramStep = 0.5;
input int RandomSeed = 0;

//+------------------------------------------------------------------+
//| Includes                                                         |
//+------------------------------------------------------------------+
#include <MQL5Book/LibRand.mqh>
#include <MQL5Book/MapArray.mqh>
#include <MQL5Book/QuickSortStructT.mqh>

#define COMMA ,

//+------------------------------------------------------------------+
//| Special map with sorting                                         |
//+------------------------------------------------------------------+
template<typename K,typename V>
class MyMapArray: public MapArray<K,V>
{
public:
   void sort()
   {
      SORT_STRUCT(Pair<K COMMA V>, array, key);
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const uint seed = RandomSeed ? RandomSeed : GetTickCount();
   Print("Random seed: ", seed);
   MathSrand(seed);
   
   // call 2 library functions
   Print("Random HEX-string: ", RandomString(30, StringPatternDigit() + "ABCDEF"));
   Print("Random strings:");
   string text[];
   RandomStrings(text, 5, 10, 20);
   ArrayPrint(text);
   
   // call another library function
   double x[];
   PseudoNormalArray(x, N, Mean, Sigma);

   // now check distribution of generated values
   Print("Random pseudo-gaussian histogram: ");
   
   // use 'long' as type for keys because 'int' is used for direct access by index
   MyMapArray<long,int> map;
   
   for(int i = 0; i < N; ++i)
   {
      map.inc((long)MathRound(x[i] / HistogramStep));
   }
   map.sort();                             // sort by key value for display
   
   int max = 0;                            // use max value for scaling
   for(int i = 0; i < map.getSize(); ++i)
   {
      max = fmax(max, map.getValue(i));
   }
   
   const double scale = fmax(max / 80, 1); // max 80 chars in histogram
   
   for(int i = 0; i < map.getSize(); ++i)  // print histogram
   {
      const int p = (int)MathRound(map.getValue(i) / scale);
      string filler;
      StringInit(filler, p, '*');
      Print(StringFormat("%+.2f (%4d)",
         map.getKey(i) * HistogramStep, map.getValue(i)), " ", filler);
   }
}
//+------------------------------------------------------------------+
/*

Random seed: 8859858
Random HEX-string: E58B125BCCDA67ABAB2F1C6D6EC677
Random strings:
"K4ZOpdIy5yxq4ble2" "NxTrVRl6q5j3Hr2FY" "6qxRdDzjp3WNA8xV"  "UlOPYinnGd36"      "6OCmde6rvErGB3wG" 
Random pseudo-gaussian histogram: 
-9.50 (   2) 
-8.50 (   1) 
-8.00 (   1) 
-7.00 (   1) 
-6.50 (   5) 
-6.00 (  10) *
-5.50 (  10) *
-5.00 (  24) *
-4.50 (  28) **
-4.00 (  50) ***
-3.50 ( 100) ******
-3.00 ( 195) ***********
-2.50 ( 272) ***************
-2.00 ( 510) ****************************
-1.50 ( 751) ******************************************
-1.00 (1029) *********************************************************
-0.50 (1288) ************************************************************************
+0.00 (1457) *********************************************************************************
+0.50 (1263) **********************************************************************
+1.00 (1060) ***********************************************************
+1.50 ( 772) *******************************************
+2.00 ( 480) ***************************
+2.50 ( 280) ****************
+3.00 ( 172) **********
+3.50 ( 112) ******
+4.00 (  52) ***
+4.50 (  43) **
+5.00 (  10) *
+5.50 (   8) 
+6.00 (   8) 
+6.50 (   2) 
+7.00 (   3) 
+7.50 (   1) 

*/
//+------------------------------------------------------------------+
