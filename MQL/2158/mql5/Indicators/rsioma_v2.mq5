//+---------------------------------------------------------------------+
//|                                                       RSIOMA_V2.mq5 | 
//|                                           Copyright © 2006, Kalenzo | 
//|                bartlomiej.gorski@gmail.com, http://www.fxservice.eu | 
//+---------------------------------------------------------------------+ 
//| Place the SmoothAlgorithms.mqh file                                 |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2006, Kalenzo"
#property link "http://www.fxservice.eu"
//--- Indicator version
#property version   "1.00"
//--- drawing the indicator in a separate window
#property indicator_separate_window
//--- number of indicator buffers is 7
#property indicator_buffers 7 
//--- five graphical plots are used
#property indicator_plots   5
//+----------------------------------------------+
//| Bullish indicator drawing parameters         |
//+----------------------------------------------+
//--- drawing the indicator as a histogram
#property indicator_type1   DRAW_HISTOGRAM
//--- Teal color is used for the indicator line
#property indicator_color1  clrTeal
//--- indicator 1 line width is equal to 2
#property indicator_width1  2
//--- displaying of the the indicator label
#property indicator_label1  "Upper"
//+----------------------------------------------+
//| Parameters of drawing the bearish indicator  |
//+----------------------------------------------+
//--- drawing the indicator 2 as a histogram
#property indicator_type2   DRAW_HISTOGRAM
//--- red color is used for the indicator line
#property indicator_color2  clrRed
//--- indicator 2 line width is equal to 2
#property indicator_width2  2
//--- displaying of the the indicator label
#property indicator_label2  "Lower"
//+----------------------------------------------+
//| Bullish indicator drawing parameters         |
//+----------------------------------------------+
//--- drawing the indicator 3 as a label
#property indicator_type3   DRAW_ARROW
//--- DeepSkyBlue color is used for the indicator
#property indicator_color3  clrDeepSkyBlue
//--- indicator 3 width is equal to 4
#property indicator_width3  4
//--- displaying the indicator label
#property indicator_label3  "Buy"
//+----------------------------------------------+
//| Parameters of drawing the bearish indicator  |
//+----------------------------------------------+
//--- drawing the indicator 4 as a label
#property indicator_type4   DRAW_ARROW
//--- magenta color is used for the indicator
#property indicator_color4  clrMagenta
//--- indicator 4 width is equal to 4
#property indicator_width4  4
//--- displaying the indicator label
#property indicator_label4  "Sell"
//+----------------------------------------------+
//| RSIOMA indicator drawing parameters          |
//+----------------------------------------------+
//--- drawing the indicator as a colored cloud
#property indicator_type5   DRAW_FILLING
//--- the following colors are used as the indicator colors
#property indicator_color5  clrLime,clrRed
//--- displaying the indicator label
#property indicator_label5  "RSIOMA"
//+----------------------------------------------+
//| CXMA class description                       |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2;
//--- declaration of variables of the class CMomentum from the file SmoothAlgorithms.mqh
CMomentum Mom;
//+----------------------------------------------+
//| declaration of enumerations                  |
//+----------------------------------------------+
enum Applied_price_      // type of constant
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPLE_,        // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price
   PRICE_DEMARK_         // Demark Price
  };
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input Smooth_Method RSIOMA_Method=MODE_EMA;   // RSIOMA averaging method
input uint RSIOMA=14;                         // Depth of averaging of RSIOMA                    
input int RSIOMAPhase=15;                     // RSIOMA averaging parameter
//--- RSIOMAPhase: for JJMA it varies within the range -100 ... +100 and influences the quality of the transient period;
//--- RSIOMAPhase: For VIDIA it is a CMO period, for AMA it is a slow average period
input Smooth_Method MARSIOMA_Method=MODE_EMA; // MARSIOMA averaging method
input uint MARSIOMA=21;                       // RSIOMA averaging depth                    
input int MARSIOMAPhase=15;                   // RSIOMA averaging parameter
//--- MARSIOMAPhase: for JJMA it varies within the range -100 ... +100 and influences the quality of the transient period;
//--- MARSIOMAPhase: For VIDIA it is a CMO period, for AMA it is a slow average period
input uint MomPeriod=1;                       // Period of Momentum
input Applied_price_ IPC=PRICE_CLOSE;         // Price constant
input uint BuyTrigger=80;                     // Buy level
input uint SellTrigger=20;                    // Sell level 
input color BuyTriggerColor=clrDodgerBlue;    // Buy color
input color SellTriggerColor=clrMagenta;      // Sell color
input uint MainTrendLong=60;                  // Upper trigger level
input uint MainTrendShort=40;                 // Lower trigger level
input color MainTrendLongColor=clrGreen;      // Uptrend color
input color MainTrendShortColor=clrRed;       // Downtrend color
input int Shift=0;                          // Horizontal shift of the indicator in bars 
//+----------------------------------------------+
//--- declaration of dynamic arrays that will be used as indicator buffers
double IndBuffer[],TriggerBuffer[];
double UpBuffer[],DnBuffer[];
double BuyBuffer[],SellBuffer[];
//--- declaration of integer variables for the start of data calculation
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//+------------------------------------------------------------------+   
//| RSIOMA indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//--- initialization of variables of the start of data calculation
   min_rates_1=XMA1.GetStartBars(RSIOMA_Method,RSIOMA,RSIOMAPhase);
   min_rates_2=int(min_rates_1+MomPeriod);
   min_rates_3=int(min_rates_2+RSIOMA+1);
   min_rates_total=min_rates_3;
//--- setting up alerts for unacceptable values of external variables
   XMA1.XMALengthCheck("RSIOMA",RSIOMA);
   XMA1.XMAPhaseCheck("RSIOMAPhase",RSIOMAPhase,RSIOMA_Method);
//--- setting up alerts for unacceptable values of external variables
   XMA1.XMALengthCheck("MARSIOMA",MARSIOMA);
   XMA1.XMAPhaseCheck("RSIOMAPhase",RSIOMAPhase,MARSIOMA_Method);
//--- set IndBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- set TriggerBuffer dynamic array as an indicator buffer
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- set IndBuffer dynamic array as an indicator buffer
   SetIndexBuffer(2,BuyBuffer,INDICATOR_DATA);
//--- set TriggerBuffer dynamic array as an indicator buffer
   SetIndexBuffer(3,SellBuffer,INDICATOR_DATA);
//--- set IndBuffer dynamic array as an indicator buffer
   SetIndexBuffer(4,IndBuffer,INDICATOR_DATA);
//--- set TriggerBuffer dynamic array as an indicator buffer
   SetIndexBuffer(5,TriggerBuffer,INDICATOR_DATA);
//--- shift the beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- shifting the indicator horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- shifting the start of drawing of the indicator
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- shifting the indicator horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- shift the beginning of indicator drawing
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- shifting the indicator horizontally by Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//--- shifting the start of drawing of the indicator 
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- shifting the indicator horizontally by Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//--- shifting the start of drawing of the indicator 
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//--- shifting the indicator horizontally by Shift
   PlotIndexSetInteger(4,PLOT_SHIFT,Shift);
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"RSIOMA");
//--- determining the accuracy of the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- the number of the indicator 4 horizontal levels   
   IndicatorSetInteger(INDICATOR_LEVELS,4);
//--- values of the indicator horizontal levels   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,BuyTrigger);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,SellTrigger);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,MainTrendLong);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,3,MainTrendShort);
//--- the following colors are used for horizontal levels lines
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,BuyTriggerColor);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,SellTriggerColor);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,MainTrendLongColor);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,3,MainTrendShortColor);
//--- short dot-dash is used for the horizontal level line  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,3,STYLE_DASHDOTDOT);
//--- initialization end
  }
//+------------------------------------------------------------------+ 
//| RSIOMA iteration function                                        | 
//+------------------------------------------------------------------+ 
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- checking if the number of bars is enough for the calculation
   if(rates_total<min_rates_total) return(0);
//--- declaration of variables with a floating point  
   double price,x1xma,rel,RSI,positive,negative,sump,sumn;
   static double prev_positive,prev_negative;
//--- declaration of integer variables and getting already calculated bars
   int first,bar;
//--- calculation of the starting number 'first' for the cycle of recalculation of bars
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
     {
      first=0; // starting number for calculation of all bars
      sumn=0.0;
      sump=0.0;
      int end=first+min_rates_3;
      for(bar=first; bar<end && !IsStopped(); bar++)
        {
         price=PriceSeries(IPC,bar,open,low,high,close);
         x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,RSIOMA_Method,RSIOMAPhase,RSIOMA,price,bar,false);
         rel=Mom.MomentumSeries(min_rates_1,prev_calculated,rates_total,MomPeriod,x1xma,bar,false);
         if(bar>=min_rates_2)
           {
            sump+=MathMax(rel,0);
            sumn-=MathMin(rel,0);
           }
        }
      prev_positive=sump/RSIOMA;
      prev_negative=sumn/RSIOMA;
      first+=min_rates_3;
     }
   else first=prev_calculated-1; // Starting index for the calculation of new bars
//--- main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,RSIOMA_Method,RSIOMAPhase,RSIOMA,price,bar,false);
      rel=Mom.MomentumSeries(min_rates_1,prev_calculated,rates_total,MomPeriod,x1xma,bar,false);

      sump=+MathMax(rel,0);
      sumn=-MathMin(rel,0);

      positive=(prev_positive*(RSIOMA-1)+sump)/RSIOMA;
      negative=(prev_negative*(RSIOMA-1)+sumn)/RSIOMA;

      if(negative) RSI=100.0-100.0/(1+positive/negative);
      else RSI=0.0;

      IndBuffer[bar]=RSI;
      TriggerBuffer[bar]=XMA2.XMASeries(min_rates_3,prev_calculated,rates_total,MARSIOMA_Method,MARSIOMAPhase,MARSIOMA,RSI,bar,false);

      if(bar<rates_total-1)
        {
         prev_positive=positive;
         prev_negative=negative;
        }

      UpBuffer[bar]=EMPTY_VALUE;
      DnBuffer[bar]=EMPTY_VALUE;
      BuyBuffer[bar]=EMPTY_VALUE;
      SellBuffer[bar]=EMPTY_VALUE;

      if(RSI>MainTrendLong) UpBuffer[bar]=-10;
      if(RSI<MainTrendShort) DnBuffer[bar]=-10;
      if(RSI<SellTrigger && RSI>IndBuffer[bar-1]) BuyBuffer[bar]=-15;
      if(RSI>BuyTrigger && RSI<IndBuffer[bar-1]) SellBuffer[bar]=-15;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
