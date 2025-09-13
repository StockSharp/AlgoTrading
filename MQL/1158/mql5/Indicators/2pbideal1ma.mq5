//+------------------------------------------------------------------+
//|                                                  2pbIdeal1MA.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2011, Nikolay Kositsin"
//---- link to the website of the author
#property link "farria@mail.redcom.ru"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window
//---- one buffer is used for calculation and drawing of the indicator
#property indicator_buffers 1
//---- only one plot is used
#property indicator_plots   1
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- blue color is used for the indicator line
#property indicator_color1  Blue
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1  2
//---- displaying the indicator label
#property indicator_label1  "2pbIdealMA"

//---- indicator input parameters
input int Period1 = 10; //rough smoothing
input int Period2 = 10; //adjusting smoothing
input int MAShift=0; //horizontal shift of MA in bars 

//---- declaration of a dynamic array that further 
// will be used as an indicator buffer
double ExtLineBuffer[];
//---- declarations of variables for smoothing constants
double w1,w2;
//+------------------------------------------------------------------+
//|  Smoothing from Neutron                                          |
//+------------------------------------------------------------------+
double GetIdealMASmooth
(
 double W1_,//first smoothing constant
 double W2_,//second smoothing constant
 double Series1,//the value of the time series from the current bar 
 double Series0,//the value of the time series from the previous bar 
 double Resalt1 //the value of the moving from the previous bar
 )
  {
//----
   double Resalt0,dSeries,dSeries2;
   dSeries=Series0-Series1;
   dSeries2=dSeries*dSeries-1.0;

   Resalt0=(W1_ *(Series0-Resalt1)+
            Resalt1+W2_*Resalt1*dSeries2)
   /(1.0+W2_*dSeries2);
//----
   return(Resalt0);
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- initializations of variables
   w1=1.0/Period1;
   w2=1.0/Period2;
//---- set ExtLineBuffer as indicator buffer
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- horizontal shift of the indicator by MAShift
   PlotIndexSetInteger(0,PLOT_SHIFT,MAShift);
//---- set the position, from which the indicator drawing starts
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,1);
//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"2pbIdealMA(",w1,",",w2,")");
//--- create label to display in Data Window
   PlotIndexSetString(0,PLOT_LABEL,shortname);
//---- creating name for displaying in a separate sub-window and in tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- set accuracy of displaying of the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- restriction to draw empty values for the indicator
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,     // number of bars in history at the current tick
                const int prev_calculated, // number of bars in history at the previous tick
                const int begin,           // number of beginning of reliable counting of bars
                const double &price[]      // price array for calculation of the indicator
                )
  {
//---- checking the number of bars to be enough for calculation
   if(rates_total<1+begin) return(0);

//---- declaration of local variables 
   int first,bar;
   
//---- calculation of the starting number 'first' for the cycle of recalculation of bars
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      first=1+begin;  // starting number for calculation of all bars
      //---- increase the position of the beginning of data by 'begin' bars as a result of calculation using data of another indicator
      if(begin>0)
         PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,begin+1);
         
      ExtLineBuffer[begin]=price[begin];
     }
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- main cycle of calculation of the indicator
   for(bar=first; bar<rates_total; bar++)
      ExtLineBuffer[bar]=GetIdealMASmooth(w1,w2,price[bar-1],price[bar],ExtLineBuffer[bar-1]);
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
