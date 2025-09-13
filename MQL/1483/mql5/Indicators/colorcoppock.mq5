//+---------------------------------------------------------------------+ 
//|                                                    ColorCoppock.mq5 | 
//|                                     Copyright © 2010, EarnForex.com | 
//|                                           http://www.earnforex.com/ | 
//+---------------------------------------------------------------------+ 
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+ 
#property copyright "Copyright © 2010, EarnForex.com"
#property link "http://www.earnforex.com/" 
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a four-color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- colors of the four-color histogram are as follows
#property indicator_color1 clrDeepPink,clrFireBrick,clrGray,clrTeal,clrSpringGreen
//---- indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "Coppock"

//+-----------------------------------+
//|  Averaging classes description    |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1;
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
enum Applied_price_ //Type od constant
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
/*enum Smooth_Method - enumeration is declared in SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input int ROC1Period=14; //first smoothing period
input int ROC2Period=10; //second smoothing period
input Smooth_Method XMA_Method=MODE_LWMA; //histogram smoothing method
input int XMA_Period=12; //histogram smoothing period
input int XMA_Phase=100;  //histogram smoothing parameter,
                       // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
//+-----------------------------------+
//---- Declaration of integer variables of data starting point
int min_rates_,min_rates_total;
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double CoppBuffer[],ColorCoppBuffer[];
//+------------------------------------------------------------------+
// The iPriceSeries function description                             |
// Moving_Average class description                                  | 
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+    
//| Coppock indicator initialization function                        | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_=MathMax(ROC1Period,ROC2Period); 
   min_rates_total=min_rates_+XMA1.GetStartBars(XMA_Method,XMA_Period,XMA_Phase);

//---- set CoppBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,CoppBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+1);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorCoppBuffer,INDICATOR_COLOR_INDEX);
   
//---- initializations of variable for indicator short name
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"Coppock( ",ROC1Period,", ",ROC2Period,", ",XMA_Period,", ",XMA_Phase,", ",Smooth," )");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+  
//| Coppock iteration function                                       | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- Checking if the number of bars is sufficient for the calculation
   if(rates_total<min_rates_total) return(0);

//---- Declaration of integer variables
   int first,bar;
//---- declaration of variables with a floating point  
   double price_0,price_1,price_2,ROCSum,XROCSum;

//---- Initialization of the indicator in the OnCalculate() block
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      first=min_rates_; // starting number for the calculation of all bars in the first loop
     }
   else // starting number for the calculation of new bars
     {
      first=prev_calculated-1;
     }

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price_0=PriceSeries(AppliedPrice,bar,open,low,high,close);
      price_1=PriceSeries(AppliedPrice,bar-ROC1Period,open,low,high,close);
      price_2=PriceSeries(AppliedPrice,bar-ROC2Period,open,low,high,close);  
      ROCSum=(price_0-price_1)/price_1+(price_0-price_2)/price_2;          
      XROCSum=XMA1.XMASeries(min_rates_,prev_calculated,rates_total,XMA_Method,XMA_Phase,XMA_Period,ROCSum,bar,false);

      //---- loading the obtained values in the indicator buffer
      CoppBuffer[bar]=XROCSum;
      ColorCoppBuffer[bar]=2;

      if(XROCSum>0)
        {
         if(XROCSum>CoppBuffer[bar-1]) ColorCoppBuffer[bar]=4;
         if(XROCSum<CoppBuffer[bar-1]) ColorCoppBuffer[bar]=3;
        }

      if(XROCSum<0)
        {
         if(XROCSum<CoppBuffer[bar-1]) ColorCoppBuffer[bar]=0;
         if(XROCSum>CoppBuffer[bar-1]) ColorCoppBuffer[bar]=1;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
