//+------------------------------------------------------------------+
//|                                                 Preprocessor.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

double array[] =
{
#include "preprocessor.txt"
};

//+------------------------------------------------------------------+
//| Replacement wrapper for OnStart function                         |
//+------------------------------------------------------------------+
void OnStart()
{
   Print(__FUNCTION__, " wrapper started");
   ArrayPrint(array);
   // ... do some work
   _OnStart();
   // ... do some work
   Print(__FUNCTION__, " wrapper done");
}

#define OnStart _OnStart
//+------------------------------------------------------------------+
