//+------------------------------------------------------------------+
//|                                                  IndBarIndex.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2021, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "WARNINIG: this indicator is intentionally made with a malfunction!\n"
#property description "When run with SimulateCalculation=true it'll stop redraw"
                      " of existing and new indicators on the same symbol! "
                      "Do not forget to remove it from a chart, after examination of the problem!\n"
#property description "Normally, the indicator displays bar indices."

#property indicator_separate_window
#property indicator_buffers 1
#property indicator_plots   1
#property indicator_type1   DRAW_LINE
#property indicator_color1  DodgerBlue

// setting this input to true will freeze all indicators drawing on
// all charts of the same symbol, until you remove the indicator
input bool SimulateCalculation = false;

double Buffer[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   SetIndexBuffer(0, Buffer, INDICATOR_DATA);
   IndicatorSetInteger(INDICATOR_DIGITS, 0);
   if(SimulateCalculation)
   {
      EventSetTimer(1);
   }
}

//+------------------------------------------------------------------+
//| Timer handler                                                    |
//+------------------------------------------------------------------+
void OnTimer()
{
   Comment("Calculation started at ", TimeLocal());
   // the endless loop emulates lengthy calculations
   while(!IsStopped())
   {
   }
   Comment("");
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   if(prev_calculated <= 0)
   {
      ArrayInitialize(Buffer, EMPTY_VALUE);
   }
  
   const int N = MathMin(rates_total, TerminalInfoInteger(TERMINAL_MAXBARS));
  
   for(int i = MathMax(prev_calculated - 1, 0); i < N && !IsStopped(); i++)
   {
      Buffer[i] = i;
   }
  
   return rates_total;
}
//+------------------------------------------------------------------+
