//+------------------------------------------------------------------+
//|                                                       AFIRMA.mq5 |
//|                                                                  |
//|                                         Copyright © 2006, gpwr.  |
//+------------------------------------------------------------------+
//--- author of the indicator
#property copyright "Copyright © 2006, gpwr."
#property link      ""
//--- indicator version
#property version   "1.01"
//--- drawing the indicator in the main window
#property indicator_chart_window 
//--- two buffers are used for calculation and drawing the indicator
#property indicator_buffers 2
//--- only two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//| Declaration of constants                     |
//+----------------------------------------------+
#define RESET 0
#define pi 3.141592653589793238462643383279502884197169399375105820974944592
//+----------------------------------------------+
//| Indicator 1 drawing parameters               |
//+----------------------------------------------+
//--- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//--- use blue violet color for the indicator 1 line
#property indicator_color1  BlueViolet
//--- line of the indicator 1 is a continuous line
#property indicator_style1  STYLE_SOLID
//--- indicator 1 line width is equal to 2
#property indicator_width1  2
//--- displaying the indicator line label
#property indicator_label1  "FIRMA"
//+----------------------------------------------+
//| Indicator 2 drawing parameters               |
//+----------------------------------------------+
//--- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//--- red color is used for the indicator 2 line
#property indicator_color2  Red
//--- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//--- indicator 2 line width is equal to 2
#property indicator_width2  2
//--- displaying the indicator line label
#property indicator_label2  "ARMA"
//+----------------------------------------------+
//| Declaration of enumerations                  |
//+----------------------------------------------+
enum ENUM_WINDOWS   // Type of constant
  {
   Rectangular = 1, // Rectangular window
   Hanning1,        // Hanning window 1
   Hanning2,        // Hanning window 2
   Blackman,        // Blackman window
   Blackman_Harris  // Blackman-Harris window
  };
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input int  Periods = 4;               // LF transmission width 1/(2*Periods)
input int  Taps    = 21;              // Number of delay units in the filter
input ENUM_WINDOWS Window=Blackman;   // Window index
input int Shift=0;                    // Horizontal shift of the indicator in bars 
//+----------------------------------------------+
//--- declaration of the integer variables for the start of data calculation
int min_rates_total;
//--- declaration of global variables
int n;
double w[],wsum,sx2,sx3,sx4,sx5,sx6,den;
//--- declaration of dynamic arrays which will be used as indicator buffers
double FIRMABuffer[],ARMABuffer[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- initialization of variables  
   ArrayResize(w,Taps);
   wsum=0.0;
   for(int k=0; k<Taps; k++)
     {
      switch(Window)
        {
         case Rectangular: w[k]=1.0; break;                                                       // Rectangular window       
         case Hanning1: w[k]=0.50-0.50*MathCos(2.0*pi*k/Taps); break;                             // Hanning window
         case Hanning2: w[k]=0.54-0.46*MathCos(2.0*pi*k/Taps); break;                             // Hamming window
         case Blackman: w[k]=0.42-0.50*MathCos(2.0*pi*k/Taps)+0.08*MathCos(4.0*pi*k/Taps); break; // Blackman window
         case Blackman_Harris: w[k]=0.35875-0.48829*MathCos(2.0*pi*k/Taps)+0.14128
            *MathCos(4.0*pi*k/Taps)-0.01168*MathCos(6.0*pi*k/Taps);                               // Blackman - Harris window
        }
      if(k!=Taps/2.0) w[k]=w[k]*MathSin(pi*(k-Taps/2.0)/Periods)/pi/(k-Taps/2.0);
      wsum+=w[k];
     }
//--- calculate sums for the least-squares method
   n=(Taps-1)/2;
   sx2 = (2*n + 1) / 3.0;
   sx3 = n*(n + 1) / 2.0;
   sx4 = sx2*(3*n*n+3*n - 1) / 5.0;
   sx5 = sx3*(2*n*n+2*n - 1) / 3.0;
   sx6 = sx2*(3*n*n*n*(n + 2) - 3*n+1) / 7.0;
   den = sx6*sx4 / sx5 - sx5;
//--- initialization of variables of the start of data calculation
   min_rates_total=Taps;
//--- set FIRMABuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(0,FIRMABuffer,INDICATOR_DATA);
//--- shifting the indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- performing shift of the beginning of counting of drawing the indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- indexing the elements in the buffer as timeseries
   ArraySetAsSeries(FIRMABuffer,true);
//--- set ARMABuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(1,ARMABuffer,INDICATOR_DATA);
//--- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- performing shift of the beginning of counting of drawing the indicator 2 by min_rates_total+1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- indexing the elements in the buffer as timeseries
   ArraySetAsSeries(ARMABuffer,true);
//--- initializations of a variable for the indicator short name
   string shortname;
   StringConcatenate(shortname,"AFIRMA(",Periods,", ",Taps,", ",EnumToString(Window),", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// number of bars calculated at previous call
                const int begin,          // bars reliable counting beginning index
                const double &price[])    // price array for the indicator calculation
  {
//--- checking the number of bars to be enough for the calculation
   if(rates_total<min_rates_total+begin) return(RESET);
//--- declarations of local variables 
   int limit,bar;
   double a0,a1,a2,a3,sx2y,sx3y,p,q;
//--- calculation of the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-1-min_rates_total-begin; // starting index for calculation of all bars
      //--- performing the shift of the beginning of the indicators drawing
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);
      PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,rates_total-n-1);
     }
   else limit=rates_total-prev_calculated; // starting index for calculation of new bars
//--- indexing elements in arrays as timeseries  
   ArraySetAsSeries(price,true);
//--- main cycle of calculation of FIRMA indicator
   for(bar=limit; bar>=0; bar--)
     {
      FIRMABuffer[bar+n]=0.0;
      for(int k=0; k<Taps; k++) FIRMABuffer[bar+n]+=price[bar+k]*w[k]/wsum;
     }
//---  initialize indicator buffers
   for(bar=limit; bar>=0; bar--)
     {
      if(bar<n) FIRMABuffer[bar]=EMPTY_VALUE;
      ARMABuffer[bar]=EMPTY_VALUE;
     }
//--- calculate regressive MA for the remaining n bars
   a0 = FIRMABuffer[n];
   a1 = FIRMABuffer[n]-FIRMABuffer[n+1];
   sx2y = 0.0;
   sx3y = 0.0;
//---
   for(int i=0; i<=n; i++)
     {
      sx2y += i*i*price[n-i];
      sx3y += i*i*i*price[n-i];
     }
//---
   sx2y = 2.0*sx2y / n / (n + 1);
   sx3y = 2.0*sx3y / n / (n + 1);
   p = sx2y - a0*sx2 - a1*sx3;
   q = sx3y - a0*sx3 - a1*sx4;
   a2 = (p*sx6 / sx5 - q) / den;
   a3 = (q*sx4 / sx5 - p) / den;
//---
   for(int k=0; k<=n; k++) ARMABuffer[n-k]=a0+k*a1+k*k*a2+k*k*k*a3;
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
