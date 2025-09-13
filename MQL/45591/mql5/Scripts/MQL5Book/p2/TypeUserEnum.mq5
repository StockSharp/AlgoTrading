//+------------------------------------------------------------------+
//|                                                 TypeUserEnum.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

#define PRT(A) Print(#A, "=", (A))

const int zero = 0; // runtime value is not known at compile time

enum
{
   MILLION = 1000000 // value known at compile time
};

enum RISK
{
   // OFF      = zero, // error: constant expression required
   LOW      = -1,
   MODERATE = -2,
   HIGH     = -3,
};

enum INCOME
{
   LOW      = 1,
   MODERATE = 2,
   HIGH     = 3,
   ENORMOUS = MILLION,
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   enum INTERNAL
   {
      ON,
      OFF,
   };

   //int x = LOW; // warning: ambiguous access, can be one of
   int x = RISK::LOW;
   int y = INCOME::LOW;

   PRT(RISK::LOW);
   PRT(INCOME::LOW);
   PRT(OFF);
}

//int z = OFF; // error: 'OFF' - undeclared identifier
//+------------------------------------------------------------------+
