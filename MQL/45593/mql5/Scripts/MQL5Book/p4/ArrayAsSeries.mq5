//+------------------------------------------------------------------+
//|                                                ArrayAsSeries.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRTS(A)  Print(#A, "=", (string)(A) + " / status:" + (string)GetLastError())
#define LIMIT 10

//+------------------------------------------------------------------+
//| Helper function to fill array with incremented indices           |
//+------------------------------------------------------------------+
template<typename T>
void indexArray(T &array[])
{
   for(int i = 0; i < ArraySize(array); ++i)
   {
      array[i] = (T)(i + 1);
   }
}

//+------------------------------------------------------------------+
//| Example class for array of objects                               |
//+------------------------------------------------------------------+
class Dummy
{
   int data[];
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   double array2D[][2];
   double fixed[LIMIT];
   double dynamic[];
   MqlRates rates[];
   Dummy dummies[];
   
   // allocate memory
   ArrayResize(dynamic, LIMIT);
   
   // fill arrays with test data: 1, 2, 3,...
   indexArray(fixed);
   indexArray(dynamic);

   // since all arrays are declared in the program,
   // they are NOT real series and will produce false
   PRTS(ArrayIsSeries(array2D)); // false
   PRTS(ArrayIsSeries(fixed));   // false
   PRTS(ArrayIsSeries(dynamic)); // false
   PRTS(ArrayIsSeries(rates));   // false
   // we'll see ArrayIsSeries=true in indicators (Part V)

   PRTS(ArrayGetAsSeries(array2D)); // false, can not be true
   
   // all the next are false by default,
   // because they are declared in this MQL-program
   PRTS(ArrayGetAsSeries(fixed));   // false
   PRTS(ArrayGetAsSeries(dynamic)); // false
   PRTS(ArrayGetAsSeries(rates));   // false
   PRTS(ArrayGetAsSeries(dummies)); // false

   // show elements in current order
   ArrayPrint(fixed, 1);
   ArrayPrint(dynamic, 1);
   /*
       1.0  2.0  3.0  4.0  5.0  6.0  7.0  8.0  9.0 10.0
       1.0  2.0  3.0  4.0  5.0  6.0  7.0  8.0  9.0 10.0
   */
   
   // error: parameter conversion not allowed
   // PRTS(ArraySetAsSeries(array2D, true));
   
   // warning: cannot be used for static allocated array
   PRTS(ArraySetAsSeries(fixed, true));   // false
   
   // next 3 are ok
   PRTS(ArraySetAsSeries(dynamic, true)); // true
   PRTS(ArraySetAsSeries(rates, true));   // true
   PRTS(ArraySetAsSeries(dummies, true)); // true

   // now check what's changed: first "real" series attribute
   PRTS(ArrayIsSeries(fixed));            // false
   PRTS(ArrayIsSeries(dynamic));          // false
   PRTS(ArrayIsSeries(rates));            // false
   PRTS(ArrayIsSeries(dummies));          // false
   
   // second, check "as series" index order
   PRTS(ArrayGetAsSeries(fixed));         // false
   PRTS(ArrayGetAsSeries(dynamic));       // true
   PRTS(ArrayGetAsSeries(rates));         // true
   PRTS(ArrayGetAsSeries(dummies));       // true
   
   // check how new order is applied for elements
   ArrayPrint(fixed, 1);    // remains the same
   ArrayPrint(dynamic, 1);  // altered order is in effect
   /*
       1.0  2.0  3.0  4.0  5.0  6.0  7.0  8.0  9.0 10.0
      10.0  9.0  8.0  7.0  6.0  5.0  4.0  3.0  2.0  1.0
   */
}
//+------------------------------------------------------------------+
