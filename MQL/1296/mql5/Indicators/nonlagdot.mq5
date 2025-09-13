//+------------------------------------------------------------------+ 
//|                                                    NonLagDot.mq5 | 
//|                                Copyright © 2006, TrendLaboratory |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                   E-mail: igorad2003@yahoo.co.uk |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, TrendLaboratory"
#property link      "http://finance.groups.yahoo.com/group/TrendLaboratory"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//---- number of indicator buffers 2
#property indicator_buffers 2
//+-----------------------------------+
//|  Declaration of constants         |
//+-----------------------------------+
#define RESET  0 // the constant for getting the command for the indicator recalculation back to the terminal
#define PI     3.1415926535 // Pi character
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- only one plot is used
#property indicator_plots   1
//---- drawing the indicator as colored labels
#property indicator_type1   DRAW_COLOR_ARROW
#property indicator_color1  Gray,Magenta,Green
#property indicator_width1  2
#property indicator_label1  "NonLagDot"
//+-----------------------------------+
//|  Indicator input parameters       |
//+-----------------------------------+
input ENUM_APPLIED_PRICE Price=PRICE_CLOSE;       // Ценовая константа
input ENUM_MA_METHOD     Type=MODE_SMA;           // Метод усреднения
input int                Length=10;               // Период расчета индикатора
input int                Filter= 0;
input double             Deviation=0;             // Девиация
input int                Shift=0;                 // Сдвиг индикатора по горизонтали в барах
//+-----------------------------------+
//---- declaration of dynamic arrays that
//---- will be used as indicator buffers
double MABuffer[];
double ColorMABuffer[];
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//---- declaration of integer variables for the indicators handles
int MA_Handle;
//---- declaration of global variables
int Phase;
double Coeff,Len,Cycle,dT1,dT2,Kd,Fi;
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- initialization of constants
   Coeff= 3*PI;
   Phase=Length-1;
   Cycle= 4;
   Len=Length*Cycle + Phase;
   dT1=(2*Cycle-1)/(Cycle*Length-1);
   dT2=1.0/(Phase-1);
   Kd=1.0+Deviation/100;
   Fi=Filter*_Point;

//---- initialization of variables of the start of data calculation 
   min_rates_total=int(Length+Len+1);

//---- getting the iMA indicator handle
   MA_Handle=iMA(NULL,0,Length,0,Type,Price);
   if(MA_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iMA indicator");

//---- set MABuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(0,MABuffer,INDICATOR_CALCULATIONS);
//---- horizontal shift of the indicator
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(MABuffer,true);

//---- set ColorMABuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(1,ColorMABuffer,INDICATOR_COLOR_INDEX);
//---- horizontal shift of the indicator  
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing   
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(ColorMABuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"NonLagDot( Length = ",Length,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- end of initialization
  }
//+------------------------------------------------------------------+  
//| Custom iteration function                                        | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // number of bars in history at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- checking for the sufficiency of bars for the calculation
   if(BarsCalculated(MA_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- declaration of local variables 
   int to_copy,limit,bar,trend0;
   double MA[],alfa,beta,t,Sum,Weight,g;
   static int trend1;

//---- calculations of the necessary amount of data to be copied
//---- and the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      to_copy=rates_total;                 // calculated number of all bars
      limit=rates_total-min_rates_total-1; // starting index for calculation of all bars
     }
   else
     {
      to_copy=rates_total-prev_calculated+int(Len); // calculated number of new bars only
      limit=rates_total-prev_calculated;            // starting index for calculation of new bars
     }

//--- copy newly appeared data in the array
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(MA,true);

   trend0=trend1;

//---- main loop of the indicator calculation
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Weight=0;
      Sum=0;
      t=0;

      for(int iii=0; iii<=Len-1; iii++)
        {
         g=1.0/(Coeff*t+1);
         if(t<=0.5) g=1;
         beta=MathCos(PI*t);
         alfa=g*beta;
         Sum+=alfa*MA[bar+iii];
         Weight+=alfa;
         if(t<1) t+=dT2;
         else if(t<Len-1) t+=dT1;
        }

      if(Weight>0) MABuffer[bar]=Kd*Sum/Weight;

      if(Filter>0) if(MathAbs(MABuffer[bar]-MABuffer[bar-1])<Fi) MABuffer[bar]=MABuffer[bar-1];

      if(MABuffer[bar]-MABuffer[bar+1]>Fi) trend0=+1;
      if(MABuffer[bar+1]-MABuffer[bar]>Fi) trend0=-1;

      ColorMABuffer[bar]=0;

      if(trend0>0) ColorMABuffer[bar]=2;
      if(trend0<0) ColorMABuffer[bar]=1;
      if(bar) trend1=trend0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+ 
