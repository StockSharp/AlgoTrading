//+------------------------------------------------------------------+
//|                                                          CMO.mq5 | 
//|                           Copyright © 2006, TrendLaboratory Ltd. |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                       E-mail: igorad2004@list.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, TrendLaboratory Ltd."
#property link      "http://finance.groups.yahoo.com/group/TrendLaboratory"
#property description "CMO"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  Declaration of constants                    |
//+----------------------------------------------+
#define RESET  0 // the constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//|  CMO indicator drawing parameters            |
//+----------------------------------------------+
//---- drawing the indicator 1 as a cloud
#property indicator_type1   DRAW_FILLING
//---- the following colors are used for the indicator
#property indicator_color1  LightSeaGreen,DarkOrange
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "CMO"
//+----------------------------------------------+
//| parameters of the indicator horizontal levels|
//+----------------------------------------------+
#property indicator_level1  +50
#property indicator_level2    0
#property indicator_level3  -50
#property indicator_levelcolor SlateGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input uint Length=14;                        // Indicator period
input ENUM_MA_METHOD Method=MODE_SMA;        // Smoothing type
input ENUM_APPLIED_PRICE Price=PRICE_CLOSE;  // Price
input int Shift=0;                           // Horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that
// will be used as indicator buffers
double Line1Buffer[];
double Line2Buffer[];
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//---- declaration of integer variables for the indicators handles
int MA_Handle;
//---- declaration of dynamic arrays that
//---- will be used as ring buffers
int Count[];
double Bulls[],Bears[];
//+------------------------------------------------------------------+
//|  Recalculation of position of a newest element in the array      |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CountArray[],// return the current value of the price series by the link
                          int Size)
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max2=Size;
   Max1=Max2-1;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CountArray[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- initialization of variables of the start of data calculation
   min_rates_total=int(Length);

//---- getting handle of the iMA indicator
   MA_Handle=iMA(NULL,0,Length,0,Method,Price);
   if(MA_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iMA indicator");

//---- memory distribution for variables' arrays
   int size=int(Length);
   if(ArrayResize(Count,size)<size) Print("Failed to distribute the memory for Count[] array");
   if(ArrayResize(Bulls,size)<size) Print("Failed to distribute the memory for Bulls[] array");
   if(ArrayResize(Bears,size)<size) Print("Failed to distribute the memory for Bears[] array");

   ArrayInitialize(Count,0);
   ArrayInitialize(Bulls,0);
   ArrayInitialize(Bears,0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,Line1Buffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing shift of the beginning of counting of drawing the indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(Line1Buffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,Line2Buffer,INDICATOR_DATA);
//---- shifting the indicator 3 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing shift of the beginning of counting of drawing the indicator 2 by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing the elements in buffers as timeseries   
   ArraySetAsSeries(Line2Buffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"CMO(",Length,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//----
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
//---- checking the number of bars to be enough for the calculation
   if(BarsCalculated(MA_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int limit,to_copy,bar;
   double MA[],dPrice,Sum;

//---- calculation of the 'first' starting number for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of the indicator calculation
     {
      limit=rates_total-2-min_rates_total; // starting index for calculation of all bars
     }
   else limit=rates_total-prev_calculated; // starting index for calculation of new bars
   to_copy=limit+2;

//---- copy newly appeared data into the arrays
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA)<=0) return(RESET);

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(MA,true);

//---- main loop of the indicator calculation
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      dPrice=MA[bar]-MA[bar+1];
      Bulls[Count[0]]=0.5*(MathAbs(dPrice)+dPrice);
      Bears[Count[0]]=0.5*(MathAbs(dPrice)-dPrice);

      double SumBulls=0,SumBears=0;
      for(int iii=0; iii<int(Length); iii++)
        {
         SumBulls+=Bulls[Count[iii]];
         SumBears+=Bears[Count[iii]];
        }

      Sum=SumBulls+SumBears;
      if(Sum) Line1Buffer[bar]=(SumBulls-SumBears)/(SumBulls+SumBears)*100;
      else Line1Buffer[bar]=0.0;
      Line2Buffer[bar]=0.0;
      if(bar) Recount_ArrayZeroPos(Count,Length);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
