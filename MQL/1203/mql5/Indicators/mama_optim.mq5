//+------------------------------------------------------------------+
//|                                                   MAMA_Optim.mq5 |
//|              MQL5 Code:     Copyright © 2010,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- indicator version number
#property version   "1.10"
//---- drawing the indicator in the main window
#property indicator_chart_window
//----two buffers are used for calculation of drawing of the indicator
#property indicator_buffers 2
//---- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  FAMA indicator drawing parameters           |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- green color is used as the color of the bullish line of the indicator
#property indicator_color1  Lime
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying of the bullish label of the indicator
#property indicator_label1  "MAMA"
//+----------------------------------------------+
//|  MAMA indicator drawing parameters           |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- red color is used as the color of the bearish indicator line
#property indicator_color2  Red
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- displaying of the bearish label of the indicator
#property indicator_label2  "FAMA"
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input double FastLimit = 0.5;
input double SlowLimit = 0.05;
//+----------------------------------------------+
//---- declaration of dynamic arrays that further 
//---- will be used as indicator buffers
double MAMABuffer[];
double FAMABuffer[];
//---- declaration of the integer variables for the start of data calculation
int StartBar;
//+------------------------------------------------------------------+
//| CountVelue() function                                            |
//+------------------------------------------------------------------+
double CountVelue(const int &CoArr[],double  &Array1[],double  &Array2[])
  {
//----
   double Resalt=
                 (0.0962*Array1[CoArr[0]]
                 +0.5769*Array1[CoArr[2]]
                 -0.5769*Array1[CoArr[4]]
                 -0.0962*Array1[CoArr[6]])
                 *(0.075*Array2[CoArr[1]]+0.54);
//----
   return(Resalt);
  }
//+------------------------------------------------------------------+
//| SmoothVelue() function                                           |
//+------------------------------------------------------------------+
double SmoothVelue(const int &CoArr[],double &Array[])
  {
//----
   return(0.2*Array[CoArr[0]]
          +0.8*Array[CoArr[1]]);
  }
//+------------------------------------------------------------------+
//|  Recalculation of position of a newest element in the array      |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos
(
 int &CoArr[]// Return the current value of the price series by the link
 )
// Recount_ArrayZeroPos(count, Length)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----
   int numb;
   static int count=1;
   count--;
   if(count<0) count=6;

   for(int iii=0; iii<7; iii++)
     {
      numb=iii+count;
      if(numb>6) numb-=7;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- initialization of constants
   StartBar=7+1;
//---- set MAMABuffer dynamic array as indicator buffer
   SetIndexBuffer(0,MAMABuffer,INDICATOR_DATA);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"MAMA");
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBar);

//---- transformation of the FAMABuffer dynamic array into an indicator buffer
   SetIndexBuffer(1,FAMABuffer,INDICATOR_DATA);
//--- create a label to display in DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"FAMA");
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBar+1);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"The MESA Adaptive Moving Average(",FastLimit,", ",SlowLimit,")");
//---- creating name for displaying in a separate sub-window and in tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- set accuracy of displaying of the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const int begin,          // number of beginning of reliable counting of bars
                const double &price[]     // price array for calculation of the indicator
                )
  {
//---- checking the number of bars to be enough for calculation
   if(rates_total<StartBar+begin) return(0);

//---- memory variables introduction  
   static double smooth[7],detrender[7],Q1[7],I1[7],I2[7];
   static double Q2[7],jI[7],jQ[7],Re[7],Im[7],period[7],Phase[7];
   static int Count[7];

//---- declaration of local variables 
   int first,bar,Bar0,Bar1,Bar3;
   double DeltaPhase,alpha;

//---- calculation of the starting number 'first' for the cycle of recalculation of bars
   if(prev_calculated==0) // checking for the first start of the indicator calculation
     {
      first=StartBar+begin; // starting number for calculation of all bars
      //----
      ArrayInitialize(smooth,0.0);
      ArrayInitialize(detrender,0.0);
      ArrayInitialize(period,0.0);
      ArrayInitialize(Phase,0.0);
      ArrayInitialize(Q1,0.0);
      ArrayInitialize(I1,0.0);
      ArrayInitialize(I2,0.0);
      ArrayInitialize(Q2,0.0);
      ArrayInitialize(jI,0.0);
      ArrayInitialize(jQ,0.0);
      ArrayInitialize(Re,0.0);
      ArrayInitialize(Im,0.0);
      //----
      MAMABuffer[first-1] = price[first-1];
      FAMABuffer[first-1] = price[first-1];

     }
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- main cycle of calculation of the indicator
   for(bar=first; bar<rates_total; bar++)
     {
      Bar0=Count[0];
      Bar1=Count[1];
      Bar3=Count[3];

      smooth[Bar0]=(4*price[bar-0]+3*price[bar-1]+2*price[bar-2]+1*price[bar-3])/10.0;
      //----
      detrender[Bar0]=CountVelue(Count,smooth,period);
      Q1[Bar0]=CountVelue(Count,detrender,period);
      I1[Bar0] = detrender[Bar3];
      jI[Bar0] = CountVelue(Count, I1, I1);
      jQ[Bar0] = CountVelue(Count, Q1, Q1);
      I2[Bar0] = I1[Bar0] - jQ[Bar0];
      Q2[Bar0] = Q1[Bar0] + jI[Bar0];
      I2[Bar0] = SmoothVelue(Count,I2);
      Q2[Bar0] = SmoothVelue(Count,Q2);
      Re[Bar0] = I2[Bar0]*I2[Bar1] + Q2[Bar0]*Q2[Bar1];
      Im[Bar0] = I2[Bar0]*Q2[Bar1] - Q2[Bar0]*I2[Bar1];
      Re[Bar0] = SmoothVelue(Count,Re);
      Im[Bar0] = SmoothVelue(Count,Im);
      //----
      if(Im[Bar0] && Re[Bar0])
        {
         double res=MathArctan(Im[Bar0]/Re[Bar0]);
         if(res) period[Bar0]=6.285714/res;
         else period[Bar0]=6.285714;
        }
      else period[Bar0]=6.285714;

      if(period[Bar0]>1.50*period[Bar1]) period[Bar0]=1.50*period[Bar1];
      if(period[Bar0]<0.67*period[Bar1]) period[Bar0]=0.67*period[Bar1];
      if(period[Bar0]<6.00) period[Bar0]=6.00;
      if(period[Bar0]>50.0) period[Bar0]=50.0;
      //----
      period[Bar0]=0.2*period[Bar0]+0.8*period[Bar1];
      //----
      if(I1[Bar0]) Phase[Bar0]=57.27272987*MathArctan(Q1[Bar0]/I1[Bar0]);
      else Phase[Bar0]=57.27272987;
      //----
      DeltaPhase=Phase[Bar1]-Phase[Bar0];
      if(DeltaPhase<1) DeltaPhase=1.0;
      //----
      alpha=FastLimit/DeltaPhase;
      if(alpha<SlowLimit)alpha=SlowLimit;
      //----
      MAMABuffer[bar]=alpha*price[bar]+(1.0-alpha)*MAMABuffer[bar-1];
      FAMABuffer[bar]=0.5*alpha*MAMABuffer[bar]+(1.0-0.5*alpha)*FAMABuffer[bar-1];
      //----
      if(bar<rates_total-1)
        {
         Recount_ArrayZeroPos(Count);
        }
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
