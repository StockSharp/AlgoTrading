//+------------------------------------------------------------------+
//|                                                          Box.mq4 |
//|                        Copyright 2021, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Mueller Peter"
#property link      "https://www.mql5.com/en/users/mullerp04"
#property version   "1.00"
#property strict
#property indicator_chart_window
#property indicator_buffers 2
#property indicator_plots 2
#property indicator_type1 DRAW_LINE
#property indicator_type2 DRAW_LINE
#property indicator_color1 clrRed
#property indicator_color2 clrGreen
#property indicator_width1 2
#property indicator_width2 2
input int period = 20; //The period of the indicator
input int overlook =  10; //The overlook of period

bool Box = false;
bool Resistance = false;
bool Support = false;
double ResBuffer[];
double SuppBuffer[];

double Resistance(int starting, const double &high[])  //The resistance calculation function
{
      double high1 = high[ArrayMaximum(high,starting,period)];
      double high2 = high[ArrayMaximum(high,starting ,period + overlook)];
      if (high1 == high2) return high1;
      else return 0;
}

double Support(int starting, const double &low[])   // Support calculation
{
      double low1 = low[ArrayMinimum(low,starting,period)];
      double low2 = low[ArrayMinimum(low,starting,period + overlook)];
      if (low1 == low2) return low1;
      else return 0;
}

int OnInit()
   {
   SetIndexBuffer(0,ResBuffer);        // Initialising buffers
   SetIndexBuffer(1,SuppBuffer);
   int Start = (int) MathMax(period,overlook);
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,Start);      
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,Start);
   ArraySetAsSeries(ResBuffer,false);     // The indicator itself will go from last bar to first bar(we have to check "past bars" in order to calculate properly)
   ArraySetAsSeries(SuppBuffer,false);    // --> Buffers will not be series
   printf("ezt dobom fel");
   return(INIT_SUCCEEDED);
  }
  
  
bool IsNewCandle()
  {
   static datetime saved_candle_time;
   if(iTime(Symbol(),PERIOD_CURRENT,0) == saved_candle_time)
      return false;
   else
      saved_candle_time = iTime(Symbol(),PERIOD_CURRENT,0);
   return true;
  } 
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
   if(IsStopped())
         return 0;
   if(!IsNewCandle())
      return prev_calculated;
   int i;
   static double Savedmin = 0.0, Savedmax = 0.0;
   ArraySetAsSeries(high,true);     // high and low have to be series arrays so that they can be passed down to Support and Resistance functions
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(open,true);
   
   if((rates_total<= period + overlook) || (period <= 0) || (overlook < 0))
      return(0);
   for(i = 0; i < period+overlook;i++)
   {
       SuppBuffer[i] =EMPTY_VALUE;
       ResBuffer[i] =EMPTY_VALUE;
   }
   for(i = MathMax(period +overlook,prev_calculated-1); i<rates_total; i++)
   { 
      bool BounceUp = (Support(rates_total-i,low)*2 + high[ArrayMaximum(high,rates_total-i,period)])/3 < close[rates_total-i];     //bounceup check
      bool BounceBack = (low[ArrayMinimum(low,rates_total-i,period)] + Resistance(rates_total-i,high)*2)/3 > close[rates_total-i]; //bounceback check
      
      //PrintFormat("BounceBack: %d bounceup :%d", BounceBack,BounceUp);
      //PrintFormat("Resistance: %lf, Support: %lf",Resistance(rates_total-i,high),Support(rates_total-i,low));
      if(Support(rates_total-i,low) != 0.0 && BounceUp && (!Support || (Support(rates_total-i,low) >Savedmin && Savedmin != 0.0)))
      {
         SuppBuffer[i] = Support(rates_total-i,low);
         Savedmin = Support(rates_total-i,low);
         Support = true;
      }
      else if(low[rates_total-i] > Savedmin && Support)
      {
         SuppBuffer[i] = Savedmin;
      }
      else
      {
         SuppBuffer[i] = EMPTY_VALUE;
         Support = false;
         Savedmin = 0;
      }
     
      if(Resistance(rates_total-i,high) != 0.0 && BounceBack && (!Resistance || (Resistance(rates_total-i,high) < Savedmax && Savedmax != 0.0)))
      {
      
         //PrintFormat("In resist: resistance: %d, Savedmax: %lf ",Resistance,Savedmax);
         ResBuffer[i] = Resistance(rates_total-i,high);
         Savedmax = Resistance(rates_total-i,high);
         Resistance = true;
      }
      else if(high[rates_total-i] < Savedmax && Resistance)
      {
         ResBuffer[i] = Savedmax;
      }
      else
      {
         ResBuffer[i] = EMPTY_VALUE;
         Resistance =false;
         Savedmax = 0;
      }
   }
   return(rates_total);
  }
//+------------------------------------------------------------------