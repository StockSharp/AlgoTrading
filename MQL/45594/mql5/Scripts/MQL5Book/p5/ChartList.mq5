//+------------------------------------------------------------------+
//|                                                    ChartList.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/Periods.mqh>

input bool IncludeEmptyCharts = true;
input bool IncludeHiddenWindows = true;

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
   int count = 0, used = 0, temp, experts = 0, scripts = 0, indicators = 0, subs = 0;
  
   Print("Chart List\nN, ID, Symbol, TF, #subwindows, *active, Windows handle");
  
   // keep enumerating all charts until no more found
   while(id != -1)
   {
      temp = 0; // MQL-program counter on every chart
      const int win = (int)ChartGetInteger(id, CHART_WINDOWS_TOTAL);
    
      const string header = StringFormat("%d %lld %s %s %s %s %s %s %lld",
         count, id, ChartSymbol(id), PeriodToString(ChartPeriod(id)),
         (win > 1 ? "#" + (string)(win - 1) : ""), (id == me ? " *" : ""),
         (ChartGetInteger(id, CHART_BRING_TO_TOP, 0) ? "active" : ""),
         (ChartGetInteger(id, CHART_IS_OBJECT) ? "object" : ""),
         ChartGetInteger(id, CHART_WINDOW_HANDLE));
    
      // columns: N, id, symbol, period, subwindow #number, self-mark, active status, object type, handle
      // addendum: expert, script, indicators (in main window and subwindows)

      string expert = ChartGetString(id, CHART_EXPERT_NAME);
      string script = ChartGetString(id, CHART_SCRIPT_NAME);
      if(StringLen(expert) > 0) expert = "[E] " + expert;
      if(StringLen(script) > 0) script = "[S] " + script;
      if(expert != NULL || script != NULL)
      {
         Print(header);
         Print(expert, " ", script);
         if(expert != NULL) experts++;
         if(script != NULL) scripts++;
         temp++;
      }
    
      for(int i = 0; i < win; i++)
      {
         const bool visible = ChartGetInteger(id, CHART_WINDOW_IS_VISIBLE, i);
         if(!visible && !IncludeHiddenWindows) continue;
         if(!visible)
         {
            Print("  ", i, "/Hidden");
         }
         const int n = ChartIndicatorsTotal(id, i);
         for(int k = 0; k < n; k++)
         {
            if(temp == 0)
            {
               Print(header);
            }
            Print("  ", i, "/", k, " [I] ", ChartIndicatorName(id, i, k));
            indicators++;
            if(i > 0) subs++;
            temp++;
         }
      }
    
      count++;
      if(temp > 0)
      {
         used++;
      }
      else
      {
         if(IncludeEmptyCharts) Print(header);
      }
      id = ChartNext(id);
   }
   Print("Total chart number: ", count, ", with MQL-programs: ", used);
   Print("Experts: ", experts, ", Scripts: ", scripts, ", Indicators: ", indicators, " (main: ", indicators - subs, " / sub: ", subs, ")");
}
//+------------------------------------------------------------------+
/*

   Chart List
   N, ID, Symbol, TF, #subwindows, *active, Windows handle
   0 132358585987782873 EURUSD M15 #1    133538
     1/0 [I] ATR(11)
   1 132360375330772909 EURUSD D1     133514
   2 132544239145024745 EURUSD M15   *   395646
    [S] ChartList
   3 132544239145024732 USDRUB D1     395688
   4 132544239145024744 EURUSD H1 #2  active  2361730
     1/0 [I] %R(14)
     2/Hidden
     2/0 [I] Momentum(15)
   5 132544239145024746 EURUSD H1     133584
   Total chart number: 6, with MQL-programs: 3
   Experts: 0, Scripts: 1, Indicators: 3 (main: 0 / sub: 3)

*/