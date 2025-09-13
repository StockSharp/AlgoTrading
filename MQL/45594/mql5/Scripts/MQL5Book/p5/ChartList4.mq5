//+------------------------------------------------------------------+
//|                                                   ChartList4.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/Periods.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ChartList();
}

//+------------------------------------------------------------------+
//| Main worker function to enumerate charts                         |
//+------------------------------------------------------------------+
void ChartList()
{
   const long me = ChartID();
   long id = ChartFirst();
   int count = 0, used = 0, temp, experts = 0, scripts = 0;
   
   Print("Chart List\nN, ID, Symbol, TF, #subwindows, *active, Windows handle");
   // keep enumerating all charts until no more found
   while(id != -1)
   {
      temp = 0; // MQL-program counter on every chart
      const int win = (int)ChartGetInteger(id, CHART_WINDOWS_TOTAL);
      const string header = StringFormat("%d %lld %s %s %s %s %s %s %lld",
         count, id, ChartSymbol(id), PeriodToString(ChartPeriod(id)),
         (win > 1 ? "#" + (string)(win - 1) : ""), (id == me ? "*" : ""),
         (ChartGetInteger(id, CHART_BRING_TO_TOP, 0) ? "active" : ""),
         (ChartGetInteger(id, CHART_IS_OBJECT) ? "object" : ""),
         ChartGetInteger(id, CHART_WINDOW_HANDLE));

      // columns: N, id, symbol, period, subwindow #number, self-mark, active status, object type, handle
      Print(header);

      string expert = ChartGetString(id, CHART_EXPERT_NAME);
      string script = ChartGetString(id, CHART_SCRIPT_NAME);
      if(StringLen(expert) > 0) expert = "[E] " + expert;
      if(StringLen(script) > 0) script = "[S] " + script;
      if(expert != NULL || script != NULL)
      {
         Print(expert, " ", script);
         if(expert != NULL) experts++;
         if(script != NULL) scripts++;
         temp++;
      }
      
      for(int i = 0; i < win; i++)
      {
         const bool visible = ChartGetInteger(id, CHART_WINDOW_IS_VISIBLE, i);
         if(!visible)
         {
            Print("  ", i, "/Hidden");
         }
      }

      count++;
      if(temp > 0)
      {
         used++;
      }
      id = ChartNext(id);
   }
   Print("Total chart number: ", count, ", with MQL-programs: ", used);
   Print("Experts: ", experts, ", Scripts: ", scripts);
}
//+------------------------------------------------------------------+
