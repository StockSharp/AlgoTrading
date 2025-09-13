//+---------------------------------------------------------------------+
//|                                                  Color3rdGenXMA.mq5 | 
//|                                         Copyright © 2011, EarnForex |
//|                                           http://www.earnforex.com/ |
//+---------------------------------------------------------------------+ 
//| Place the SmoothAlgorithms.mqh file                                 |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2011, EarnForex"
#property link      "http://www.earnforex.com"
//---- indicator version number
#property version   "1.10"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Parameters of indicator drawing  |
//+-----------------------------------+
//---- drawing the indicator as a multicolored line
#property indicator_type1   DRAW_COLOR_LINE
//---- colors of the three-color line are
#property indicator_color1  clrDeepPink,clrGray,clrBlue
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1  2
//---- displaying the indicator label
#property indicator_label1  "3rdGenXMA"

//+-----------------------------------+
//|  Описание класса CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2;
//+-----------------------------------+
//|  declaration of enumerations      |
//+-----------------------------------+
enum Applied_price_ //Type of constant
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
input Smooth_Method XMA_Method=MODE_EMA; //averaging method
input uint XLength=50; //smoothing depth                    
input int XPhase=15; //smoothing parameter,
                     //for JJMA, it varies within the range -100 ... +100 and influences on the quality of the transient period;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ IPC=PRICE_TYPICAL;//price constant
input int Shift=0; // horizontal shift of the indicator in bars
input int PriceShift=0; // vertical shift of the indicator in points
//+-----------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double IndBuffer[];
double ColorIndBuffer[];

//---- Declaration of the average vertical shift value variable
double dPriceShift;
double Alpha;
//---- Declaration of integer variables of data starting point
int min_rates_total,min_rates_,SLength;
//+------------------------------------------------------------------+   
//| 3rdGenXMA indicator initialization function                      | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of data calculation starting point
   SLength=int(2*XLength);
   double Lambda=1.0*SLength/(1.0*XLength);
   Alpha=Lambda *(SLength-1)/(SLength-Lambda);

//---- Initialization of variables of data calculation starting point
   min_rates_=XMA1.GetStartBars(XMA_Method,SLength,XPhase);
   min_rates_total=min_rates_+XMA1.GetStartBars(XMA_Method,XLength,XPhase)+2;

//---- setting alerts for invalid values of external parameters
   XMA1.XMALengthCheck("XLength",XLength);
//---- setting alerts for invalid values of external parameters
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);

//---- Initialization of the vertical shift
   dPriceShift=_Point*PriceShift;

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- set dynamic array as as a color index buffer   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);

//---- shifting the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- create label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"3rdGenXMA");
//---- setting values of the indicator that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);

//---- initializations of variable for indicator short name
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"3rdGenXMA(",Smooth,", ",XLength,")");
//---- creating name for displaying if separate sub-window and in tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- determine the accuracy of displaying indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| 3rdGenXMA iteration function                                     | 
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
//---- checking for the sufficiency of the number of bars for the calculation
   if(rates_total<min_rates_total) return(0);

//---- Declaration of variables with a floating point  
   double price,x1xma,x2xma,x3rdGenXMA;
//---- Declaration of integer variables and getting the bars already calculated
   int first,bar,clr;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) //checking for the first start of calculation of an indicator
     {
      first=0; // starting number for calculation of all bars
     }
   else first=prev_calculated-1; // starting index for the calculation of new bars

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- Calling the PriceSeries function to get the input price price
      price=PriceSeries(IPC,bar,open,low,high,close);

      //---- XMASeries function two calls
      x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,SLength,price,bar,false);
      x2xma=XMA2.XMASeries(min_rates_,prev_calculated,rates_total,XMA_Method,XPhase,XLength,x1xma,bar,false);
      x3rdGenXMA=(Alpha+1)*x1xma-Alpha*x2xma;
      //----       
      IndBuffer[bar]=x3rdGenXMA+dPriceShift;
     }

//---- correction of the first variable value
   if(prev_calculated>rates_total || prev_calculated<=0) //checking for the first start of calculation of an indicator
      first=min_rates_total; // starting index for calculation of all bars

//---- Main loop of the signal line coloring
   for(bar=first; bar<rates_total; bar++)
     {
      clr=1;
      if(IndBuffer[bar-1]<IndBuffer[bar]) clr=2;
      if(IndBuffer[bar-1]>IndBuffer[bar]) clr=0;
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
