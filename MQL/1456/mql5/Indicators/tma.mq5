//+------------------------------------------------------------------+ 
//|                                                          TMA.mq5 | 
//|                                   Copyright © 2006, Matias Romeo | 
//|                                    mailto:matias.romeo@gmail.com | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright © 2006, Matias Romeo"
#property link "mailto:matias.romeo@gmail.com" 
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers
#property indicator_buffers 1 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- DodgerBlue color is used as the color of the bullish line of the indicator
#property indicator_color1 clrDodgerBlue
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "TMA"
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input uint Length=30; // smoothing depth                   
input int Shift=0; // horizontal shift of the indicator in bars
input int PriceShift=0; // vertical shift of the indicator in pointsõ
//+-----------------------------------+

//---- declaration of a dynamic array that further 
// will be used as an indicator buffer
double TMA[];

//---- Declaration of the average vertical shift value variable
double dPriceShift;
//---- Declaration of integer variables of data starting point
int min_rates_total;
int divisor;
int weights[];
//+------------------------------------------------------------------+    
//| TMA indicator initialization function                            | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=int(MathCeil(Length/2.0));
   ArrayResize(weights,min_rates_total);
   for(int iii=0; iii<min_rates_total; iii++)
     {
      int i1=iii+1;
      weights[iii]=i1;
      weights[min_rates_total-i1]=i1;
     }

   divisor=0;
   for(int kkk=0; kkk<min_rates_total; kkk++) divisor+=weights[kkk];

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,TMA,INDICATOR_DATA);
//---- shifting the indicator horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"TMA");
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"TMA(",Length,",",Shift,",",PriceShift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- Initialization of the vertical shift
   dPriceShift=_Point*PriceShift;
//----+ end of initialization
  }
//+------------------------------------------------------------------+  
//| TMA iteration function                                           | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const int begin,          // number of beginning of reliable counting of bars
                const double &price[]     // price array for calculation of the indicator
                )
  {
//---- checking the number of bars to be enough for calculation
   if(rates_total<min_rates_total+begin)return(0);

//---- Declaration of local variables
   int first,bar;
   double tma_val,tma;

   if(prev_calculated==0) // checking for the first start of the indicator calculation
     {
      first=min_rates_total+begin; // starting number for calculation of all bars
      //---- performing the shift of beginning of indicator drawing
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);
     }
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- main indicator calculation loop
   for(bar=first; bar<rates_total; bar++)
     {
      tma_val=0.0; 
      for(int nnn=0; nnn<min_rates_total; nnn++) tma_val+=price[bar-min_rates_total+nnn]*weights[nnn];
      tma=tma_val/divisor;      
      TMA[bar]=tma+dPriceShift;
     }
//----+     
   return(rates_total);
  }
//+------------------------------------------------------------------+
