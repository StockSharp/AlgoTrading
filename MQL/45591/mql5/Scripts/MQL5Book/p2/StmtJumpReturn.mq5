//+------------------------------------------------------------------+
//|                                               StmtJumpReturn.mq5 |
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
   string s = "Hello, " + Symbol();
   const int n = StringLen(s);

   for(int i = 0; i < n; ++i)
   {
      for(int j = i + 1; j < n; ++j)
      {
         if(s[i] == s[j])
         {
            PrintFormat("Duplicate: %c", s[i]);
            return;
         }
      }
   }
   
   Print("No duplicates");
}
//+------------------------------------------------------------------+
