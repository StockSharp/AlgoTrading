//+------------------------------------------------------------------+
//|                                                   StepSto_v1.mq5 |
//|                           Copyright © 2005, TrendLaboratory Ltd. |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                       E-mail: igorad2004@list.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, TrendLaboratory Ltd."
#property link      "http://finance.groups.yahoo.com/group/TrendLaboratory"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- two buffers are used for the indicator calculation and drawing
#property indicator_buffers 2
//---- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//|  StepSto Fast indicator drawing parameters   |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- DarkOrange color is used as the color of the bullish line of the indicator
#property indicator_color1  clrDarkOrange
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying of the bullish label of the indicator
#property indicator_label1  "StepSto Fast"
//+----------------------------------------------+
//|  StepSto Slow indicator drawing parameters   |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- MediumSlateBlue color is used for the indicator bearish line
#property indicator_color2  clrMediumSlateBlue
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- displaying of the bearish label of the indicator
#property indicator_label2  "StepSto Slow"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 50.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET  0 // The constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input double Kfast=1.0000;
input double Kslow=1.0000;
input int Shift=0;             // Horizontal shift of the indicator in bars
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double FastBuffer[];
double SlowBuffer[];
//---- Declaration of integer variables for the indicator handles
int ATR_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total,PeriodATR;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   PeriodATR=10;
   min_rates_total=MathMax(2,PeriodATR);

//---- getting the iATR indicator handle
   ATR_Handle=iATR(NULL,PERIOD_CURRENT,PeriodATR);
   if(ATR_Handle==INVALID_HANDLE) Print(" Failed to get handle of iATR indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,FastBuffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(FastBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,SlowBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 2 by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(SlowBuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"StepSto(",Kfast,",",Kslow,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
   if(BarsCalculated(ATR_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int limit,to_copy,TrendMin0,TrendMax0,TrendMid0;
   double ATR[],linemin,linemax,linemid,Sto1,Sto2,bsmin,bsmax,StepSizeMin,StepSizeMax;
   double SminMid0,SmaxMid0,SminMin0,SmaxMin0,SminMax0,SmaxMax0,ATRmax0,ATRmin0,StepSizeMid;
   static double SminMin1,SmaxMin1,SminMax1,SmaxMax1,SminMid1,SmaxMid1,ATRmax1,ATRmin1;
   static int TrendMin1,TrendMax1,TrendMid1;

//---- indexing elements in arrays as time series  
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(ATR,true);

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total-1; // starting index for the calculation of all bars
      ATRmax1=0.0;
      ATRmin1=999999999.9;
      SminMin1=close[limit];
      SmaxMin1=close[limit];
      SminMax1=close[limit];
      SmaxMax1=close[limit];
      SminMid1=close[limit];
      SmaxMid1=close[limit];
      TrendMax1=+1;
      TrendMin1=-1;
      TrendMid1=+1;
     }
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars
//----
   to_copy=limit+1;

//---- copy newly appeared data into the arrays
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);

//---- restore values of the variables
   TrendMax0=TrendMax1;
   TrendMin0=TrendMin1;
   TrendMid0=TrendMid1;

//---- main indicator calculation loop
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ATRmax0=MathMax(ATR[bar],ATRmax1);
      ATRmin0=MathMin(ATR[bar],ATRmin1);
      //----
      StepSizeMin=(Kfast*ATRmin0);
      StepSizeMax=(Kfast*ATRmax0);
      StepSizeMid=Kfast*0.5*Kslow*(ATRmax0+ATRmin0);
      //----
      SmaxMin0=close[bar]+2*StepSizeMin;
      SminMin0=close[bar]-2*StepSizeMin;

      SmaxMax0=close[bar]+2*StepSizeMax;
      SminMax0=close[bar]-2*StepSizeMax;

      SmaxMid0=close[bar]+2*StepSizeMid;
      SminMid0=close[bar]-2*StepSizeMid;
      //----
      if(close[bar]>SmaxMin1) TrendMin0=+1;
      if(close[bar]<SminMin1) TrendMin0=-1;

      if(close[bar]>SmaxMax1) TrendMax0=+1;
      if(close[bar]<SminMax1) TrendMax0=-1;

      if(close[bar]>SmaxMid1) TrendMid0=+1;
      if(close[bar]<SminMid1) TrendMid0=-1;

      if(TrendMin0>0 && SminMin0<SminMin1) SminMin0=SminMin1;
      if(TrendMin0<0 && SmaxMin0>SmaxMin1) SmaxMin0=SmaxMin1;

      if(TrendMax0>0 && SminMax0<SminMax1) SminMax0=SminMax1;
      if(TrendMax0<0 && SmaxMax0>SmaxMax1) SmaxMax0=SmaxMax1;

      if(TrendMid0>0 && SminMid0<SminMid1) SminMid0=SminMid1;
      if(TrendMid0<0 && SmaxMid0>SmaxMid1) SmaxMid0=SmaxMid1;

      if(TrendMin0>0) linemin=SminMin0+StepSizeMin;
      if(TrendMin0<0) linemin=SmaxMin0-StepSizeMin;

      if(TrendMax0>0) linemax=SminMax0+StepSizeMax;
      if(TrendMax0<0) linemax=SmaxMax0-StepSizeMax;

      if(TrendMid0>0) linemid=SminMid0+StepSizeMid;
      if(TrendMid0<0) linemid=SmaxMid0-StepSizeMid;
      //----      
      bsmin=linemax-StepSizeMax;
      bsmax=linemax+StepSizeMax;

      Sto1=(linemin-bsmin)/(bsmax-bsmin);
      Sto2=(linemid-bsmin)/(bsmax-bsmin);

      FastBuffer[bar]=Sto1*100;
      SlowBuffer[bar]=Sto2*100;

      if(bar)
        {
         ATRmax1=ATRmax0;
         ATRmin1=ATRmin0;
         SminMin1=SminMin0;
         SmaxMin1=SmaxMin0;
         SminMax1=SminMax0;
         SmaxMax1=SmaxMax0;
         SminMid1=SminMid0;
         SmaxMid1=SmaxMid0;
         TrendMax1=TrendMax0;
         TrendMin1=TrendMin0;
         TrendMid1=TrendMid0;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
