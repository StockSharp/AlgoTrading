//+---------------------------------------------------------------------+ 
//|                                                      ColorRMACD.mq5 | 
//|                                  Copyright © 2010, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers is 4
#property indicator_buffers 4 
//---- only two plots are used
#property indicator_plots   2
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a four-color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- colors of the four-color histogram are as follows
#property indicator_color1 Gray,Teal,BlueViolet,IndianRed,Magenta
//---- indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "RMACD"

//---- drawing indicator as a three-colored line
#property indicator_type2 DRAW_COLOR_LINE
//---- colors of the three-color line are
#property indicator_color2 Gray,Lime,Red
//---- the indicator line is a dash-dotted curve
#property indicator_style2 STYLE_DASHDOTDOT
//---- the width of indicator line is 3
#property indicator_width2 3
//---- displaying label of the signal line
#property indicator_label2  "Signal Line"
//+-----------------------------------+
//|  Declaration of constants         |
//+-----------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
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
input uint Fast_RVI=12; //fast moving average period
input uint Slow_TRVI=26; //slow moving average period
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  //volume
input Smooth_Method Signal_Method=MODE_JJMA; //signal line smoothing method
input uint Signal_XMA=9; //signal line period 
input int Signal_Phase=100; // signal line parameter,
                            //that changes within the range -100 ... +100,
//depends of the quality of the transitional prices;
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input uint AlertCount=0;//number of alerts
//+-----------------------------------+
//---- Declaration of integer variables for storing indicator handles
int RVI_Handle,TRVI_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total,min_rates;
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double RMACDBuffer[],SignBuffer[],ColorRMACDBuffer[],ColorSignBuffer[];
//+------------------------------------------------------------------+
// The iPriceSeries function description                             |
// Moving_Average class description                                  | 
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+    
//| RMACD indicator initialization function                          | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates=int(MathMax(Fast_RVI,Slow_TRVI+8));
   min_rates_total=min_rates+XMA1.GetStartBars(Signal_Method,Signal_XMA,Signal_Phase);

//---- getting handle of the RVI indicator
   RVI_Handle=iRVI(NULL,0,Fast_RVI);
   if(RVI_Handle==INVALID_HANDLE) Print("Failed to get handle of RVI indicator");

//---- getting handle of the TRVI indicator
   TRVI_Handle=iCustom(NULL,0,"TRVI",Slow_TRVI,VolumeType,0);
   if(TRVI_Handle==INVALID_HANDLE) Print("Failed to get handle of TRVI indicator");

//---- set RMACDBuffer dynamic array as indicator buffer
   SetIndexBuffer(0,RMACDBuffer,INDICATOR_DATA);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(RMACDBuffer,true);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorRMACDBuffer,INDICATOR_COLOR_INDEX);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(ColorRMACDBuffer,true);

//---- set SignBuffer dynamic array as an indicator buffer
   SetIndexBuffer(2,SignBuffer,INDICATOR_DATA);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(SignBuffer,true);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(3,ColorSignBuffer,INDICATOR_COLOR_INDEX);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(ColorSignBuffer,true);

//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates+1);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- setting alerts for invalid values of external parameters
   XMA1.XMALengthCheck("Fast_RVI", Fast_RVI);
   XMA1.XMALengthCheck("Slow_TRVI", Slow_TRVI);
   XMA1.XMALengthCheck("Signal_XMA", Signal_XMA);
//---- setting alerts for invalid values of external parameters
   XMA1.XMAPhaseCheck("Signal_Phase",Signal_Phase,Signal_Method);

//---- initializations of variable for indicator short name
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(Signal_Method);
   StringConcatenate(shortname,"RMACD( ",Fast_RVI,", ",Slow_TRVI,", ",Smooth,", ",Signal_XMA," )");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+  
//| RMACD iteration function                                         | 
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
   if(BarsCalculated(RVI_Handle)<rates_total
      || BarsCalculated(TRVI_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int to_copy,limit,bar,maxbar;
   double RVI[],TRVI[];

   static uint UpCount,DnCount;
//---- declaration of variables with a floating point  
   double rmacd,sign_xma;

   maxbar=rates_total-1-min_rates;

//---- calculations of the necessary amount of data to be copied and
//the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=maxbar; // starting index for calculation of all bars
     }
   else
     {
      limit=rates_total-prev_calculated; // starting index for the calculation of new bars
     }

   to_copy=limit+1;

//---- copy newly appeared data into the arrays
   if(CopyBuffer(RVI_Handle,MAIN_LINE,0,to_copy,RVI)<=0) return(RESET);
   if(CopyBuffer(TRVI_Handle,SIGNAL_LINE,0,to_copy,TRVI)<=0) return(RESET);

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(RVI,true);
   ArraySetAsSeries(TRVI,true);

//---- Main calculation loop of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      rmacd=RVI[bar]-TRVI[bar];
      sign_xma=XMA1.XMASeries(maxbar,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,rmacd,bar,true);

      //---- Loading the obtained values in the indicator buffers      
      RMACDBuffer[bar]= rmacd;
      SignBuffer[bar] = sign_xma;
     }

   if(prev_calculated>rates_total || prev_calculated<=0) limit--;

//---- main loop of the RMACD indicator coloring
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ColorRMACDBuffer[bar]=0;

      if(RMACDBuffer[bar]>0)
        {
         if(RMACDBuffer[bar]>RMACDBuffer[bar+1]) ColorRMACDBuffer[bar]=1;
         if(RMACDBuffer[bar]<RMACDBuffer[bar+1]) ColorRMACDBuffer[bar]=2;
        }

      if(RMACDBuffer[bar]<0)
        {
         if(RMACDBuffer[bar]<RMACDBuffer[bar+1]) ColorRMACDBuffer[bar]=3;
         if(RMACDBuffer[bar]>RMACDBuffer[bar+1]) ColorRMACDBuffer[bar]=4;
        }
     }

   if(prev_calculated>rates_total || prev_calculated<=0) limit=rates_total-1-min_rates_total-1;

//---- Main loop of the signal line coloring
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ColorSignBuffer[bar]=0;
      if(RMACDBuffer[bar]>SignBuffer[bar+1]) ColorSignBuffer[bar]=1;
      if(RMACDBuffer[bar]<SignBuffer[bar+1]) ColorSignBuffer[bar]=2;
     }

//---- alerts counters reset to zeros
   if(rates_total!=prev_calculated)
     {
      UpCount=0;
      DnCount=0;
     }

   int bar1=1;
   int bar2=2;

//---- submission of an alert for buying
   if(UpCount<AlertCount && ColorSignBuffer[bar1]==2 && ColorSignBuffer[bar2]==1)
     {
      UpCount++;
      Alert("RMACD indicator "+Symbol()+EnumToString(PERIOD_CURRENT)+": ""Buy signal"+Symbol());
     }

//---- submission of an alert for selling
   if(DnCount<AlertCount && ColorSignBuffer[bar1]==1 && ColorSignBuffer[bar2]==2)
     {
      DnCount++;
      Alert("RMACD indicator "+Symbol()+EnumToString(PERIOD_CURRENT)+": ""Sell signal "+Symbol());
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
