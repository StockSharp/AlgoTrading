//+------------------------------------------------------------------+
//|                                                    ArrayFill.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int dynamic[];
   int fixed[][4] = {{1, 2, 3, 4}, {5, 6, 7, 8}};
   
   PRT(ArrayInitialize(fixed, -1));
   ArrayPrint(fixed);
   ArrayFill(fixed, 3, 4, +1);
   ArrayPrint(fixed);

   PRT(ArrayResize(dynamic, 10, 50));
   PRT(ArrayInitialize(dynamic, 0));
   ArrayPrint(dynamic);
   PRT(ArrayResize(dynamic, 50));
   ArrayPrint(dynamic);
   ArrayFill(dynamic, 10, 40, 0);
   ArrayPrint(dynamic);
   
   /*
      output
   
ArrayInitialize(fixed,-1)=8
    [,0][,1][,2][,3]
[0,]  -1  -1  -1  -1
[1,]  -1  -1  -1  -1
    [,0][,1][,2][,3]
[0,]  -1  -1  -1   1
[1,]   1   1   1  -1
ArrayResize(dynamic,10,50)=10
ArrayInitialize(dynamic,0)=10
0 0 0 0 0 0 0 0 0 0
ArrayResize(dynamic,50)=50
[ 0]           0           0           0           0           0
               0           0           0           0           0
[10] -1402885947  -727144693   699739629   172950740 -1326090126
           47384           0           0     4194184           0
[20]           2           0           2           0           0
               0           0  1765933056  2084602885 -1956758056
[30]    73910037 -1937061701          56           0          56
               0     1048601  1979187200       10851           0
[40]           0           0           0  -685178880 -1720475236
       782716519 -1462194191  1434596297   415166825 -1944066819
0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0
0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0
   
   */
}
//+------------------------------------------------------------------+
