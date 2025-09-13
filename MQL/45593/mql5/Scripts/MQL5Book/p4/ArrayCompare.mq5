//+------------------------------------------------------------------+
//|                                                 ArrayCompare.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "=", (A))

#define LIMIT 10

//+------------------------------------------------------------------+
//| Simple struct to fill in arrays with random(!) data              |
//+------------------------------------------------------------------+
struct Dummy
{
   int x;
   int y;
   
   Dummy()
   {
      x = rand() / 10000;
      y = rand() / 5000;
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // array of structs
   Dummy a1[LIMIT];
   ArrayPrint(a1);
   /*
      NB: output will change on every run due to randomization
      
       [x] [y]
   [0]   0   3
   [1]   2   4
   [2]   2   3
   [3]   1   6
   [4]   0   6
   [5]   2   0
   [6]   0   4
   [7]   2   5
   [8]   0   5
   [9]   3   6
   */
   
   // pair-wise comparison of successive elements
   // -1: [i] < [i + 1]
   // +1: [i] > [i + 1]
   for(int i = 0; i < LIMIT - 1; ++i)
   {
      PRT(ArrayCompare(a1, a1, i, i + 1, 1));
   }
   /*
   ArrayCompare(a1,a1,i,i+1,1)=-1
   ArrayCompare(a1,a1,i,i+1,1)=1
   ArrayCompare(a1,a1,i,i+1,1)=1
   ArrayCompare(a1,a1,i,i+1,1)=1
   ArrayCompare(a1,a1,i,i+1,1)=-1
   ArrayCompare(a1,a1,i,i+1,1)=1
   ArrayCompare(a1,a1,i,i+1,1)=-1
   ArrayCompare(a1,a1,i,i+1,1)=1
   ArrayCompare(a1,a1,i,i+1,1)=-1
   */

   // compare first half with second half of the array
   PRT(ArrayCompare(a1, a1, 0, LIMIT / 2, LIMIT / 2));
   /*
   ArrayCompare(a1,a1,0,10/2,10/2)=-1
   */
   
   // arrays of strings, 1D and 2D
   string s[] = {"abc","456","$"};
   string s0[][3] = {{"abc","456","$"}};
   string s1[][3] = {{"abc","456",""}};
   string s2[][3] = {{"abc","456"}}; // note the last element is omitted, null
   string s3[][2] = {{"abc","456"}};
   string s4[][2] = {{"aBc","456"}};

   PRT(ArrayCompare(s0, s));  // s0 == s, 1D and 2D hold the same data
   PRT(ArrayCompare(s0, s1)); // s0 > s1, because "$" > ""
   PRT(ArrayCompare(s1, s2)); // s1 > s2, because "" > null
   PRT(ArrayCompare(s2, s3)); // s2 > s3, because of length [3] > [2]
   PRT(ArrayCompare(s3, s4)); // s3 < s4, because "abc" < "aBc"
   /*
   ArrayCompare(s0,s)=0
   ArrayCompare(s0,s1)=1
   ArrayCompare(s1,s2)=1
   ArrayCompare(s2,s3)=1
   ArrayCompare(s3,s4)=-1
   */
   
   PRT(StringCompare("abc", "aBc")); // check up
   /*
   StringCompare(abc,aBc)=-1
   */

   PRT(ArrayCompare(s0, s1, 1, 1, 1)); // second elements (index 1) are the same
   PRT(ArrayCompare(s1, s2, 0, 0, 2)); // first 2 elements are the same
   /*
   ArrayCompare(s0,s1,1,1,1)=0
   ArrayCompare(s1,s2,0,0,2)=0
   */
}
//+------------------------------------------------------------------+
