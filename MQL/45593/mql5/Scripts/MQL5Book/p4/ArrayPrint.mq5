//+------------------------------------------------------------------+
//|                                                   ArrayPrint.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Small struct to use as a field in other structs                  |
//+------------------------------------------------------------------+
struct Pair
{
   int x, y;
};

//+------------------------------------------------------------------+
//| Struct with mostly plain type fields                             |
//+------------------------------------------------------------------+
struct SimpleStruct
{
   double value;
   datetime time;
   int count;
   ENUM_APPLIED_PRICE type;
   color clr;
   string details;
   void *ptr;
   Pair pair;
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int array1D[] = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
   double array2D[][5] = {{1, 2, 3, 4, 5}, {6, 7, 8, 9, 10}};
   double array3D[][3][5] =
   {
      {{ 1,  2,  3,  4,  5}, { 6,  7,  8,  9, 10}, {11, 12, 13, 14, 15}},
      {{16, 17, 18, 19, 20}, {21, 22, 23, 24, 25}, {26, 27, 28, 29, 30}},
   };

   Print("array1D");
   ArrayPrint(array1D);
   Print("array2D");
   ArrayPrint(array2D);
   Print("array3D");
   ArrayPrint(array3D);
   
   /*
   
      output (on EURUSD chart with 5 digit prices)
      
   array1D
    1  2  3  4  5  6  7  8  9 10
   array2D
            [,0]     [,1]     [,2]     [,3]     [,4]
   [0,]  1.00000  2.00000  3.00000  4.00000  5.00000
   [1,]  6.00000  7.00000  8.00000  9.00000 10.00000
   array3D
   
   */
   
   SimpleStruct simple[] =
   {
      { 12.57839, D'2021.07.23 11:15', 22345, PRICE_MEDIAN, clrBlue, "text message"},
      {135.82949, D'2021.06.20 23:45', 8569, PRICE_TYPICAL, clrAzure},
      { 1087.576, D'2021.05.15 10:01:30', -3298, PRICE_WEIGHTED, clrYellow, "note"},
   };
   Print("SimpleStruct (default)");
   ArrayPrint(simple);

   Print("SimpleStruct (custom)");
   ArrayPrint(simple, 3, ";", 0, WHOLE_ARRAY, ARRAYPRINT_DATE);
   
   /*
      output (on EURUSD chart with 5 digit prices)
   
   SimpleStruct (default)
          [value]              [time] [count] [type]    [clr]      [details] [ptr] [pair]
   [0]   12.57839 2021.07.23 11:15:00   22345      5 00FF0000 "text message"   ...    ...
   [1]  135.82949 2021.06.20 23:45:00    8569      6 00FFFFF0 null             ...    ...
   [2] 1087.57600 2021.05.15 10:01:30   -3298      7 0000FFFF "note"           ...    ...
   SimpleStruct (custom)
     12.578;2021.07.23;  22345;     5;00FF0000;"text message";  ...;   ...
    135.829;2021.06.20;   8569;     6;00FFFFF0;null          ;  ...;   ...
   1087.576;2021.05.15;  -3298;     7;0000FFFF;"note"        ;  ...;   ...
   
   */
}
//+------------------------------------------------------------------+
