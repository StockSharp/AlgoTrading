//+------------------------------------------------------------------+
//|                                                       Arrays.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   char array[100];      // no initialization
   int array2D[3][2] =
   {
      {1, 2},            // format for better readability
      {3, 4},
      {5, 6}
   };
   int array2Dt[2][3] =
   {
      {1, 3, 5},
      {2, 4, 6}
   };
   ENUM_APPLIED_PRICE prices[] =
   {
      PRICE_OPEN, PRICE_HIGH, PRICE_LOW, PRICE_CLOSE
   };
   // double d[5] = {1, 2, 3, 4, 5, 6}; // error: too many initializers

   ArrayPrint(array);    // show some 'random' values in the log
   ArrayPrint(array2D);  // look how 2D-arrays logged
   ArrayPrint(array2Dt);
   ArrayPrint(prices);   // find out the price enum's values
}

//+------------------------------------------------------------------+
