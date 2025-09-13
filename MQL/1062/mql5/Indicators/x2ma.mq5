//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2011, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+
/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+
//|                                                         X2MA.mq5 | 
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers
#property indicator_buffers 1 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Parameters of indicator drawing  |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- blue-violet color is used as the color of the indicator line
#property indicator_color1 BlueViolet
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- Indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "X2MA"

//+-----------------------------------+
//|  CXMA class description           |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2;
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
input Smooth_Method MA_Method1=MODE_SMA; //Method of averaging of the first smoothing 
input int Length1=12; //First smoothing depth                    
input int Phase1=15; //First smoothing parameter,
  // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
  // for VIDIA it is a CMO period, for AMA it is a slow average period
input Smooth_Method MA_Method2=MODE_JJMA; //method of averaging of the second smoothing 
input int Length2 = 5; //Second smoothing depth 
input int Phase2=15;  //Second smoothing parameter,
  // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
  // for VIDIA it is a CMO period, for AMA it is a slow average period
input Applied_price_ IPC=PRICE_CLOSE;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // horizontal shift of the indicator in bars
input int PriceShift=0; // vertical shift of the indicator in points
//+-----------------------------------+

//---- declaration of a dynamic array that further 
// will be used as an indicator buffer
double X2MA[];

//---- Declaration of the average vertical shift value variable
double dPriceShift;
//---- declaration of the integer variables for the start of data calculation
int StartBars,StartBars1,StartBars2;
//+------------------------------------------------------------------+   
//| X2MA indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   StartBars1=XMA1.GetStartBars(MA_Method1, Length1, Phase1);
   StartBars2=XMA2.GetStartBars(MA_Method2, Length2, Phase2);
   StartBars=StartBars1+StartBars2;
//---- setting up alerts for unacceptable values of external parameters
   XMA1.XMALengthCheck("Length1", Length1);
   XMA2.XMALengthCheck("Length2", Length2);
//---- setting up alerts for unacceptable values of external parameters
   XMA1.XMAPhaseCheck("Phase1", Phase1, MA_Method1);
   XMA2.XMAPhaseCheck("Phase2", Phase2, MA_Method2);
   
//---- initialization of the vertical shift
   dPriceShift=_Point*PriceShift;
   
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,X2MA,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"X2MA");
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
   
//---- initializations of variable for indicator short name
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(MA_Method1);
   string Smooth2=XMA1.GetString_MA_Method(MA_Method2);
   StringConcatenate(shortname,"X2MA(",Length1,", ",Length2,", ",Smooth1,", ",Smooth2,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
   
//---- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
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
   if(rates_total<StartBars) return(0);

//---- declaration of variables with a floating point  
   double price_,x1xma,x2xma;
//---- Declaration of integer variables and getting already calculated bars
   int first,bar;

//---- calculation of the starting number 'first' for the cycle of recalculation of bars
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
      first=0; // starting number for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- Main cycle of calculation of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- call of the PriceSeries function to get the input price 'price_'
      price_=PriceSeries(IPC,bar,open,low,high,close);

      //---- Two calls of the XMASeries function. 
      //---- The 'begin' parameter is increased by StartBars1 in the second call, as it is another XMA smoothing  
      x1xma = XMA1.XMASeries( 0, prev_calculated, rates_total, MA_Method1, Phase1, Length1, price_, bar, false);
      x2xma = XMA2.XMASeries(StartBars1, prev_calculated, rates_total, MA_Method2, Phase2, Length2,  x1xma, bar, false);
      //----       
      X2MA[bar]=x2xma+dPriceShift;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
