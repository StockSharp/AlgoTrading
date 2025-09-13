//+------------------------------------------------------------------+
//|                                                     StmtNull.mq5 |
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
   int i = 0;
   ushort c;
   string s = "Hello, " + Symbol();

   while((c = s[i++]) != ' ' && c != 0); // intentional ';' (!)

   if(c == ' ')
   {
      Print("Space found at: ", i);
   }
}
//+------------------------------------------------------------------+
