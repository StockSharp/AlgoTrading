//+------------------------------------------------------------------+
//|                                                          AML.mq5 |
//|                                                         andreybs |
//|                                               andreybs@yandex.ru |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Adaptive Market Level"
//---- author of the indicator
#property link      "andreybs@yandex.ru"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- one buffer is used for calculation and drawing of the indicator
#property indicator_buffers 1
//---- one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  Cycle indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- DarkOrchid color is used as the color of the bullish line of the indicator
#property indicator_color1  DarkOrchid
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- the line width of the indicator 1 is 3
#property indicator_width1  3
//---- displaying the bullish label of the indicator
#property indicator_label1  "AML"
//+----------------------------------------------+
//| Indicator input parameters                 |
//+----------------------------------------------+
input int Fractal=6;
input int Lag=7;
input int Shift=0; //horizontal shift of the indicator in bars
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double AMLBuffer[];
//---- Declaration of integer variables of data starting point
int min_rates_total,size;
//---- declaration of dynamic arrays that will further be 
// used as ring buffers
int Count[];
double Smooth[];
//+------------------------------------------------------------------+
//|  recalculating position of the newest element in the array               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos
(
 int &CoArr[]// Return the current value of the price series by the link
 )
// Recount_ArrayZeroPos(count, Length)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max2=7;
   Max1=Max2-1;

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
//---- Initialization of variables of data starting point
   min_rates_total=Fractal+Lag;

//---- Initialization of variables
   size=Lag+1;

//---- Memory allocation for arrays of variables  
   ArrayResize(Count,size);
   ArrayResize(Smooth,size);

//---- Initialization of arrays of variables
   ArrayInitialize(Smooth,0.0);
   ArrayInitialize(Count,0.0);

//---- setting dynamic array as indicator buffer
   SetIndexBuffer(0,AMLBuffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(AMLBuffer,true);

//---- initialization of a variable for a short name of the indicator
   string shortname;
   StringConcatenate(shortname,"AML(",Fractal,", ",Lag,", ",Shift,")");
//--- creating a name to be displayed in a separate subwindow and in a tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // history in bars at the current tick
                const int prev_calculated,// history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double& high[],     // price array of price highs for the calculation of the indicator
                const double& low[],      // price array of price lows for the calculation of the indicator
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- checking for the sufficiency of the number of bars for the calculation
   if(rates_total<min_rates_total) return(0);

//---- declaring local variables 
   int MaxBar,limit,bar;

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit=rates_total-min_rates_total-1; // starting index for the calculation of all bars
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars

   MaxBar=rates_total-min_rates_total-3;

//---- indexing array elements as time series  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);

//---- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      double R1 = Range(Fractal,bar,high,low)/Fractal; 
      double R2 = Range(Fractal,bar+Fractal,high,low)/Fractal; 
      double R3 = Range(2*Fractal,bar,high,low)/(2*Fractal);
      
      double dim = 0;
      if(R1+R2>0&&R3>0) dim=(MathLog(R1+R2)-MathLog(R3))*1.44269504088896;
      
      double alpha = MathExp(-Lag*(dim-1.0));
      if(alpha > 1.0) alpha = 1.0;
      if(alpha < 0.01) alpha = 0.01;
      
      double price = (high[bar]+low[bar]+2*open[bar]+2*close[bar])/6;
      Smooth[Count[0]] = alpha * price + (1.0 - alpha) * Smooth[Count[1]];          
      
      if(MathAbs(Smooth[Count[0]]-Smooth[Count[Lag]]) >= Lag*Lag*_Point) AMLBuffer[bar] = Smooth[Count[0]];
      else AMLBuffer[bar] = AMLBuffer[bar+1];
      if(bar>0) Recount_ArrayZeroPos(Count);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
double Range(int count,int start,const double& High[],const double& Low[])
  {
//----
   double max = High[ArrayMaximum(High,start,count)];
   double min = Low[ArrayMinimum(Low,start,count)];
//----
   return(max-min);
  }
//+------------------------------------------------------------------+
