//+---------------------------------------------------------------------+
//|                                           ColorSchaffTrendCycle.mq5 |
//|                                     Copyright © 2011, EarnForex.com |
//|                                           http://www.earnforex.com/ |
//+---------------------------------------------------------------------+ 
//| Place the SmoothAlgorithms.mqh file                                 |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2011, EarnForex.com"
#property link      "http://www.earnforex.com"
#property description "Schaff Trend Cycle - Cyclical Stoch over Stoch over XMACD."
#property description "The code adapted Nikolay Kositsin."
//---- indicator version number
#property version   "2.10"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1

//+-----------------------------------+
//|  Parameters of indicator drawing  |
//+-----------------------------------+
//---- drawing the indicator as a color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- the following colors are used in the histogram
#property indicator_color1 clrMagenta,clrMediumOrchid,clrDarkOrange,clrPeru,clrBlue,clrDodgerBlue,clrMediumSeaGreen,clrLime
//---- indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "Schaff Trend Cycle"

//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
//---- setting lower and upper borders of the indicator window
#property indicator_minimum -110
#property indicator_maximum +110

//+-----------------------------------+
//|  Averaging classes description    |
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
//|  Input parameters of the indicator|
//+-----------------------------------+
input Smooth_Method XMA_Method=MODE_EMA; //Histogram smoothing method
input int Fast_XMA = 23; //Fast moving average period
input int Slow_XMA = 50; //Slow moving average period
input int XPhase= 100;  //moving averages smoothing parameter,
                       //hat changes within the range -100 ...  +100 for JJMA, impacts the transitional process quality;
// For VIDIA, it is the CMO period, for AMA, it is the period of slow moving average
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Cycle=10; //Stochastic oscillator period
input int HighLevel=+60;
input int MiddleLevel=0;
input int LowLevel=-60;
//+-----------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double STC_Buffer[];
double ColorSTC_Buffer[];
//----
int Count[];
bool st1_pass,st2_pass;
double XMACD[],ST[],Factor;
//---- Declaration of integer variables of data starting point
int min_rates_total,min_rates_1,min_rates_2;
//+------------------------------------------------------------------+
//|  Recalculation of position of a newest element in the array      |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos
(
 int &CoArr[],// Return the current value of the price series by the link
 int Rates_total,
 int Bar
 )
// Recount_ArrayZeroPos(Count,rates_total,bar);
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----
   if(Bar>=Rates_total-1) return;

   int numb;
   static int count=1;
   count--;

   if(count<0) count=Cycle-1;

   for(int iii=0; iii<Cycle; iii++)
     {
      numb=iii+count;
      if(numb>Cycle-1) numb-=Cycle;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_1=MathMax(XMA1.GetStartBars(XMA_Method,Fast_XMA,XPhase),
                       XMA1.GetStartBars(XMA_Method,Slow_XMA,XPhase));
   min_rates_2=min_rates_1+Cycle;
   min_rates_total=min_rates_2+Cycle+1;

//---- memory distribution for variables' arrays
   if(ArrayResize(ST,Cycle)<Cycle) Print("Failed to distribute the memory for ST array");
   if(ArrayResize(XMACD,Cycle)<Cycle) Print("Failed to distribute the memory for XMACD array");
   if(ArrayResize(Count,Cycle)<Cycle) Print("Failed to distribute the memory for Count array");

//---- initialization of constants  
   Factor=0.5;
   st1_pass = false;
   st2_pass = false;

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,STC_Buffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- set dynamic array as a index buffer
   SetIndexBuffer(1,ColorSTC_Buffer,INDICATOR_COLOR_INDEX);

//---- initializations of variable for indicator short name
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"Schaff Trend Cycle( ",
                     Smooth,", ",Fast_XMA,", ",Slow_XMA,", ",Cycle," )");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);

//---- setting up alerts for unacceptable values of external parameters
   XMA1.XMALengthCheck("Fast_XMA", Fast_XMA);
   XMA1.XMALengthCheck("Slow_XMA", Slow_XMA);
//---- setting up alerts for unacceptable values of external parameters
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);

//---- the number of the indicator 3 horizontal levels   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- values of the indicator horizontal levels   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,MiddleLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//---- gray and magenta colors are used for horizontal levels lines  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrMediumSeaGreen);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrMagenta);
//---- short dot-dash is used for the horizontal level line  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//---- end of initialization
  }
//+------------------------------------------------------------------+
//| Schaff Trend Cycle                                               |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- 
   if(rates_total<=min_rates_total) return(0);

//---- declaration of variables with a floating point  
   double price_,fastxma,slowxma,LLV,HHV;
//---- Declaration of integer variables
   int first,bar,Bar0,Bar1;

//---- calculation of the starting number 'first' for the cycle of recalculation of bars
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
      first=0; // starting number for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      Bar0=Count[0];
      Bar1=Count[1];
      price_=PriceSeries(AppliedPrice,bar,open,low,high,close);;

      fastxma = XMA1.XMASeries(0, prev_calculated, rates_total, XMA_Method, XPhase, Fast_XMA, price_, bar, false);
      slowxma = XMA2.XMASeries(0, prev_calculated, rates_total, XMA_Method, XPhase, Slow_XMA, price_, bar, false);

      XMACD[Bar0]=fastxma-slowxma;

      if(bar<=min_rates_2)
        {
         Recount_ArrayZeroPos(Count,rates_total,bar);
         continue;
        }

      LLV=XMACD[ArrayMinimum(XMACD)];
      HHV=XMACD[ArrayMaximum(XMACD)];

      //---- first stochastic calculation
      if(HHV-LLV!=0) ST[Bar0]=((XMACD[Bar0]-LLV)/(HHV-LLV))*100;
      else           ST[Bar0]=ST[Bar1];

      if(st1_pass) ST[Bar0]=Factor *(ST[Bar0]-ST[Bar1])+ST[Bar1];
      st1_pass=true;

      if(bar<=min_rates_2)
        {
         Recount_ArrayZeroPos(Count,rates_total,bar);
         continue;
        }

      LLV=ST[ArrayMinimum(ST)];
      HHV=ST[ArrayMaximum(ST)];

      //---- second stochastic calculation
      if(HHV-LLV!=0) STC_Buffer[bar]=((ST[Bar0]-LLV)/(HHV-LLV))*200-100;
      else           STC_Buffer[bar]=STC_Buffer[bar-1];

      //---- second stochastic smoothing
      if(st2_pass) STC_Buffer[bar]=Factor *(STC_Buffer[bar]-STC_Buffer[bar-1])+STC_Buffer[bar-1];
      st2_pass=true;

      //---- recalculation of the elements position in ring buffers during a bar change
      Recount_ArrayZeroPos(Count,rates_total,bar);
     }

   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//---- Main cycle of the indicator coloring
   for(bar=first; bar<rates_total; bar++)
     {
      double Sts=STC_Buffer[bar];
      double dSts=Sts-STC_Buffer[bar-1];
      int clr=4;
      //----
      if(Sts>0)
        {
         if(Sts>HighLevel)
           {
            if(dSts>=0) clr=7;
            else clr=6;
           }
         else
           {
            if(dSts>=0) clr=5;
            else clr=4;
           }
        }
      //----  
      if(Sts<0)
        {
         if(Sts<LowLevel)
           {
            if(dSts<0) clr=0;
            else clr=1;
           }
         else
           {
            if(dSts<0) clr=2;
            else clr=3;
           }
        }
      //----  
      ColorSTC_Buffer[bar]=clr;
     }
//----
   return(rates_total);
  }
//+----------------------------------------------------
