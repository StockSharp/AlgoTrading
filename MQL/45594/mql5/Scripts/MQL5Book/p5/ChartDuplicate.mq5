//+------------------------------------------------------------------+
//|                                               ChartDuplicate.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string temp = "/Files/ChartTemp";
   if(ChartSaveTemplate(0, temp))
   {
      const long id = ChartOpen(NULL, 0);
      if(!ChartApplyTemplate(id, temp))
      {
         Print("Apply Error: ", _LastError);
      }
   }
   else
   {
      Print("Save Error: ", _LastError);
   }
}
//+------------------------------------------------------------------+
