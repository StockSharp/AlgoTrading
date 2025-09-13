//+---------------------------------------------------------------------+
//|                                                             ZPF.mq5 | 
//|                                     Copyright © 2010, Ivan Kornilov |
//|                                            E-mail: excelf@gmail.com |
//+---------------------------------------------------------------------+
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+

#property copyright "Copyright © 2010, Ivan Kornilov"
#property link      "E-mail: excelf@gmail.com"
#property description "ZPF"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  Parameters of drawing the cloud odindicator |
//+----------------------------------------------+
//---- drawing the indicator 1 as a cloud
#property indicator_type1   DRAW_FILLING
//---- the following colors are used for the indicator
#property indicator_color1  clrRed,clrTeal
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying of the bearish label of the indicator
#property indicator_label1  "ZPF"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level2   0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+

//+-----------------------------------+
//|  CXMA class description           |
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
input Smooth_Method XMA_Method=MODE_SMA; //first smoothing averaging method 
input int XLength=12; //first smoothing depth                    
input int XPhase=15; //first smoothing parameter,
                     // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ IPC=PRICE_CLOSE;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  //volume
input int Shift=0; // horizontal shift of the indicator in bars
//+-----------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double Line1Buffer[];
double Line2Buffer[];
//---- Declaration of integer variables of data starting point
int min_rates_total,min_rates_1,min_rates_2,X2Length;
//+------------------------------------------------------------------+   
//| X2MA indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   X2Length=2*XLength;
   min_rates_total=XMA2.GetStartBars(XMA_Method,X2Length,XPhase);
//---- setting alerts for invalid values of external parameters
   XMA1.XMALengthCheck("XLength",XLength);
//---- setting alerts for invalid values of external parameters
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,Line1Buffer,INDICATOR_DATA);
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,Line2Buffer,INDICATOR_DATA);

//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"ZPF(",XLength,", ",EnumToString(VolumeType),", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
  }
//+------------------------------------------------------------------+ 
//| X2MA iteration function                                          | 
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
//---- checking the number of bars to be enough for calculation
   if(rates_total<min_rates_total) return(RESET);

//---- declaration of variables with a floating point  
   double price_,x1ma,x2ma,xvol,zpf;
//---- Declaration of integer variables and getting the bars already calculated
   int first,bar;
   long vol;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
      first=0; // starting number for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- Getting volume
      if(VolumeType==VOLUME_TICK) vol=tick_volume[bar];
      else vol=volume[bar];

      //---- Calling the PriceSeries function to get the input price price_
      price_=PriceSeries(IPC,bar,open,low,high,close);

      //---- three calls of the XMASeries function. 
      x1ma = XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,price_,bar,false);
      x2ma = XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,X2Length,price_,bar,false);
      xvol = XMA3.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,double(vol),bar,false);
      //---- 
      zpf=xvol*(x1ma-x2ma)/2;
      Line1Buffer[bar]=-zpf;
      Line2Buffer[bar]=+zpf;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
