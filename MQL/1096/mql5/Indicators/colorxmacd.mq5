/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+ 
//|                                                   ColorXMACD.mq5 | 
//|                               Copyright © 2011, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers 4
#property indicator_buffers 4 
//---- only two plots are used
#property indicator_plots   2
//+-----------------------------------+
//|  Parameters of indicator drawing  |
//+-----------------------------------+
//---- drawing the indicator as a four-color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- the following colors are used in the four color histogram
#property indicator_color1 Gray,Teal,BlueViolet,IndianRed,Magenta
//---- Indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "XMACD"

//---- drawing of the indicator as a three color line
#property indicator_type2 DRAW_COLOR_LINE
//---- the following colors are used in a three-colored line
#property indicator_color2 Gray,Lime,Red
//---- the indicator line is a dash-dotted curve
#property indicator_style2 STYLE_DASHDOTDOT
//---- the width of indicator line is 3
#property indicator_width2 3
//---- displaying label of the signal line
#property indicator_label2  "Signal Line"
//+-----------------------------------+
//|  Averagings classes description   |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2,XMA3;
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
   PRICE_TRENDFOLLOW1_   //TrendFollow_2 Price 
  };
/*enum Smooth_Method - the enumeration is declared in the SmoothAlgorithms.mqh file
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
//|  Input parameters of the indicator|
//+-----------------------------------+
input Smooth_Method MA_Method=MODE_T3; //histogram smoothing method
input int Fast_XMA = 12; ///Fast moving average period
input int Slow_XMA = 26; //Slow moving average period
input int Phase= 100;  //moving averages smoothing parameter
                       // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// for VIDIA it is a CMO period, for AMA it is a slow average period
input Smooth_Method Signal_Method=MODE_JJMA; //Signal line smoothing method
input int Signal_XMA=9; //signal line period 
input int Signal_Phase=100; // signal line parameter
                            //that changes within the range -100 ... +100,
//impacts the transitional process quality;
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
//+-----------------------------------+
//---- declaration of the integer variables for the start of data calculation
int start,macd_start;
//---- declaration of dynamic arrays that further 
//---- will be used as indicator buffers
double XMACDBuffer[],SignBuffer[],ColorXMACDBuffer[],ColorSignBuffer[];
//+------------------------------------------------------------------+
// The iPriceSeries function description                             |
// Moving_Average class description                                  | 
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+    
//| XMACD indicator initialization function                          | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   macd_start=MathMax(XMA1.GetStartBars(MA_Method,Fast_XMA,Phase),XMA1.GetStartBars(MA_Method,Slow_XMA,Phase));
   start=macd_start+XMA1.GetStartBars(Signal_Method,Signal_XMA,Signal_Phase);

//---- set XMACDBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,XMACDBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,macd_start);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"XMACD");
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- set dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorXMACDBuffer,INDICATOR_COLOR_INDEX);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,macd_start+1);

//---- set SignBuffer dynamic array as an indicator buffer
   SetIndexBuffer(2,SignBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,start);
//--- create a label to display in DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"Signal XMA");
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- set dynamic array as a color index buffer   
   SetIndexBuffer(3,ColorSignBuffer,INDICATOR_COLOR_INDEX);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,start+1);

//---- setting up alerts for unacceptable values of external parameters
   XMA1.XMALengthCheck("Fast_XMA", Fast_XMA);
   XMA1.XMALengthCheck("Slow_XMA", Slow_XMA);
   XMA1.XMALengthCheck("Signal_XMA", Signal_XMA);
//---- setting up alerts for unacceptable values of external parameters
   XMA1.XMAPhaseCheck("Phase", Phase, MA_Method);
   XMA1.XMAPhaseCheck("Signal_Phase", Signal_Phase, Signal_Method);

//---- initializations of variable for indicator short name
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(MA_Method);
   string Smooth2=XMA1.GetString_MA_Method(Signal_Method);
   StringConcatenate(shortname,
                     "XMACD( ",Fast_XMA,", ",Slow_XMA,", ",Signal_XMA,", ",Smooth1,", ",Smooth2," )");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+  
//| XMACD iteration function                                         | 
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
//---- Checking the number of bars to be enough for calculation
   if(rates_total<start) return(0);

//---- Declaration of integer variables
   int first1,first2,first3,bar;
//---- declaration of variables with a floating point  
   double price_,fast_xma,slow_xma,xmacd,sign_xma;

//---- Initialization of the indicator in the OnCalculate() block
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of calculation of an indicator
     {
      first1=0; // starting number for calculation of all first loop bars
      first2=macd_start+1; // starting index for calculation of all second loop bars
      first3=start+1; // starting index for calculation of all third loop bars
     }
   else // starting number for calculation of new bars
     {
      first1=prev_calculated-1;
      first2=first1;
      first3=first1;
     }

//---- Main cycle of calculation of the indicator
   for(bar=first1; bar<rates_total; bar++)
     {
      price_=PriceSeries(AppliedPrice,bar,open,low,high,close);;

      fast_xma = XMA1.XMASeries(0, prev_calculated, rates_total, MA_Method, Phase, Fast_XMA, price_, bar, false);
      slow_xma = XMA2.XMASeries(0, prev_calculated, rates_total, MA_Method, Phase, Slow_XMA, price_, bar, false);

      xmacd=fast_xma-slow_xma;
      sign_xma=XMA3.XMASeries(macd_start,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,xmacd,bar,false);

      //---- Loading the obtained values in the indicator buffers      
      XMACDBuffer[bar]= xmacd;
      SignBuffer[bar] = sign_xma;
     }

//---- Main loop of the XMACD indicator coloring
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorXMACDBuffer[bar]=0;

      if(XMACDBuffer[bar]>0)
        {
         if(XMACDBuffer[bar]>XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=1;
         if(XMACDBuffer[bar]<XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=2;
        }

      if(XMACDBuffer[bar]<0)
        {
         if(XMACDBuffer[bar]<XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=3;
         if(XMACDBuffer[bar]>XMACDBuffer[bar-1]) ColorXMACDBuffer[bar]=4;
        }
     }

//---- Main loop of the signal line coloring
   for(bar=first3; bar<rates_total; bar++)
     {
      ColorSignBuffer[bar]=0;
      if(XMACDBuffer[bar]>SignBuffer[bar-1]) ColorSignBuffer[bar]=1;
      if(XMACDBuffer[bar]<SignBuffer[bar-1]) ColorSignBuffer[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
