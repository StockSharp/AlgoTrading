//+---------------------------------------------------------------------+
//|                                                    Digital_MACD.mq5 |
//|                                        Copyright © 2006, CrazyChart |
//+---------------------------------------------------------------------+ 
//| Place the SmoothAlgorithms.mqh file                                 |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2006, CrazyChart"
#property link "" 
//--- Indicator version
#property version   "1.00"
//--- drawing the indicator in a separate window
#property indicator_separate_window 
//--- number of indicator buffers 4
#property indicator_buffers 4 
//--- two plots are used
#property indicator_plots   2
//+-----------------------------------+
//|  Indicator 1 drawing parameters   |
//+-----------------------------------+
//---- drawing indicator as a four-color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//--- the following colors are used in the four color histogram
#property indicator_color1 clrMagenta,clrIndianRed,clrGray,clrBlueViolet,clrDeepSkyBlue
//--- Indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//--- indicator line width is 2
#property indicator_width1 2
//--- displaying the indicator label
#property indicator_label1 "Digital MACD"
//+-----------------------------------+
//|  Indicator 2 drawing parameters   |
//+-----------------------------------+
//---- drawing the indicator as a three-colored line
#property indicator_type2 DRAW_COLOR_LINE
//---- the following colors are used in a three-color line
#property indicator_color2 clrRed,clrGray,clrLimeGreen
//--- Indicator line is a solid one
#property indicator_style2 STYLE_SOLID
//--- indicator line width is 3
#property indicator_width2 3
//---- displaying the signal line label
#property indicator_label2  "Signal Line"
//+-----------------------------------+
//|  Description of smoothing classes |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//--- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA;
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
enum Applied_price_      //Type of constant
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPLE_,         //Simple Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price 
  };
//+-----------------------------------+
//|  Indicator input parameters       |
//+-----------------------------------+
input Smooth_Method Signal_Method=MODE_SMA; //Signal line averaging method
input int Signal_XMA=5; //Signal line period 
input int Signal_Phase=100; // Signal line parameter
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//price constant
//+-----------------------------------+
//---- declaration of the integer variables for the start of data calculation
int min_rates_total,min_rates_1;
//--- declaration of dynamic arrays that
//--- will be used as indicator buffers
double DMACDBuffer[],SignBuffer[],ColorDMACDBuffer[],ColorSignBuffer[];
//+------------------------------------------------------------------+    
//| DMACD indicator initialization function                          | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- initialization of variables of data calculation start
   min_rates_1=66;
   min_rates_total=min_rates_1+XMA.GetStartBars(Signal_Method,Signal_XMA,Signal_Phase)+2;
//--- set DMACDBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,DMACDBuffer,INDICATOR_DATA);
//--- Setting a dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorDMACDBuffer,INDICATOR_COLOR_INDEX);
//--- set SignBuffer dynamic array as an indicator buffer
   SetIndexBuffer(2,SignBuffer,INDICATOR_DATA);
//--- Setting a dynamic array as a color index buffer   
   SetIndexBuffer(3,ColorSignBuffer,INDICATOR_COLOR_INDEX);
//--- shifting the start of drawing of the indicator
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- shifting the start of drawing of the indicator
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- setting up alerts for unacceptable values of external variables
   XMA.XMALengthCheck("Signal_XMA",Signal_XMA);
//--- setting up alerts for unacceptable values of external variables
   XMA.XMAPhaseCheck("Signal_Phase",Signal_Phase,Signal_Method);
//--- initializations of a variable for the indicator short name
   string shortname;
   string Smooth=XMA.GetString_MA_Method(Signal_Method);
   StringConcatenate(shortname,"Digital_MACD( ",Signal_XMA,", ",Smooth," )");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- initialization end
  }
//+------------------------------------------------------------------+  
//| DMACD iteration function                                         | 
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
//--- declaration of integer variables
   int first1,first2,first3,bar,clr;
//--- declaration of variables with a floating point  
   double fast_dma,slow_dma,dmacd,sign_dma;
//--- initialization of the indicator in the OnCalculate() block
   if(prev_calculated>rates_total || prev_calculated<=0)// Checking for the first start of the indicator calculation
     {
      first1=min_rates_1; // starting number for calculating all bars of the first loop
      first2=min_rates_1+1; // starting number for calculation of all bars of the second loop
      first3=min_rates_total+1; // starting index for calculation of all third loop bars
     }
   else // starting number for calculation of new bars
     {
      first1=prev_calculated-1;
      first2=first1;
      first3=first1;
     }
//--- main indicator calculation loop
   for(bar=first1; bar<rates_total; bar++)
     {
      fast_dma=
               0.2149840610*PriceSeries(AppliedPrice,bar-0,open,low,high,close)
               +0.2065763732*PriceSeries(AppliedPrice,bar-1,open,low,high,close)
               +0.1903728890*PriceSeries(AppliedPrice,bar-2,open,low,high,close)
               +0.1675422436*PriceSeries(AppliedPrice,bar-3,open,low,high,close)
               +0.1397053150*PriceSeries(AppliedPrice,bar-4,open,low,high,close)
               +0.1087951881*PriceSeries(AppliedPrice,bar-5,open,low,high,close)
               +0.0768869405*PriceSeries(AppliedPrice,bar-6,open,low,high,close)
               +0.0460244906*PriceSeries(AppliedPrice,bar-7,open,low,high,close)
               +0.0180517395*PriceSeries(AppliedPrice,bar-8,open,low,high,close)
               -0.0055294579*PriceSeries(AppliedPrice,bar-9,open,low,high,close)
               -0.0236660212*PriceSeries(AppliedPrice,bar-10,open,low,high,close)
               -0.0358140055*PriceSeries(AppliedPrice,bar-11,open,low,high,close)
               -0.0419497760*PriceSeries(AppliedPrice,bar-12,open,low,high,close)
               -0.0425331450*PriceSeries(AppliedPrice,bar-13,open,low,high,close)
               -0.0384279507*PriceSeries(AppliedPrice,bar-14,open,low,high,close)
               -0.0307917433*PriceSeries(AppliedPrice,bar-15,open,low,high,close)
               -0.0209443384*PriceSeries(AppliedPrice,bar-16,open,low,high,close)
               -0.0102335925*PriceSeries(AppliedPrice,bar-17,open,low,high,close)
               +0.0000932767*PriceSeries(AppliedPrice,bar-18,open,low,high,close)
               +0.0089950015*PriceSeries(AppliedPrice,bar-19,open,low,high,close)
               +0.0157131144*PriceSeries(AppliedPrice,bar-20,open,low,high,close)
               +0.0198149331*PriceSeries(AppliedPrice,bar-21,open,low,high,close)
               +0.0211989019*PriceSeries(AppliedPrice,bar-22,open,low,high,close)
               +0.0200639819*PriceSeries(AppliedPrice,bar-23,open,low,high,close)
               +0.0168532934*PriceSeries(AppliedPrice,bar-24,open,low,high,close)
               +0.0121825067*PriceSeries(AppliedPrice,bar-25,open,low,high,close)
               +0.0067474241*PriceSeries(AppliedPrice,bar-26,open,low,high,close)
               +0.0012444305*PriceSeries(AppliedPrice,bar-27,open,low,high,close)
               -0.0037087682*PriceSeries(AppliedPrice,bar-28,open,low,high,close)
               -0.0076300416*PriceSeries(AppliedPrice,bar-29,open,low,high,close)
               -0.0102110543*PriceSeries(AppliedPrice,bar-30,open,low,high,close)
               -0.0113306266*PriceSeries(AppliedPrice,bar-31,open,low,high,close)
               -0.0110462105*PriceSeries(AppliedPrice,bar-32,open,low,high,close)
               -0.0095662166*PriceSeries(AppliedPrice,bar-33,open,low,high,close)
               -0.0072080453*PriceSeries(AppliedPrice,bar-34,open,low,high,close)
               -0.0043494435*PriceSeries(AppliedPrice,bar-35,open,low,high,close)
               -0.0013771970*PriceSeries(AppliedPrice,bar-36,open,low,high,close)
               +0.0013575268*PriceSeries(AppliedPrice,bar-37,open,low,high,close)
               +0.0035760416*PriceSeries(AppliedPrice,bar-38,open,low,high,close)
               +0.0050946166*PriceSeries(AppliedPrice,bar-39,open,low,high,close)
               +0.0058339574*PriceSeries(AppliedPrice,bar-40,open,low,high,close)
               +0.0058160431*PriceSeries(AppliedPrice,bar-41,open,low,high,close)
               +0.0051486631*PriceSeries(AppliedPrice,bar-42,open,low,high,close)
               +0.0039984014*PriceSeries(AppliedPrice,bar-43,open,low,high,close)
               +0.0025619380*PriceSeries(AppliedPrice,bar-44,open,low,high,close)
               +0.0010531475*PriceSeries(AppliedPrice,bar-45,open,low,high,close)
               -0.0003481453*PriceSeries(AppliedPrice,bar-46,open,low,high,close)
               -0.0014937154*PriceSeries(AppliedPrice,bar-47,open,low,high,close)
               -0.0022905986*PriceSeries(AppliedPrice,bar-48,open,low,high,close)
               -0.0027000514*PriceSeries(AppliedPrice,bar-49,open,low,high,close)
               -0.0027359080*PriceSeries(AppliedPrice,bar-50,open,low,high,close)
               -0.0024543322*PriceSeries(AppliedPrice,bar-51,open,low,high,close)
               -0.0019409837*PriceSeries(AppliedPrice,bar-52,open,low,high,close)
               -0.0012957482*PriceSeries(AppliedPrice,bar-53,open,low,high,close)
               -0.0006179734*PriceSeries(AppliedPrice,bar-54,open,low,high,close)
               +0.0000057542*PriceSeries(AppliedPrice,bar-55,open,low,high,close)
               +0.0005111297*PriceSeries(AppliedPrice,bar-56,open,low,high,close)
               +0.0008605279*PriceSeries(AppliedPrice,bar-57,open,low,high,close)
               +0.0010441921*PriceSeries(AppliedPrice,bar-58,open,low,high,close)
               +0.0010775684*PriceSeries(AppliedPrice,bar-59,open,low,high,close)
               +0.0009966494*PriceSeries(AppliedPrice,bar-60,open,low,high,close)
               +0.0008537300*PriceSeries(AppliedPrice,bar-61,open,low,high,close)
               +0.0007142855*PriceSeries(AppliedPrice,bar-62,open,low,high,close)
               +0.0006599146*PriceSeries(AppliedPrice,bar-63,open,low,high,close)
               -0.0008151017*PriceSeries(AppliedPrice,bar-64,open,low,high,close);
      //---
      slow_dma=
               0.0825641231*PriceSeries(AppliedPrice,bar-0,open,low,high,close)
               +0.0822783080*PriceSeries(AppliedPrice,bar-1,open,low,high,close)
               +0.0814249974*PriceSeries(AppliedPrice,bar-2,open,low,high,close)
               +0.0800166909*PriceSeries(AppliedPrice,bar-3,open,low,high,close)
               +0.0780735197*PriceSeries(AppliedPrice,bar-4,open,low,high,close)
               +0.0756232268*PriceSeries(AppliedPrice,bar-5,open,low,high,close)
               +0.0727009740*PriceSeries(AppliedPrice,bar-6,open,low,high,close)
               +0.0693478349*PriceSeries(AppliedPrice,bar-7,open,low,high,close)
               +0.0656105823*PriceSeries(AppliedPrice,bar-8,open,low,high,close)
               +0.0615409157*PriceSeries(AppliedPrice,bar-9,open,low,high,close)
               +0.0571939540*PriceSeries(AppliedPrice,bar-10,open,low,high,close)
               +0.0526285643*PriceSeries(AppliedPrice,bar-11,open,low,high,close)
               +0.0479025123*PriceSeries(AppliedPrice,bar-12,open,low,high,close)
               +0.0430785482*PriceSeries(AppliedPrice,bar-13,open,low,high,close)
               +0.0382152880*PriceSeries(AppliedPrice,bar-14,open,low,high,close)
               +0.0333706133*PriceSeries(AppliedPrice,bar-15,open,low,high,close)
               +0.0286021160*PriceSeries(AppliedPrice,bar-16,open,low,high,close)
               +0.0239614376*PriceSeries(AppliedPrice,bar-17,open,low,high,close)
               +0.0194972056*PriceSeries(AppliedPrice,bar-18,open,low,high,close)
               +0.0152532583*PriceSeries(AppliedPrice,bar-19,open,low,high,close)
               +0.0112682658*PriceSeries(AppliedPrice,bar-20,open,low,high,close)
               +0.0075745482*PriceSeries(AppliedPrice,bar-21,open,low,high,close)
               +0.0041980052*PriceSeries(AppliedPrice,bar-22,open,low,high,close)
               +0.0011588603*PriceSeries(AppliedPrice,bar-23,open,low,high,close)
               -0.0015292889*PriceSeries(AppliedPrice,bar-24,open,low,high,close)
               -0.0038593393*PriceSeries(AppliedPrice,bar-25,open,low,high,close)
               -0.0058303888*PriceSeries(AppliedPrice,bar-26,open,low,high,close)
               -0.0074473108*PriceSeries(AppliedPrice,bar-27,open,low,high,close)
               -0.0087203043*PriceSeries(AppliedPrice,bar-28,open,low,high,close)
               -0.0096645874*PriceSeries(AppliedPrice,bar-29,open,low,high,close)
               -0.0102995666*PriceSeries(AppliedPrice,bar-30,open,low,high,close)
               -0.0106483424*PriceSeries(AppliedPrice,bar-31,open,low,high,close)
               -0.0107374524*PriceSeries(AppliedPrice,bar-32,open,low,high,close)
               -0.0105952115*PriceSeries(AppliedPrice,bar-33,open,low,high,close)
               -0.0102516944*PriceSeries(AppliedPrice,bar-34,open,low,high,close)
               -0.0097377645*PriceSeries(AppliedPrice,bar-35,open,low,high,close)
               -0.0090838346*PriceSeries(AppliedPrice,bar-36,open,low,high,close)
               -0.0083237046*PriceSeries(AppliedPrice,bar-37,open,low,high,close)
               -0.0074804382*PriceSeries(AppliedPrice,bar-38,open,low,high,close)
               -0.0065902734*PriceSeries(AppliedPrice,bar-39,open,low,high,close)
               -0.0056742995*PriceSeries(AppliedPrice,bar-40,open,low,high,close)
               -0.0047554314*PriceSeries(AppliedPrice,bar-41,open,low,high,close)
               -0.0038574209*PriceSeries(AppliedPrice,bar-42,open,low,high,close)
               -0.0029983549*PriceSeries(AppliedPrice,bar-43,open,low,high,close)
               -0.0021924972*PriceSeries(AppliedPrice,bar-44,open,low,high,close)
               -0.0014513858*PriceSeries(AppliedPrice,bar-45,open,low,high,close)
               -0.0007848072*PriceSeries(AppliedPrice,bar-46,open,low,high,close)
               -0.0001995891*PriceSeries(AppliedPrice,bar-47,open,low,high,close)
               +0.0003009728*PriceSeries(AppliedPrice,bar-48,open,low,high,close)
               +0.0007162164*PriceSeries(AppliedPrice,bar-49,open,low,high,close)
               +0.0010478905*PriceSeries(AppliedPrice,bar-50,open,low,high,close)
               +0.0012994016*PriceSeries(AppliedPrice,bar-51,open,low,high,close)
               +0.0014755433*PriceSeries(AppliedPrice,bar-52,open,low,high,close)
               +0.0015824007*PriceSeries(AppliedPrice,bar-53,open,low,high,close)
               +0.0016272598*PriceSeries(AppliedPrice,bar-54,open,low,high,close)
               +0.0016185271*PriceSeries(AppliedPrice,bar-55,open,low,high,close)
               +0.0015648336*PriceSeries(AppliedPrice,bar-56,open,low,high,close)
               +0.0014747659*PriceSeries(AppliedPrice,bar-57,open,low,high,close)
               +0.0013569946*PriceSeries(AppliedPrice,bar-58,open,low,high,close)
               +0.0012193896*PriceSeries(AppliedPrice,bar-59,open,low,high,close)
               +0.0010695971*PriceSeries(AppliedPrice,bar-60,open,low,high,close)
               +0.0009140878*PriceSeries(AppliedPrice,bar-61,open,low,high,close)
               +0.0007591540*PriceSeries(AppliedPrice,bar-62,open,low,high,close)
               +0.0016019033*PriceSeries(AppliedPrice,bar-63,open,low,high,close);

      dmacd=(fast_dma-slow_dma)/_Point;
      sign_dma=XMA.XMASeries(min_rates_1,prev_calculated,rates_total,Signal_Method,Signal_Phase,Signal_XMA,dmacd,bar,false);
      //--- Loading the obtained values in the indicator buffers      
      DMACDBuffer[bar]=dmacd;
      SignBuffer[bar]=sign_dma;
     }
//--- Main loop of the DMACD indicator coloring
   for(bar=first2; bar<rates_total; bar++)
     {
      clr=2;

      if(DMACDBuffer[bar]>0)
        {
         if(DMACDBuffer[bar]>DMACDBuffer[bar-1]) clr=4;
         if(DMACDBuffer[bar]<DMACDBuffer[bar-1]) clr=3;
        }

      if(DMACDBuffer[bar]<0)
        {
         if(DMACDBuffer[bar]<DMACDBuffer[bar-1]) clr=0;
         if(DMACDBuffer[bar]>DMACDBuffer[bar-1]) clr=1;
        }

      ColorDMACDBuffer[bar]=clr;
     }
//--- main loop of the signal line coloring
   for(bar=first3; bar<rates_total; bar++)
     {
      clr=1;     
      if(DMACDBuffer[bar]>SignBuffer[bar-1]) clr=2;
      if(DMACDBuffer[bar]<SignBuffer[bar-1]) clr=0;   
      ColorSignBuffer[bar]=clr;
     }
//---
   return(rates_total);
  }
//+------------------------------------------------------------------+
