//+---------------------------------------------------------------------+
//|                                                   BlauCMomentum.mq5 |
//|                                  Copyright © 2013, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| Place the SmoothAlgorithms.mqh file |
//| in the directory: terminal_data_folder\MQL5\Include        |
//+---------------------------------------------------------------------+
//---- Copyright
#property copyright "Copyright © 2013, Nikolay Kositsin"
//---- link to the website of the author
#property link "farria@mail.redcom.ru" 
#property description "Candle Momentum"
//---- Indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- three buffers are used for the indicator calculation and drawing
#property indicator_buffers 3
//---- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Indicator 1 drawing parameters            |
//+----------------------------------------------+
//---- drawing the indicator as a line
#property indicator_type1 DRAW_LINE
//---- color used for the color of the indicator line
#property indicator_color1 clrDarkViolet
//---- Indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- indicator line width is 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1  "BlauCMomentum Line"
//+----------------------------------------------+
//|  Indicator 2 drawing parameters            |
//+----------------------------------------------+
//---- drawing the indicator as five-colored labels
#property indicator_type2 DRAW_COLOR_ARROW
//---- the following colors are used
#property indicator_color2 clrDeepPink,clrOrange,clrGray,clrYellowGreen,clrTeal
//---- indicator line width is 2
#property indicator_width2 2
//---- displaying the indicator label
#property indicator_label2  "BlauCMomentum"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels |
//+----------------------------------------------+
#property indicator_level1   1
#property indicator_levelcolor clrBlue          // color of the level line
#property indicator_levelstyle STYLE_DASHDOTDOT // style of the level line
#property indicator_levelwidth 1                // width of the level line

//+----------------------------------------------+
//|  CXMA class description                           |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2,XMA3;
//+----------------------------------------------+
//|  declaration of enumerations                     |
//+----------------------------------------------+
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
//+----------------------------------------------+
//|  declaration of enumerations                     |
//+----------------------------------------------+
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
//+----------------------------------------------+
//| Indicator input parameters                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_EMA; //averaging method

input uint XLength=1;   //period of Momentum
input uint XLength1=20; //depth of the first averaging
input uint XLength2=5; //depth of the second averaging
input uint XLength3=3;  //depth of the third averaging
input int XPhase=15;    //smoothing parameter,
                        //for JJMA, it varies within the range -100 ... +100 and influences on the quality of the transient period;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ IPC1=PRICE_CLOSE;   //price constant of closing
input Applied_price_ IPC2=PRICE_OPEN;    //price constant of opening
input int Shift=0;                       // Horizontal shift of the indicator in bars 
//+----------------------------------------------+

//---- declaration of dynamic arrays that 
// will be used as indicator buffers
double LineBuffer[],IndBuffer[],ColorIndBuffer[];
//---- Declaration of integer variables of data starting point
int min_rates_total,min_rates_1,min_rates_2,min_rates_3;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_1=int(XLength-1);
   min_rates_2=min_rates_1+XMA1.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_3=min_rates_2+XMA1.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_total=min_rates_3+XMA1.GetStartBars(XMA_Method,XLength3,XPhase);   
   if(IPC1==IPC2 && XLength==1) Print("Invalid values of price constants!");

//---- setting the dynamic array LineBuffer as an indicator buffer
   SetIndexBuffer(0,LineBuffer,INDICATOR_DATA);
//---- Performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- Setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
   
//---- Set IndBuffer dynamic array as an indicator buffer
   SetIndexBuffer(1,IndBuffer,INDICATOR_DATA);
//---- Performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- Setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);

//---- Setting a dynamic array as a color index buffer   
   SetIndexBuffer(2,ColorIndBuffer,INDICATOR_COLOR_INDEX);

//--- Creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"BlauCMomentum");
//--- Determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
   double Mom,XMom,XXMom,XXXMom;
   int first,bar;

//---- Calculation of the starting number 'first' for the cycle of recalculation of bars
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
     {
      first=min_rates_1; // starting number for calculation of all bars
     }
   else first=prev_calculated-1; // Starting index for the calculation of new bars

//---- main cycle of calculation of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {

      Mom=PriceSeries(IPC1,bar,open,low,high,close)-PriceSeries(IPC2,bar-XLength+1,open,low,high,close);  

      XMom=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XPhase,XLength1,Mom,bar,false);
      XXMom=XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,XMA_Method,XPhase,XLength2,XMom,bar,false);
      XXXMom=XMA3.XMASeries(min_rates_3,prev_calculated,rates_total,XMA_Method,XPhase,XLength3,XXMom,bar,false);
      
      IndBuffer[bar]=100*XXXMom/_Point;
      LineBuffer[bar]=IndBuffer[bar];
     }

   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//---- Main loop of the Ind indicator coloring
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=2;

      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
        }

      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
        }

      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
