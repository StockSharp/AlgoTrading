//+------------------------------------------------------------------+
//|                                                 ChartSynchro.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Run this script on 2 or more charts with the same or different   |
//| symbols, with the same or different timeframes.                  |
//| Then manual scrolling of one of these charts will automatically  |
//| scroll other charts to the same position.                        |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   datetime bar = 0; // current timestamp of the first visible bar

   const string namePosition = __FILE__; // global variable name
  
   ChartSetInteger(0, CHART_AUTOSCROLL, false); // disable autoscroll
  
   while(!IsStopped())
   {
      const bool active = ChartGetInteger(0, CHART_BRING_TO_TOP);
      const int move = (int)ChartGetInteger(0, CHART_FIRST_VISIBLE_BAR);

      // foreground chart is the main, the others follow it
      if(active)
      {
         const datetime first = iTime(_Symbol, _Period, move);
         if(first != bar)
         {
            // if position changed, store it in the global variable
            bar = first;
            GlobalVariableSet(namePosition, bar);
            Comment("Chart ", ChartID(), " scrolled to ", bar);
         }
      }
      else
      {
         const datetime b = (datetime)GlobalVariableGet(namePosition);
      
         if(b != bar)
         {
            // if the global variable changed, adjust position according to it
            bar = b;
            const int difference = move - iBarShift(_Symbol, _Period, bar);
            ChartNavigate(0, CHART_CURRENT_POS, difference);
            Comment("Chart ", ChartID(), " forced to ", bar);
         }
      }
    
      Sleep(250);
   }
   Comment("");
}
//+------------------------------------------------------------------+
