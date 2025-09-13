//+------------------------------------------------------------------+
//|                                     Stochastic CG Oscillator.mq5 |
//|                                                                  |
//| Stochastic CG Oscillator                                         |
//|                                                                  |
//| Algorithm taken from book                                        |
//|     "Cybernetics Analysis for Stock and Futures"                 |
//| by John F. Ehlers                                                |
//|                                                                  |
//|                                              contact@mqlsoft.com |
//|                                          http://www.mqlsoft.com/ |
//+------------------------------------------------------------------+
//--- Copyright
#property copyright "Coded by Witold Wozniak"
#property link      "www.mqlsoft.com"
//--- indicator version
#property version   "1.00"
//--- drawing the indicator in a separate window
#property indicator_separate_window
//--- two buffers are used for calculating and drawing the indicator
#property indicator_buffers 2
//--- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  Cyber Cycle indicator drawing parameters    |
//+----------------------------------------------+
//--- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//--- red color is used for the indicator line
#property indicator_color1  Red
//--- the line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//--- indicator 1 line width is equal to 1
#property indicator_width1  1
//---- displaying of the the indicator label
#property indicator_label1  "Stochastic CG Oscillator"
//+----------------------------------------------+
//|  Trigger indicator drawing parameters        |
//+----------------------------------------------+
//--- drawing indicator 2 as a line
#property indicator_type2   DRAW_LINE
//--- blue color is used for the indicator signal line
#property indicator_color2  Blue
//--- the line of the indicator 2 is a continuous curve
#property indicator_style2  STYLE_SOLID
//--- indicator 2 line width is equal to 1
#property indicator_width2  1
//--- displaying of the indicator signal line label
#property indicator_label2  "Trigger"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 +0.7
#property indicator_level2  0.0
#property indicator_level3 -0.7
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input int Length=8;  // Indicator period 
input int Shift=0;   // Horizontal shift of the indicator in bars 
//+----------------------------------------------+
//--- declaration of dynamic arrays that
//--- will be used as indicator buffers
double StocCGBuffer[];
double TriggerBuffer[];
//--- declaration of integer variables for the start of data calculation
int min_rates_total;
//--- declaration of global variables
int Count1[],Count2[];
double CG[],Value1[],Price[],StoCGShift;
//+------------------------------------------------------------------+
//|  Recalculation of position of the newest element in the array    |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos1(int &CoArr[]) // Return the current value of the price series by the link
  {
//---
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
//---
  }
//+------------------------------------------------------------------+
//|  Recalculation of position of the newest element in the array    |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos2(int &CoArr[]) // Return the current value of the price series by the link
  {
//---
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
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- initialization of variables of data calculation start
   min_rates_total=Length+3;

//--- initialization of variables  
   StoCGShift=(Length+1.0)/2.0;

//---- memory distribution for variables' arrays  
   ArrayResize(Count1,Length);
   ArrayResize(Price,Length);
   ArrayResize(CG,Length);
   ArrayResize(Count2,4);
   ArrayResize(Value1,4);

//--- set StocCGBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(0,StocCGBuffer,INDICATOR_DATA);
//---- shifting the indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 1 drawing by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//--- set the TriggerBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- performing shift of the beginning of counting of drawing the indicator 2 by min_rates_total+1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);

//--- initializations of a variable for the indicator short name
   string shortname;
   StringConcatenate(shortname,"Stochastic CG Oscillator(",Length,", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double& high[],     // price array of price maximums for the indicator calculation
                const double& low[],      // price array of minimums of price for the indicator calculation
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- checking if the number of bars is enough for the calculation
   if(rates_total<min_rates_total) return(0);

//--- declarations of local variables 
   int first,bar;
   double Num,Denom,hh,ll,tmp;

//--- calculation of the 'first' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      first=3; // starting index for calculation of all bars
      for(int numb=0; numb<Length; numb++) Count1[numb]=numb;
      for(int numb=0; numb<4; numb++) Count2[numb]=numb;

     }
   else first=prev_calculated-1; // starting number for calculation of new bars

//--- main indicator calculation loop
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      Price[Count1[0]]=(high[bar]+low[bar])/2.0;

      if(bar<Length)
        {
         Recount_ArrayZeroPos1(Count1);
         Recount_ArrayZeroPos2(Count2);
         continue;
        }

      Num=0.0;
      Denom=0.0;

      for(int iii=0; iii<Length; iii++)
        {
         Num+=(1.0+iii)*Price[Count1[iii]];
         Denom+=Price[Count1[iii]];
        }

      if(Denom!=0.0) CG[Count1[0]]=-Num/Denom+StoCGShift;
      else CG[Count1[0]]=0.0;

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

      StocCGBuffer[bar]=(4.0*Value1[Count2[0]]+3.0*Value1[Count2[1]]+2.0*Value1[Count2[2]]+Value1[Count2[3]])/10.0;
      StocCGBuffer[bar] = 2.0 * (StocCGBuffer[bar] - 0.5);
      TriggerBuffer[bar]= 0.96 *(StocCGBuffer[bar-1]+0.02);

      if(bar<rates_total-1)
        {
         Recount_ArrayZeroPos1(Count1);
         Recount_ArrayZeroPos2(Count2);
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
