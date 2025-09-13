/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+ 
//|                                                        XMACD.mq5 | 
//|                               Copyright © 2010, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only two plots are used
#property indicator_plots   2
//+-----------------------------------+
//|  Parameters of indicator drawing  |
//+-----------------------------------+
//---- drawing the indicator as a histogram
#property indicator_type1 DRAW_HISTOGRAM
//---- MediumSlateBlue color is used as the color of the diagrams of the MACD indicator
#property indicator_color1 MediumSlateBlue
//---- the indicator line is a continuous curve
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "XMACD"

//---- drawing the indicator as a line
#property indicator_type2 DRAW_LINE
//---- Coral color is used as the color of the signal line
#property indicator_color2 Coral
//---- the indicator line is a dash-dotted curve
#property indicator_style2 STYLE_DASHDOTDOT
//---- Indicator line width is equal to 1
#property indicator_width2 2
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
input int Phase = 100;  //moving averages smoothing parameter,
                       // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// for VIDIA it is a CMO period, for AMA it is a slow average period
input Smooth_Method Signal_Method=MODE_JJMA; //Signal line smoothing method
input int Signal_XMA = 9; //signal line period 
input int Signal_Phase = 100; // signal line parameter,
                     //that changes within the range -100 ... +100,
//impacts the transitional process quality;
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
//+-----------------------------------+
//---- indicator buffers
double MACDBuffer[],SignBuffer[];

int start,macd_start; 
//+------------------------------------------------------------------+    
//| XMACD indicator initialization function                          | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   macd_start=MathMax(XMA1.GetStartBars(MA_Method, Fast_XMA, Phase),XMA1.GetStartBars(MA_Method, Slow_XMA, Phase));
   start=macd_start+XMA1.GetStartBars(Signal_Method, Signal_XMA, Signal_Phase);
   
//---- transformation of the dynamic array MACDBuffer into an indicator buffer
   SetIndexBuffer(0,MACDBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,macd_start+1);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"XMACD");
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- set SignBuffer dynamic array as an indicator buffer
   SetIndexBuffer(1,SignBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,start+1);
//--- create a label to display in DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Signal XMA");
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

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
   int first,bar;
//---- declaration of variables with a floating point  
   double price_,fast_xma,slow_xma,xmacd,sign_xma;

//---- Initialization of the indicator in the OnCalculate() block
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      first=0; // starting number for calculation of all bars
     }
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- Main cycle of calculation of the indicator
   for(bar=first; bar<rates_total; bar++)
     {
      price_=PriceSeries(AppliedPrice,bar,open,low,high,close);;

      fast_xma = XMA1.XMASeries(0, prev_calculated, rates_total, MA_Method, Phase, Fast_XMA, price_, bar, false);
      slow_xma = XMA2.XMASeries(0, prev_calculated, rates_total, MA_Method, Phase, Slow_XMA, price_, bar, false);

      xmacd=fast_xma-slow_xma;
      sign_xma=XMA3.XMASeries(macd_start,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,xmacd,bar,false);

      //---- Loading the obtained values in the indicator buffers      
      MACDBuffer[bar] = xmacd;
      SignBuffer[bar] = sign_xma;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
