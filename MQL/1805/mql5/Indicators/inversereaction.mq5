//+------------------------------------------------------------------+
//|                                              InverseReaction 1.2 |
//|                                              2013-2014 Erdem SEN |
//|                         http://login.mql5.com/en/users/erdogenes |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013-2014, Erdem Sen"
#property version   "1.2"
#property link      "http://login.mql5.com/en/users/erdogenes"
//---
#property description "This indicator is based on the idea of that an unusual impact" 
#property description "in price changes will be adjusted by an inverse reaction."
#property description "The signal comes out when the price-change exceeds the possible"
#property description "volatility limits, then you can expect an inverse reaction."

//--- Indicator
#property indicator_separate_window
#property indicator_buffers 3
#property indicator_plots   3

//--- PriceChanges Plot
#property indicator_label1  "PriceChanges"
#property indicator_type1   DRAW_HISTOGRAM
#property indicator_color1  clrBlueViolet

//--- UpperLevel Plot 
#property indicator_label2  "UpperLevel"
#property indicator_type2   DRAW_LINE
#property indicator_color2  clrRed

//--- LowerLevel Plot
#property indicator_label3  "LowerLevel"
#property indicator_type3   DRAW_LINE
#property indicator_color3  clrGreen

//--- ZeroLevel
#property indicator_level1 0
#property indicator_levelcolor clrBlueViolet

//---inputs
input int      MaPeriod    = 3;     // Moving average period
input double   Coefficient = 1.618; // Confidence coefficient

//--- global variables
int      drawstart,calcstart;
//--- buffers
double   PriceChanges[],u_DCL[],l_DCL[];

//---initialization functiom-----------------------------------------+
int OnInit()
  {
//--- main buffers
   SetIndexBuffer(0,PriceChanges,INDICATOR_DATA);
   SetIndexBuffer(1,u_DCL,INDICATOR_DATA);
   SetIndexBuffer(2,l_DCL,INDICATOR_DATA); 
//--- determine the start point for plotting
   drawstart  = MaPeriod-1;
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,drawstart);
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,drawstart);
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,drawstart); 
//--- set a shortname
   string name = StringFormat("InverseReaction (%d, %.3f)",MaPeriod,Coefficient);
   IndicatorSetString(INDICATOR_SHORTNAME,name);
//--- set digits for vertical axis
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---
   return(INIT_SUCCEEDED);
  }  
//---iteration function----------------------------------------------+

int OnCalculate(const int rates_total,const int prev_calculated,const datetime &time[],
                const double &open[],const double &high[],const double &low[],const double &close[],
                const long &tick_volume[],const long &volume[],const int &spread[])
  {
   //--- for avoiding recalculation
   if(prev_calculated>rates_total || prev_calculated<=0) calcstart=0; 
   else calcstart=prev_calculated-1; 
   //--- check if there is enoug bars
   if(calcstart==0 && rates_total<MaPeriod) 
     {   Print("Sorry!!, there are not enough bars. Download more historical data and retry");
         return(0);
     }
   //--- calculate the buffers
   for(int i=calcstart;i<rates_total;i++)
      {  PriceChanges[i]=(close[i]-open[i]);
         u_DCL[i]= DynamicConfidenceLevel(i,MaPeriod,Coefficient,PriceChanges);
         l_DCL[i]= -u_DCL[i];
      }            
//---
   return(rates_total);
  }

/*-------------------------------------------------------------------
Dynamic Confidence Levels (DCL)

   To determine DCLs, first, moving standard deviation (MStD) must be calculated. With the assumption of 
   "PERFECT NORMALITY CONDITIONS" in price changes, and using absolute values with "HALF NORMAL" 
   distribution method, MStD can be calculated by MA:
   Ex:---------------------------------------------------| 
   |    MStD = Sqrt[Pi]/Sqrt[2] * MA[Abs[Price Changes]] |
   |    GoldenRatio ~= z[%80] * Sqrt[Pi]/Sqrt[2]         |
   |    DCL[%80]~= GoldenRatio * MA[Abs[Price Changes]]  |
   |-----------------------------------------------------|
   With large numbers of MaPeriod, DCL aproximates to static ConfidenceLevel for normal distribution, 
   However the system is dynamic and memory is very short for such economic behavours, 
   so it set with a small number: 3 as default. (!!! plus, considering a possible HEAVY-TAIL problem, 
   small values of MaPeriod will relatively response better.)
*/

double DynamicConfidenceLevel(const int position,const int period,const double coef,const double &price[])
  {   double result=0.0;
      if(position>=period-1 && period>0)
         {  for(int i=0;i<period;i++) result+=fabs(price[position-i]);
            result /= period;
            result *= coef;
         }
   return(result);
  }
//-------------------------------------------------------------------