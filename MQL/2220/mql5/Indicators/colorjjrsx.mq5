//+---------------------------------------------------------------------+
//|                                                      ColorJJRSX.mq5 | 
//|                                Copyright © 2013,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Place the SmoothAlgorithms.mqh file                                 |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2013, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Relative Strength Index"
//--- indicator version
#property version   "1.00"
//--- drawing the indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers
#property indicator_buffers 3 
//--- 2 plots are used
#property indicator_plots   2
//+-----------------------------------+
//|  Indicator 1 drawing parameters   |
//+-----------------------------------+
//--- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//--- color is used for the color of the indicator line
#property indicator_color1 clrGray
//--- Indicator line is a solid one
#property indicator_style1  STYLE_SOLID
//--- indicator line width is 1
#property indicator_width1  1
//---- displaying of the the indicator label
#property indicator_label1  "JJRSX"
//+-----------------------------------+
//|  Indicator 2 drawing parameters   |
//+-----------------------------------+
//---- drawing the indicator as a color histogram
#property indicator_type2   DRAW_COLOR_HISTOGRAM
//--- the following colors are used in the histogram
#property indicator_color2 clrRed,clrDarkOrange,clrGray,clrTeal,clrLime
//--- the indicator line is a continuous curve
#property indicator_style2  STYLE_SOLID
//--- indicator line width is 3
#property indicator_width2  3
//---- displaying of the the indicator label
#property indicator_label2  "High Short;Short;Flat;Long;High Long"
//--- description of averaging classes and indicators
#include <SmoothAlgorithms.mqh>
//+------------------------------------------+
//--- declaration of the CJMA and CJurX classes variables from the SmoothAlgorithms.mqh file
CJJMA JMA;
CJurX UpJurX,DnJurX;
//+------------------------------------------+
//| declaration of enumerations              |
//+------------------------------------------+
enum Applied_price_ //Type of constant
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
//+------------------------------------------+
//| declaration of enumerations              |
//+------------------------------------------+
enum IndStyle // indicator display style
  {
   COLOR_LINE = DRAW_COLOR_LINE,          //Colored line
   COLOR_HISTOGRAM=DRAW_COLOR_HISTOGRAM,  //Colored histogram
   COLOR_ARROW=DRAW_COLOR_ARROW           //Colored labels
  };
//+------------------------------------------+
//| Indicator input parameters               |
//+------------------------------------------+
input uint JurXPeriod=8;              // JurX period
input uint JMAPeriod=3;               // JMA period
input int JMAPhase=100;               // JMA averaging parameter,
//--- JMAPhase: -100...+100,  influences the quality of the transition process;
input Applied_price_ IPC=PRICE_CLOSE; // Applied price
input int Shift=0;                    // Horizontal shift of the indicator in bars
input IndStyle Style=COLOR_HISTOGRAM; // JJRSX display style
input int HighLevel=+20;              // Upper trigger level
input int MiddleLevel=0;              // The middle of the range
input int LowLevel=-20;               // Lower trigger level
//+------------------------------------------+
//--- declaration of dynamic arrays that
//--- will be used as indicator buffers
double JJRSX[];
double JJRSX_[];
double ColorJJRSX[];
//--- declaration of integer variables of data starting point
int min_rates_total,min_rates_;
//+------------------------------------------------------------------+   
//| JJRSX indicator initialization function                          | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//--- initialization of variables of the start of data calculation
   min_rates_=2;
   min_rates_total=min_rates_+30;
//--- set dynamic array as an indicator buffer
   SetIndexBuffer(0,JJRSX_,INDICATOR_DATA);
//--- set dynamic array as an indicator buffer
   SetIndexBuffer(1,JJRSX,INDICATOR_DATA);
//--- Setting a dynamic array as a color index buffer   
   SetIndexBuffer(2,ColorJJRSX,INDICATOR_COLOR_INDEX);
//--- shifting the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- shift the beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- shifting the indicator 1 horizontally
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- shift the beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- change the indicator style   
   PlotIndexSetInteger(1,PLOT_DRAW_TYPE,Style);
//--- initializations of a variable for the indicator short name
   string shortname;
   StringConcatenate(shortname,"Relative Strenght Index(",
                     string(JurXPeriod),",",string(JMAPeriod),",",string(JMAPhase),",",EnumToString(IPC),",",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- the number of the indicator 3 horizontal levels   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//--- values of the indicator horizontal levels   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,MiddleLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
//--- gray and magenta colors are used for horizontal levels lines  
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrPurple);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrPurple);
//--- Short dot-dash is used for the horizontal level line  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//--- initialization end
  }
//+------------------------------------------------------------------+ 
//| JJRSX iteration function                                         | 
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
   double dPrice,AbsdPrice,jrsx,uprsx,dnrsx;
//--- declaration of integer variables and getting already calculated bars
   int first,clr;
//--- calculation of the starting number 'first' for the cycle of recalculation of bars
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
     {
      first=1; // starting number for calculation of all bars
     }
   else
     {
      first=prev_calculated-1; //Starting number for calculation of new bars
     }
//--- main calculation loop of the indicator
   for(int bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //--- call of the PriceSeries function to get the input price 'price_'
      dPrice=PriceSeries(IPC,bar,open,low,high,close)-PriceSeries(IPC,bar-1,open,low,high,close);
      AbsdPrice=MathAbs(dPrice);
      //--- prohibition for zero divide!
      if(!AbsdPrice) AbsdPrice=_Point;
      //--- preliminary initialization of the indicator buffer using an empty value
      JJRSX[bar]=EMPTY_VALUE;
      //--- two calls of the JurXSeries function. 
      uprsx=UpJurX.JurXSeries(1,prev_calculated,rates_total,0,JurXPeriod,dPrice,bar,false);
      dnrsx=DnJurX.JurXSeries(1,prev_calculated,rates_total,0,JurXPeriod,AbsdPrice,bar,false);
      jrsx=100*uprsx/dnrsx;
      //--- one call of the JMASeries function 
      JJRSX[bar]=JMA.JJMASeries(min_rates_,prev_calculated,rates_total,0,JMAPhase,JMAPeriod,jrsx,bar,false);
      JJRSX_[bar]=JJRSX[bar];
      //---
      clr=2;
      if(JJRSX[bar]>JJRSX[bar-1]) {if(JJRSX[bar]>HighLevel) clr=4; else clr=3;}
      if(JJRSX[bar]<JJRSX[bar-1]) {if(JJRSX[bar]<LowLevel)  clr=0; else clr=1;}
      ColorJJRSX[bar]=clr;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
