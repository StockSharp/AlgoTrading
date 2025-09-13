//+------------------------------------------------------------------+
//|                                                    ColorXADX.mq5 |
//|                           Copyright © 2010,     Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2010, Nikolay Kositsin"
//---- link to the website of the author
#property link "farria@mail.redcom.ru" 
//---- indicator version number
#property version   "1.02"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- four buffers are used for the indicator calculation and drawing
#property indicator_buffers 5
//---- two plots are used
#property indicator_plots   3
//+----------------------------------------------+
//|  XDi indicator drawing parameters            |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_FILLING
//---- the following colors are used for the indicator
#property indicator_color1  Lime,Red
//---- displaying of the bullish label of the indicator
#property indicator_label1  "XDi"
//+----------------------------------------------+
//|  XADX Line indicator drawing parameters      |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- use gray color for the indicator line
#property indicator_color2  Gray
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- displaying of the bearish label of the indicator
#property indicator_label2  "XADX Line"
//+----------------------------------------------+
//|  ADX indicator drawing parameters            |
//+----------------------------------------------+
//---- drawing indicator 3 as line
#property indicator_type3   DRAW_COLOR_ARROW
//---- the following colors are used for the ADX indicator line
#property indicator_color3  Gray,Blue,Magenta,Red
//---- the indicator 3 line is a continuous curve
#property indicator_style3  STYLE_SOLID
//---- indicator 3 line width is equal to 3
#property indicator_width3  3
//---- displaying of the bearish label of the indicator
#property indicator_label3  "XADX"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_levelcolor Blue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+-----------------------------------+
//|  Averaging classes description    |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XDIP,XDIM,XADX;
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
//|  Declaration of enumerations      |
//+-----------------------------------+
enum ENUM_WIDTH //Type of constant
  {
   w_1=0,  //1
   w_2,    //2
   w_3,    //3
   w_4,    //4
   w_5     //5
  };

//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_T3; //histogram smoothing method
input int ADX_Period =14; // XMA smoothing period                 
input int ADX_Phase=100; // XMA smoothing parameter,
                      //that changes within the range -100 ... +100,
//depends of the quality of the transitional prices;

input Applied_price_ IPC=PRICE_CLOSE_;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */

input int Shift=0; // horizontal shift of the indicator in bars
input int ExtraHighLevel=60; //maximal trend level
input int HighLevel=40; //signal trend level
input int LowLevel=20;//weak trend level
input ENUM_LINE_STYLE LevelStyle=STYLE_DASHDOTDOT; //levels lines style
input color LevelColor=clrBlue; //levels color
input ENUM_WIDTH LevelWidth=w_1; //levels width
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double DiPlusBuffer[];
double DiMinusBuffer[];
double ADXBuffer[];
double ADXLineBuffer[];
double ColorADXBuffer[];
//---- Declaration of integer variables of data starting point
int min_rates_di,min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_di=XADX.GetStartBars(XMA_Method,ADX_Period,ADX_Phase);
   min_rates_total=2*min_rates_di+1;
   min_rates_di++;

//---- set DiPlusBuffer dynamic array as an indicator buffer
   SetIndexBuffer(0,DiPlusBuffer,INDICATOR_DATA);
//---- set DiMinusBuffer dynamic array as an indicator buffer
   SetIndexBuffer(1,DiMinusBuffer,INDICATOR_DATA);  
//---- set ADXLineBuffer dynamic array as an indicator buffer
   SetIndexBuffer(2,ADXLineBuffer,INDICATOR_DATA);
//---- set ADXBuffer dynamic array as an indicator buffer
   SetIndexBuffer(3,ADXBuffer,INDICATOR_DATA);
//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(4,ColorADXBuffer,INDICATOR_COLOR_INDEX);
   
//---- shifting the starting point for drawing indicator 1 by min_rates_di
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_di);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);


//---- shifting the starting point for drawing indicator 2 by min_rates_di
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_di);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);


//---- shifting the starting point for drawing indicator 3 by min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);


//---- initializations of variable for indicator short name
   string shortname;
   string Smooth=XADX.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"XADX( ",Smooth,", ",ADX_Period,", ",ADX_Phase,", ",EnumToString(IPC),", ",Shift," )");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
   
//---- indicator levels drawing parameters
   IndicatorSetInteger(INDICATOR_LEVELS,3);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,ExtraHighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,LowLevel);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,LevelColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,LevelStyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,0,LevelWidth);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,LevelColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,LevelStyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,1,LevelWidth);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,LevelColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,LevelStyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,2,LevelWidth);   
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double& high[],     // price array of maximums of price for the calculation of indicator
                const double& low[],      // price array of price lows for the indicator calculation
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- checking the number of bars to be enough for calculation
   if(rates_total<min_rates_total) return(0);

//---- declaration of local variables 
   int first,bar;
   double DiPlus,DiMinus;
   double Hi,Lo,prevHi,prevLo,prevCl,dTmpP,dTmpN,tr,dTmp;

//---- Initialization of the indicator in the OnCalculate() block
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
        first=1; // starting number for calculation of all bars
   else first=prev_calculated-1;// starting number for calculation of new bars
   
//---- main indicator calculation loop
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {

      Hi=high[bar];
      prevHi=high[bar-1];

      Lo=low[bar];
      prevLo=low[bar-1];

      prevCl=close[bar-1];

      dTmpP=Hi-prevHi;
      dTmpN=prevLo-Lo;

      if(dTmpP<0.0) dTmpP=0.0;
      if(dTmpN<0.0) dTmpN=0.0;

      if(dTmpP>dTmpN) dTmpN=0.0;
      else
        {
         if(dTmpP<dTmpN) dTmpP=0.0;
         else
           {
            dTmpP=0.0;
            dTmpN=0.0;
           }
        }

      tr=MathMax(MathMax(MathAbs(Hi-Lo),MathAbs(Hi-prevCl)),MathAbs(Lo-prevCl));
      //---
      if(tr!=0.0)
        {
         DiPlus=100.0*dTmpP/tr;
         DiMinus=100.0*dTmpN/tr;
        }
      else
        {
         DiPlus=0.0;
         DiMinus=0.0;
        }

      DiPlusBuffer [bar]=XDIP.XMASeries(1,prev_calculated,rates_total,XMA_Method,ADX_Phase,ADX_Period,DiPlus, bar,false);
      DiMinusBuffer[bar]=XDIM.XMASeries(1,prev_calculated,rates_total,XMA_Method,ADX_Phase,ADX_Period,DiMinus,bar,false);
      
      dTmp=DiPlusBuffer[bar]+DiMinusBuffer[bar];
      
      if(dTmp!=0.0) dTmp=100.0*MathAbs((DiPlusBuffer[bar]-DiMinusBuffer[bar])/dTmp);
      else          dTmp=0.0;
      
      ADXBuffer[bar]=XADX.XMASeries(min_rates_di,prev_calculated,rates_total,XMA_Method,ADX_Phase,ADX_Period,dTmp,bar,false);
      ADXLineBuffer[bar]=ADXBuffer[bar];
     }
     
//---- Main loop of the signal line coloring
   for(bar=first; bar<rates_total; bar++)
     {
      ColorADXBuffer[bar]=1;
      if(ADXBuffer[bar]>ExtraHighLevel) ColorADXBuffer[bar]=3;
      else if(ADXBuffer[bar]>HighLevel) ColorADXBuffer[bar]=2;
      else if(ADXBuffer[bar]<LowLevel)  ColorADXBuffer[bar]=0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
