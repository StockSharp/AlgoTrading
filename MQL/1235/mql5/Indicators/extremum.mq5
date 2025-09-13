//+------------------------------------------------------------------+
//|                                                     Extremum.mq5 |
//|                                           Copyright © 2010, Egor | 
//|                                                                  | 
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2010, Egor"
//---- link to the website of the author
#property link ""
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- six buffers are used for calculation of drawing of the indicator
#property indicator_buffers 6
//---- six graphical plots are used
#property indicator_plots   6
//+----------------------------------------------+
//|  Indicator 1 drawing parameters              |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- blue color is used for the indicator line
#property indicator_color1  clrBlue
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "Extremum Line"
//+----------------------------------------------+
//|  Indicator 2 drawing parameters              |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- Teal color is used for indicator line
#property indicator_color2  clrTeal
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 1
#property indicator_width2  1
//---- displaying of the bearish label of the indicator
#property indicator_label2  "Upper Line"
//+----------------------------------------------+
//|  Indicator 3 drawing parameters              |
//+----------------------------------------------+
//---- drawing indicator 3 as line
#property indicator_type3   DRAW_LINE
//---- Magenta color is used for indicator line
#property indicator_color3  clrMagenta
//---- the indicator 3 line is a continuous curve
#property indicator_style3  STYLE_SOLID
//---- thickness of the indicator 3 line is equal to 1
#property indicator_width3  1
//---- displaying of the bearish label of the indicator
#property indicator_label3  "Lower Line"
//+----------------------------------------------+
//|  Indicator 4 drawing parameters              |
//+----------------------------------------------+
//---- drawing the indicator 4 as a histogram
#property indicator_type4   DRAW_HISTOGRAM
//---- red color is used as the color of the indicator line
#property indicator_color4  clrRed
//---- indicator 4 line width is equal to 2
#property indicator_width4  2
//---- displaying of the bearish label of the indicator
#property indicator_label4  "Lower Histogram"
//+----------------------------------------------+
//|  Indicator 5 drawing parameters              |
//+----------------------------------------------+
//---- drawing the indicator 5 as a histogram
#property indicator_type5   DRAW_HISTOGRAM
//---- blue color is used for the indicator line
#property indicator_color5  clrBlue
//---- indicator 5 line width is equal to 2
#property indicator_width5  2
//---- displaying of the bearish label of the indicator
#property indicator_label5  "Signal Histogram"
//+----------------------------------------------+
//|  Indicator 6 drawing parameters              |
//+----------------------------------------------+
//---- drawing the indicator 6 as a histogram
#property indicator_type6   DRAW_HISTOGRAM
//---- light green color is used for indicator line
#property indicator_color6  clrLime
//---- indicator 6 line width is equal to 2
#property indicator_width6  2
//---- displaying of the bearish label of the indicator
#property indicator_label6  "Upper Histogram"
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint NBars=20; // indicator period 
input int Shift = 0; // horizontal shift of the indicator in bars 
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double  LineBuffer[];
double  DnBuffer[];
double  UpBuffer[];
double  DnhBuffer[];
double  FlhBuffer[];
double  UphBuffer[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,LineBuffer,INDICATOR_DATA);
//---- shifting indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing 
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,NBars);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 2 drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,NBars);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(2,UpBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 2 drawing
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,NBars);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(3,DnhBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 2 drawing
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,NBars);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(4,FlhBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(4,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 2 drawing
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,NBars);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(5,UphBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(5,PLOT_SHIFT,Shift);
//---- shifting the starting point of the indicator 2 drawing
   PlotIndexSetInteger(5,PLOT_DRAW_BEGIN,NBars);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"Extremum(",NBars,", ",Shift,")");
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
                const datetime &Time[],
                const double &Open[],
                const double& High[],     // price array of maximums of price for the calculation of indicator
                const double& Low[],      // price array of minimums of price for the calculation of indicator
                const double &Close[],
                const long &Tick_Volume[],
                const long &Volume[],
                const int &Spread[]
                )
  {
//---- checking the number of bars to be enough for calculation
   if(rates_total<int(NBars)) return(0);

//---- declaration of local variables 
   int first,bar,i;
   double n,m,sum,dif;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
      first=int(NBars-1); // starting number for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- main indicator calculation loop
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      FlhBuffer[bar]=0.0;
      DnhBuffer[bar]=0.0;
      UphBuffer[bar]=0.0;

      n=0;
      m=0;
      for(i=0;i<int(NBars); i++) if(High[bar]>High[bar-i] && High[bar]-High[bar-i]>n) n=High[bar]-High[bar-i];
      for(i=0;i<int(NBars); i++) if(Low[bar]<Low[bar-i] && Low[bar]-Low[bar-i]<m) m=High[bar]-Low[bar-i];

      sum=n+m;
      dif=m-n;

      LineBuffer[bar]=sum;
      UpBuffer[bar]=+dif;
      DnBuffer[bar]=-dif;

      if(sum==+dif || sum==-dif) FlhBuffer[bar]=-sum/2;

      if(sum<0) DnhBuffer[bar]=sum;
      if(sum>0) UphBuffer[bar]=sum;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
