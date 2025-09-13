//+------------------------------------------------------------------+
//|                                          StmtSelectionSwitch.mq5 |
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
   for(int i = 0; i < 7; i++)
   {
      double factor = 1.0;
      
      switch(i)
      {
         case -1:
            Print("-1: Never hit");
            break;
   
         case 1:
            Print("Case 1");
            factor = 1.5;
            break;
   
         case 2: // fall-through, no break (!)
            Print("Case 2");
            factor *= 2;
   
         case 3: // same statements for 3 and 4
         case 4:
            Print("Case 3 & 4");
            {
               double local_var = i * i;
               factor *= local_var;
            }
            break;
   
         case 5:
            Print("Case 5");
            factor = 100;
            break;
   
         default:
            Print("Default: ", i);
      }
      
      Print(factor);
   }
}
//+------------------------------------------------------------------+
