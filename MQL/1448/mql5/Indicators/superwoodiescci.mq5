//+------------------------------------------------------------------+
//|                                              SuperWoodiesCCI.mq5 |
//|                                         Copyright © 2012, duckfu | 
//|                                         http://www.dopeness.org/ | 
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2012, duckfu"
//---- author of the indicator
#property link      "http://www.dopeness.org"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- four buffers are used for the indicator calculation and drawing
#property indicator_buffers 4
//---- three plots are used
#property indicator_plots   3
//+----------------------------------------------+
//|  CCI indicator drawing parameters            |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- Purple color is used as the color of the bullish line of the indicator
#property indicator_color1  clrPurple
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying of the bullish label of the indicator
#property indicator_label1  "CCI"
//+----------------------------------------------+
//|  TCCI indicator drawing parameters           |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- blue color is used for the indicator bearish line
#property indicator_color2  clrBlue
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- displaying of the bearish label of the indicator
#property indicator_label2  "TCCI"
//+----------------------------------------------+
//|  The Histogram indicator drawing parameters  |
//+----------------------------------------------+
//---- drawing the indicator as a four-color histogram
#property indicator_type3 DRAW_COLOR_HISTOGRAM
//---- colors of the four-color histogram are as follows
#property indicator_color3 clrRed,clrBlueViolet,clrLimeGreen
//---- indicator line is a solid one
#property indicator_style3 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width3 2
//---- displaying the indicator label
#property indicator_label3 "SuperWoodiesCCI"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint CCI_Period=50;
input uint TCCI_Period=10;
input ENUM_APPLIED_PRICE  applied_price=PRICE_TYPICAL;  // price type or handle
input int Shift=0;                                      // horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double CCIBuffer[];
double TCCIBuffer[];
double HistBuffer[];
double ColorHistBuffer[];
//---- Declaration of integer variables for the indicator handles
int CCI_Handle,TCCI_Handle;
//---- Declaration of integer variables of data starting point
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=int(MathMax(CCI_Period,TCCI_Period))+6;

//---- getting handle of the iCCI indicator
   CCI_Handle=iCCI(NULL,0,CCI_Period,applied_price);
   if(CCI_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iCCI indicator");

//---- getting handle of the iCCI indicator
   TCCI_Handle=iCCI(NULL,0,TCCI_Period,applied_price);
   if(TCCI_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iCCI indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,CCIBuffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(CCIBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,TCCIBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 2 by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(TCCIBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(2,HistBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- shifting the starting point for drawing indicator 2 by min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(HistBuffer,true);

//---- set dynamic array as a color buffer
   SetIndexBuffer(3,ColorHistBuffer,INDICATOR_COLOR_INDEX);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(ColorHistBuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"SuperWoodiesCCI(",
                     CCI_Period,", ",CCI_Period,", ",EnumToString(applied_price),", ",Shift,")");
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
   if(BarsCalculated(CCI_Handle)<rates_total
      || BarsCalculated(TCCI_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int to_copy,limit,bar,j,uptrending,downtrending,clr;

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit=rates_total-min_rates_total-1; // starting index for the calculation of all bars
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars

   to_copy=limit+1;
//---- copy newly appeared data into the arrays   
   if(CopyBuffer(CCI_Handle,0,0,to_copy,CCIBuffer)<=0) return(RESET);
   if(CopyBuffer(TCCI_Handle,0,0,to_copy,TCCIBuffer)<=0) return(RESET);

//---- main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      clr=1;
      HistBuffer[bar]=CCIBuffer[bar];

      uptrending=0;
      for(j=0; j<6; j++)
        {
         if(CCIBuffer[bar+j]>0) uptrending++;
         else if(CCIBuffer[bar+j]<0) uptrending=0;
        }

      if(uptrending>5) clr=2;

      downtrending=0;
      for(j=0; j<6; j++)
        {
         if(CCIBuffer[bar+j]<0) downtrending++;
         else if(CCIBuffer[bar+j]>0) downtrending=0;
        }
        
      if(downtrending>5)  clr=0;
      
      ColorHistBuffer[bar]=clr;
 
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
