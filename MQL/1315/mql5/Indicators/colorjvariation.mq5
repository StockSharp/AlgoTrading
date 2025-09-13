/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+
//|                                              ColorJVariation.mq5 | 
//|                                         Copyright © 2010, LeMan. |
//|                                                 b-market@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, LeMan."
#property link      "b-market@mail.ru"
//---- indicator version number
#property version   "1.10"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing indicator as a three-colored line
#property indicator_type1 DRAW_COLOR_LINE
//---- colors of the three-color line are
#property indicator_color1 Gray,Lime,Red
//---- the indicator line is a continuous curve
#property indicator_style1 STYLE_SOLID
//---- indicator line width is equal to 4
#property indicator_width1  2
//---- displaying the indicator label
#property indicator_label1  "JVariation"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor Blue
#property indicator_levelstyle STYLE_SOLID
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input int Period_=12; //period of averaging
input ENUM_MA_METHOD MA_Method_=MODE_SMA; //method of averaging  
input int JLength_=3; // depth of JMA smoothing                   
input int JPhase_=100; // parameter of the JMA smoothing,
                      //that changes within the range -100 ... +100,
//depends of the quality of the transitional prices;      
input int Shift=0; // horizontal shift of the indicator in bars
//+-----------------------------------+
//---- indicator buffer
double LineBuffer[],ColorLineBuffer[];

//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
// Moving_Average class description                                  | 
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+   
//| Variation indicator initialization function                      | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
  min_rates_total=int(3*Period_+30+1);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,LineBuffer,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"JVariation");
//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorLineBuffer,INDICATOR_COLOR_INDEX);
   
//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"JVariation( Period_ = ",Period_,", MA_Method_ = ",MA_Method_,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| Variation iteration function                                     | 
//+------------------------------------------------------------------+ 
int OnCalculate
(
 const int rates_total,// number of bars in history at the current tick
 const int prev_calculated,// amount of history in bars at the previous tick
 const int begin,// number of beginning of reliable counting of bars
 const double &price[]// price array for calculation of the indicator
 )
  {
//---- Checking if the number of bars is sufficient for the calculation
   if(rates_total<min_rates_total+begin) return(0);

//---- Declaration of integer variables
   int first1,first2=0,bar;
//---- declaration of variables with a floating point  
   double ma1,ma2,ma3,jma3;
//---- Declaration of static variables
   static int start1,start2,start3,start4;

//---- Initialization of the indicator in the OnCalculate() block
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      first1=begin; // starting number for calculation of all bars
      
      //---- Initialization of variables of the start of data calculation
      start1=begin;
      
      if(MA_Method_!=MODE_EMA)
        {
         start2 = start1 + Period_;
         start3 = start2 + Period_;
         start4 = start3 + Period_;
         first2 = start4 + 30 + 2;
        }
      else
        {
         start2 = start1 + 30;
         start3 = start2 + 30;
         start4 = start3 + 30;
         first2 = start4 + 30 + 2;
        }
        
      //--- increase the position of the data start by 'begin' bars as a result of the calculation using data of another indicator
      if(begin>0) PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,first2);
     }
   else
     {
      first1=prev_calculated-1; // starting index for calculation of new bars
      first2=first1;
     }

//---- declaration of the Moving_Average class variables from the SmoothAlgorithms.mqh file
   static CMoving_Average MA1,MA2,MA3;
//---- declaration of the JJMA class variables from the SmoothAlgorithms.mqh file
   static CJJMA JMA;

//---- Main calculation loop of the indicator
   for(bar=first1; bar<rates_total; bar++)
     {
      //---- Three call of the function MASeries.  
      ma1 = MA1.MASeries(start1, prev_calculated, rates_total, Period_, MA_Method_, price[bar], bar, false);
      ma2 = MA2.MASeries(start2, prev_calculated, rates_total, Period_, MA_Method_, price[bar]-ma1, bar, false);
      ma3 = MA3.MASeries(start3, prev_calculated, rates_total, Period_, MA_Method_, price[bar]-ma2-ma1, bar, false);
      
      //---- One call of the JJMASeries function. 
      //The parameters Phase and Length don't change at every bar (Din = 0) 
      jma3=JMA.JJMASeries(start4,prev_calculated,rates_total,0,JPhase_,JLength_,ma3,bar,false);
      //----       
      LineBuffer[bar]=jma3;
     }
     
//---- Main indicator line coloring loop
   for(bar=first2; bar<rates_total; bar++)
     {
      ColorLineBuffer[bar]=0;
      if(LineBuffer[bar]>LineBuffer[bar-1]) ColorLineBuffer[bar]=1;
      if(LineBuffer[bar]<LineBuffer[bar-1]) ColorLineBuffer[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
