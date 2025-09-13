//+------------------------------------------------------------------+
//|                                         Fisher CG Oscillator.mq5 |
//|                                                                  |
//| Fisher Stochastic CG Oscillator                                  |
//|                                                                  |
//| Algorithm taken from book                                        |
//|     "Cybernetics Analysis for Stock and Futures"                 |
//| by John F. Ehlers                                                |
//|                                                                  |
//|                                              contact@mqlsoft.com |
//|                                          http://www.mqlsoft.com/ |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Coded by Witold Wozniak"
//---- author of the indicator
#property link      "www.mqlsoft.com"
//---- indicator version
#property version   "1.00"
//---- drawing the indicator in a separate window
#property indicator_separate_window
//---- two buffers are used for calculation and drawing the indicator
#property indicator_buffers 2
//---- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Fisher CG indicator drawing parameters      |
//+----------------------------------------------+
//---- drawing the indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- red color is used as the color of the indicator line
#property indicator_color1  Red
//---- the indicator 1 line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- indicator 1 line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator line label
#property indicator_label1  "Fisher CG Oscillator"
//+----------------------------------------------+
//|  Trigger indicator drawing parameters        |
//+----------------------------------------------+
//---- dawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- blue color is used for the indicator line
#property indicator_color2  Blue
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- thickness of the indicator 2 line is equal to 1
#property indicator_width2  1
//---- displaying the indicator line label
#property indicator_label2  "Trigger"
//+----------------------------------------------+
//| Horizontal levels display parameters         |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input int Length=10;  // Indicator period 
input int Shift=0;    // Horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that
//---- will be used as indicator buffers
double FCGBuffer[];
double TriggerBuffer[];
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//---- declaration of global variables
int Count1[],Count2[];
double CG[],Value1[],CGshift;
//+------------------------------------------------------------------+
//|  Recalculation of position of the newest element in the array    |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos1(int &CoArr[]) // Return the current value of the price series by the link
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max1=Length-1;
   Max2=Length;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+
//|  Recalculation of position of the newest element in the array    |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos2(int &CoArr[]) // Return the current value of the price series by the link
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max1=3;
   Max2=4;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- initialization of variables of the start of data calculation
   min_rates_total=Length;
//---- initialization of variables  
   CGshift=(Length+1.0)/2.0;
//---- memory distribution for variables' arrays  
   ArrayResize(Count1,Length);
   ArrayResize(CG,Length);
   ArrayResize(Count2,4);
   ArrayResize(Value1,4);
   
   ArrayInitialize(Count1,0);
   ArrayInitialize(CG,0.0);
   ArrayInitialize(Count2,0);
   ArrayInitialize(Value1,0.0);

//---- set FCGBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(0,FCGBuffer,INDICATOR_DATA);
//---- shifting the indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing shift of the beginning of counting of drawing the indicator 1 by 3
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,3);

//---- set TriggerBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing shift of the beginning of counting of drawing the indicator 2 by 3
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,3);

//---- initializations of variable for the indicator short name
   string shortname;
   StringConcatenate(shortname,"Fisher CG Oscillator(",Length,", ",Shift,")");
//---- creation of the name to be displayed in a separate sub-window and in a tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// number of bars calculated at previous call
                const datetime &time[],
                const double &open[],
                const double& high[],     // price array of maximums of price for the indicator calculation
                const double& low[],      // price array of minimums of price for the indicator calculation
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- checking the number of bars to be enough for the calculation
   if(rates_total<min_rates_total) return(0);

//---- declarations of local variables 
   int first,bar;
   double price,Num,Denom,hh,ll,tmp,Value2;

//---- calculation of the 'first' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
     {
      first=min_rates_total;     // starting index for calculation of all bars
     }
   else first=prev_calculated-1; // starting index for calculation of new bars

//---- main indicator calculation loop
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      Num=0.0;
      Denom=0.0;

      for(int count=0; count<Length; count++)
        {
         price=(high[bar-count]+low[bar-count])/2.0;
         Num += (1.0 + count) * price;
         Denom+=price;
        }
      if(Denom!=0.0) CG[Count1[0]]=-Num/Denom+CGshift;
      else           CG[Count1[0]]=0.0;

      hh = CG[Count1[0]];
      ll = CG[Count1[0]];

      for(int iii=0; iii<Length; iii++)
        {
         tmp= CG[Count1[iii]];
         hh = MathMax(hh, tmp);
         ll = MathMin(ll, tmp);
        }

      if(hh!=ll) Value1[Count2[0]]=(CG[Count1[0]]-ll)/(hh-ll);
      else Value1[Count2[0]]=0.0;

      Value2=(4.0*Value1[Count2[0]]+3.0*Value1[Count2[1]]+2.0*Value1[Count2[2]]+Value1[Count2[3]])/10.0;
      FCGBuffer[bar]=0.5*MathLog((1.0+1.98 *(Value2-0.5))/(1.0-1.98 *(Value2-0.5)));
      TriggerBuffer[bar]=FCGBuffer[bar-1];

      if(bar<rates_total-1)
        {
         Recount_ArrayZeroPos1(Count1);
         Recount_ArrayZeroPos2(Count2);
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
