//+------------------------------------------------------------------+
//|                                           exOnTickMarketWatch.mq5|
//|                                            Copyright 2010, Lizar |
//|                                               Lizar-2010@mail.ru |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2010, Lizar"
#property link        "Lizar-2010@mail.ru"
#property version     "1.00"

//+------------------------------------------------------------------+
//| Event handler function                                           |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,         // event id or 1000+symbol index
                  const long& spread,   // spread
                  const double& bid,    // bid price
                  const string& symbol) // symbol
  {
   Print(" New tick on the symbol ",symbol," index in the list=",id," bid=",bid," spread=",spread);
  }

