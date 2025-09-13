//+------------------------------------------------------------------+
//|                                                   SpreadInfo.mq5 |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"
#property version   "1.00"
#property indicator_separate_window

#property indicator_buffers 4
#property indicator_plots 4

#property indicator_color1 White
#property indicator_color2 White
#property indicator_color3 White
#property indicator_color4 White

double Max[];
double Min[];
double ChangeAsk[];
double ChangeBid[];

double prevAsk, prevBid;

int OnInit()
{
   SetIndexBuffer(0, Max);
   SetIndexBuffer(1, Min);
   SetIndexBuffer(2, ChangeAsk);
   SetIndexBuffer(3, ChangeBid);
   
   PlotIndexSetDouble(0, PLOT_EMPTY_VALUE, 0);
   PlotIndexSetDouble(1, PLOT_EMPTY_VALUE, EMPTY_VALUE);
   PlotIndexSetDouble(2, PLOT_EMPTY_VALUE, 0);
   PlotIndexSetDouble(3, PLOT_EMPTY_VALUE, 0);
   
   ArraySetAsSeries(Max, true);
   ArraySetAsSeries(Min, true);
   ArraySetAsSeries(ChangeAsk, true);
   ArraySetAsSeries(ChangeBid, true);
   
   prevAsk = EMPTY_VALUE;
   prevBid = EMPTY_VALUE;
   
   return(0);
}

int OnCalculate(const int bars,
                const int counted,
                const datetime& time[],
                const double& open[],
                const double& high[],
                const double& low[],
                const double& close[],
                const long& tick_volume[],
                const long& volume[],
                const int& sprd[])
{
   double ask  = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   double bid  = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   
   double spread = MathRound((ask - bid)/_Point);
   
   if(Max[0] < spread) Max[0] = spread;
   if(Min[0] > spread) Min[0] = spread;
   
   if (prevAsk != EMPTY_VALUE && prevBid != EMPTY_VALUE)
   {
      ChangeAsk[0] = (ask > prevAsk) ? 1 : -1;
      ChangeBid[0] = (bid > prevBid) ? 1 : -1;
   }

   prevAsk = ask;
   prevBid = bid;
      
   return(bars);
}