//+------------------------------------------------------------------+
//|                                                    ArrayCopy.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRTS(A) Print(#A, "=", (string)(A) + " / status:" + (string)GetLastError())

//+------------------------------------------------------------------+
//| Dummy class for objects/pointers examples                        |
//+------------------------------------------------------------------+
class Dummy
{
   int x;
};

//+------------------------------------------------------------------+
//| Simple struct to copy                                            |
//+------------------------------------------------------------------+
struct Simple
{
   int x;
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Dummy objects1[5], objects2[5];
   /*
   // error: structures or classes containing objects are not allowed
   PRTS(ArrayCopy(objects1, objects2));
   */
   
   // raw copy pointers to objects
   Dummy *pointers1[5], *pointers2[5];
   for(int i = 0; i < 5; ++i)
   {
      pointers1[i] = &objects1[i];
   }
   PRTS(ArrayCopy(pointers2, pointers1));
   for(int i = 0; i < 5; ++i)
   {
      Print(i, " ", pointers1[i], " ", pointers2[i]);
   }
   // will output the same instance ids of the objects
   /*
   ArrayCopy(pointers2,pointers1)=5 / status:0
   0 1048576 1048576
   1 2097152 2097152
   2 3145728 3145728
   3 4194304 4194304
   4 5242880 5242880
   */

   // simple structures are ok   
   Simple s1[3] = {{123}, {456}, {789}}, s2[];
   PRTS(ArrayCopy(s2, s1));
   ArrayPrint(s2);
   /*
   ArrayCopy(s2,s1)=3 / status:0
       [x]
   [0] 123
   [1] 456
   [2] 789
   */

   int dynamic[];
   int dynamic2Dx5[][5];
   int dynamic2Dx4[][4];
   int fixed[][4] = {{1, 2, 3, 4}, {5, 6, 7, 8}};
   int insert[] = {10, 11, 12};
   double array[1] = {M_PI};
   string texts[];
   string message[1] = {"ok"};
   
   // one number '2' will be copied from 'fixed',
   // and 3 more elements allocated, since 'fixed'
   // has 4 elements in a row (these 3 contain random data)
   PRTS(ArrayCopy(dynamic2Dx4, fixed, 0, 1, 1));
   ArrayPrint(dynamic2Dx4);
   /*
   ArrayCopy(dynamic2Dx4,fixed,0,1,1)=1 / status:0
       [,0][,1][,2][,3]
   [0,]   2   1   2   3
   */
   
   // all numbers starting from 3-th element of 'fixed'
   // will be placed at positions, starting from 1-st in 'dynamic2Dx4';
   // space for 2 more elements are allocated (with random data)
   // because 5 elements are copied (8 - 3), and 5 plus 1 (target offset)
   // makes 6, which needs 2 additional elements to complete a row
   // of 4 elements according to 2-d dimension of 'dynamic2Dx4'
   PRTS(ArrayCopy(dynamic2Dx4, fixed, 1, 3));
   ArrayPrint(dynamic2Dx4);
   /*
   ArrayCopy(dynamic2Dx4,fixed,1,3)=5 / status:0
       [,0][,1][,2][,3]
   [0,]   2   4   5   6
   [1,]   7   8   3   4
   */

   // allocate 'dynamic' for all elements in 'fixed'
   // copy as a flat 1D array
   PRTS(ArrayCopy(dynamic, fixed));
   ArrayPrint(dynamic);
   /*
   ArrayCopy(dynamic,fixed)=8 / status:0
   1 2 3 4 5 6 7 8
   */

   // allocate 'dynamic2Dx5' to get 3 elements of 'insert',
   // since 'dynamic2Dx5' is 2D with 5 elements in a row
   // 2 more elements will be allocated to complete the row,
   // they contain random data
   PRTS(ArrayCopy(dynamic2Dx5, insert));
   ArrayPrint(dynamic2Dx5);
   /*
   ArrayCopy(dynamic2Dx5,insert)=3 / status:0
       [,0][,1][,2][,3][,4]
   [0,]  10  11  12   4   5
   */

   // overwrite 'dynamic2Dx5' with elements from 'fixed'
   PRTS(ArrayCopy(dynamic2Dx5, fixed));
   ArrayPrint(dynamic2Dx5);
   /*
   ArrayCopy(dynamic2Dx5,fixed)=8 / status:0
       [,0][,1][,2][,3][,4]
   [0,]   1   2   3   4   5
   [1,]   6   7   8   0   0
   */

   // overwrite first 3 elements of 'fixed' with elements from 'insert'
   PRTS(ArrayCopy(fixed, insert));
   ArrayPrint(fixed);
   /*
   ArrayCopy(fixed,insert)=3 / status:0
       [,0][,1][,2][,3]
   [0,]  10  11  12   4
   [1,]   5   6   7   8
   */

   // overwrite last 3 elements of 'fixed' with elements from 'insert'
   PRTS(ArrayCopy(fixed, insert, 5));
   ArrayPrint(fixed);
   /*
   ArrayCopy(fixed,insert,5)=3 / status:0
       [,0][,1][,2][,3]
   [0,]  10  11  12   4
   [1,]   5  10  11  12
   */

   // attempt to copy 'insert' past the end of 'fixed',
   // since 'fixed' is not dynamic, it can not be expanded
   PRTS(ArrayCopy(fixed, insert, 8)); // 4006, ERR_INVALID_ARRAY
   ArrayPrint(fixed); // no change

   // string arrays are compatible with string arrays only
   PRTS(ArrayCopy(texts, insert)); // 5050, ERR_INCOMPATIBLE_ARRAYS
   ArrayPrint(texts); // no output
   
   ResetLastError();

   PRTS(ArrayCopy(texts, message));
   ArrayPrint(texts);
   /*
   ArrayCopy(texts,message)=1 / status:0
   "ok"
   */
   
   // double is converted to int, M_PI becomes 3 and replaces value '11'
   PRTS(ArrayCopy(insert, array, 1));
   ArrayPrint(insert);
   /*
   ArrayCopy(insert,array,1)=1 / status:0
   10  3 12
   */
}
//+------------------------------------------------------------------+
