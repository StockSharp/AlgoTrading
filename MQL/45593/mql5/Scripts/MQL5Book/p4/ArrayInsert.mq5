//+------------------------------------------------------------------+
//|                                                  ArrayInsert.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRTS(A) Print(#A, "=", (string)(A) + " / status:" + (string)GetLastError())

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int dynamic[];
   int dynamic2Dx5[][5];
   int dynamic2Dx4[][4];
   int fixed[][4] = {{1, 2, 3, 4}, {5, 6, 7, 8}};
   int insert[] = {10, 11, 12};
   int array[1] = {100};
   
   // 1D and 2D arrays can't be mixed
   PRTS(ArrayInsert(dynamic, fixed, 0)); // false:4006, ERR_INVALID_ARRAY
   ArrayPrint(dynamic); // empty
   // 2D arrays with different second dimension size can't be mixed
   PRTS(ArrayInsert(dynamic2Dx5, fixed, 0)); // false:4006, ERR_INVALID_ARRAY
   ArrayPrint(dynamic2Dx5); // empty
   // no matter if arrays are both fixed (or dynamic),
   // dimension number and higher sizes must match
   PRTS(ArrayInsert(fixed, insert, 0)); // false:4006, ERR_INVALID_ARRAY
   ArrayPrint(fixed); // doesn't change
   
   // target index is out of bound (10 > 3), can be 0, 1, 2 for 'insert'
   PRTS(ArrayInsert(insert, array, 10)); // false:5052, ERR_SMALL_ARRAY
   ArrayPrint(insert); // doesn't change
   
   // successful tests
   // second row from 'fixed' will be copied
   PRTS(ArrayInsert(dynamic2Dx4, fixed, 0, 1, 1)); // true
   ArrayPrint(dynamic2Dx4);
   // both rows of 'fixed' will be added to the end
   PRTS(ArrayInsert(dynamic2Dx4, fixed, 1)); // true
   ArrayPrint(dynamic2Dx4);
   // 'dynamic' will be allocated to get all element of 'insert'
   PRTS(ArrayInsert(dynamic, insert, 0)); // true
   ArrayPrint(dynamic);
   // 'dynamic' array will expand by 1 element
   PRTS(ArrayInsert(dynamic, array, 1)); // true
   ArrayPrint(dynamic);
   // new element will push off the last one from 'insert'
   PRTS(ArrayInsert(insert, array, 1)); // true
   ArrayPrint(insert);
}
//+------------------------------------------------------------------+
