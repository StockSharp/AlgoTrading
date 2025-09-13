//+------------------------------------------------------------------+
//|                                              StmtSelectionIf.mq5 |
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
   if(Period() < PERIOD_D1)
   {
      Print("Intraday");
   }
   else
   {
      Print("Interday");
   }

   string s = "Hello, " + Symbol();
   int capital = 0, punctuation = 0;
   for(int i = 0; i < StringLen(s); ++i)
   {
      if(s[i] >= 'A' && s[i] <= 'Z')
         ++capital;
      else if(!(s[i] >= 'a' && s[i] <= 'z'))
         ++punctuation;

   }
   Print(capital, " ", punctuation);
}
//+------------------------------------------------------------------+
