//+------------------------------------------------------------------+
//|                                                  ArrayRemove.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRTS(A) Print(#A, "=", (string)(A) + " / status:" + (string)GetLastError())

//+------------------------------------------------------------------+
//| Simple struct                                                    |
//+------------------------------------------------------------------+
struct Simple
{
   int x;
};

//+------------------------------------------------------------------+
//| Looked like simple struct, but not so                            |
//+------------------------------------------------------------------+
struct NotSoSimple
{
   int x;
   string s; // string field makes compiler create implicit destructor
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Simple structs1[10];
   PRTS(ArrayRemove(structs1, 0, 5)); // true / status:0

   NotSoSimple structs2[10];
   PRTS(ArrayRemove(structs2, 0, 5)); // false / status:4005, ERR_STRUCT_WITHOBJECTS_ORCLASS
   
   ResetLastError();

   int dynamic[];
   int dynamic2Dx4[][4];
   int fixed[][4] = {{1, 2, 3, 4}, {5, 6, 7, 8}};
   
   // duplicate array twice with exact and "flattened" structure
   ArrayCopy(dynamic, fixed);
   ArrayCopy(dynamic2Dx4, fixed);

   // show original data   
   ArrayPrint(dynamic);
   /*
   1 2 3 4 5 6 7 8
   */
   ArrayPrint(dynamic2Dx4);
   /*
       [,0][,1][,2][,3]
   [0,]   1   2   3   4
   [1,]   5   6   7   8
   */
   
   PRTS(ArrayRemove(fixed, 0, 1));
   ArrayPrint(fixed);
   /*
   ArrayRemove(fixed,0,1)=true / status:0
       [,0][,1][,2][,3]
   [0,]   5   6   7   8
   [1,]   5   6   7   8
   */

   PRTS(ArrayRemove(dynamic2Dx4, 0, 1));
   ArrayPrint(dynamic2Dx4);
   /*
   ArrayRemove(dynamic2Dx4,0,1)=true / status:0
       [,0][,1][,2][,3]
   [0,]   5   6   7   8
   */

   PRTS(ArrayRemove(dynamic, 2, 3));
   ArrayPrint(dynamic);
   /*
   ArrayRemove(dynamic,2,3)=true / status:0
   1 2 6 7 8
   */

   PRTS(ArrayRemove(dynamic, 21));
   ArrayPrint(dynamic);
   /*
   ArrayRemove(dynamic,2,3)=false / status:5052, ERR_SMALL_ARRAY
   1 2 6 7 8
   */
}
//+------------------------------------------------------------------+
