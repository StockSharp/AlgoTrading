//+------------------------------------------------------------------+
//|                                                 ArrayReverse.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRTS(A) Print(#A, "=", (string)(A) + " / status:" + (string)GetLastError())

//+------------------------------------------------------------------+
//| Simple class for objects to fill in arrays                       |
//+------------------------------------------------------------------+
class Dummy
{
   static int counter;
   int x;
   string s; // string field makes compiler create implicit destructor
public:
   Dummy() { x = counter++; }
};

static int Dummy::counter;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Dummy objects[5];
   Print("Objects before reverse");
   ArrayPrint(objects);
   /*
   Objects before reverse
       [x]  [s]
   [0]   0 null
   [1]   1 null
   [2]   2 null
   [3]   3 null
   [4]   4 null
   */
   
   PRTS(ArrayReverse(objects));
   Print("Objects after reverse");
   ArrayPrint(objects);
   /*
   ArrayReverse(objects)=true / status:0
   Objects after reverse
       [x]  [s]
   [0]   4 null
   [1]   3 null
   [2]   2 null
   [3]   1 null
   [4]   0 null
   */

   int dynamic[];
   int dynamic2Dx4[][4];
   int fixed[][4] = {{1, 2, 3, 4}, {5, 6, 7, 8}};

   // duplicate array twice with exact and "flattened" structure
   ArrayCopy(dynamic, fixed);
   ArrayCopy(dynamic2Dx4, fixed);
   
   PRTS(ArrayReverse(fixed));
   ArrayPrint(fixed);
   /*
   ArrayReverse(fixed)=true / status:0
       [,0][,1][,2][,3]
   [0,]   5   6   7   8
   [1,]   1   2   3   4
   */
   
   PRTS(ArrayReverse(dynamic, 4, 3));
   ArrayPrint(dynamic);
   /*
   ArrayReverse(dynamic,4,3)=true / status:0
   1 2 3 4 7 6 5 8
   */

   PRTS(ArrayReverse(dynamic, 0, 1)); // does nothing (count = 1)
   ArrayPrint(dynamic);
   /*
   ArrayReverse(dynamic,0,1)=true / status:0
   1 2 3 4 7 6 5 8
   */
   
   PRTS(ArrayReverse(dynamic2Dx4, 2, 1)); // ERR_SMALL_ARRAY
   /*
   ArrayReverse(dynamic2Dx4,2,1)=false / status:5052
   */   
}
//+------------------------------------------------------------------+
