//+---------------------------------------------------------------------+
//|                                                    VininI_Trend.mq5 | 
//|                                   Copyright © 2008, Victor Nicolaev | 
//|                                                       vinin@mail.ru | 
//+---------------------------------------------------------------------+ 
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2008, Victor Nicolaev"
#property link      "vinin@mail.ru"
#property description "VininI_Trend"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- drawing the indicator as a multy-color histogram
#property indicator_type1   DRAW_COLOR_HISTOGRAM
//---- the following colors are used in the histogram
#property indicator_color1  clrRed,clrMaroon,clrGray,clrTeal,clrLimeGreen
//---- Indicator line width is equal to 2
#property indicator_width1  2
//---- displaying the indicator label
#property indicator_label1  "VininI_Trend"
//+-----------------------------------+
//| Indicator window parameters       |
//+-----------------------------------+
//#property indicator_minimum -130
//#property indicator_maximum +130

//+-----------------------------------+
//|  Averaging classes description    |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA classes variables from the SmoothAlgorithms.mqh file
CXMA XMA1[],XMA2;
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
enum Applied_price_ //Type od constant
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
/*enum Smooth_Method - enumeration is declared in SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input Smooth_Method MA_Method1=MODE_SMA; //smoothing method of moving averages
input uint Length1=3; //start smoothing depth of moving averages                  
input int Phase1=15; //moving averages smoothing parameter,
                     // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period

input uint MA_Step=10;  //step of depth changing of the moving averages smoothing
input uint MA_Count=10;  //number of smoothing steps

input Smooth_Method MA_Method2=MODE_JJMA; //indicator smoothing method
input uint Length2=20; //indicator smoothing depth
input int Phase2=100;  //indicator smoothing parameter,
                       // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period

input Applied_price_ IPC=PRICE_CLOSE;//price constant
input int UpLevel=+10; //Up level (range 0/+100)
input int DnLevel=-10; //Down level (range -100/0)
input int Shift=0; // horizontal shift of the indicator in bars
//+-----------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double IndBuffer[],ColorIndBuffer[];

//---- Declaration of integer variables of data starting point
int min_rates_total,min_rates_,nsize,period[];
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation   
   min_rates_=XMA2.GetStartBars(MA_Method1,Length1+MA_Step*(MA_Count-1),Phase1);
   min_rates_total=min_rates_+XMA2.GetStartBars(MA_Method2,Length2,Phase2);

//---- setting alerts for invalid values of external parameters
   XMA2.XMALengthCheck("Length1", Length1);
   XMA2.XMALengthCheck("Length2", Length2);
//---- setting alerts for invalid values of external parameters
   XMA2.XMAPhaseCheck("Phase1", Phase1, MA_Method1);
   XMA2.XMAPhaseCheck("Phase2", Phase2, MA_Method2);

//---- memory allocation for arrays of variables
   nsize=int(MA_Count);
   if(ArrayResize(XMA1,nsize)<nsize) Print("Failed to distribute the memory for XMA1[] array");
   if(ArrayResize(period,nsize)<nsize) Print("Failed to distribute the memory for period[] array");

//---- Initialization of variables
   for(int sm=0; sm<nsize && !IsStopped(); sm++) period[sm]=int(Length1+sm*MA_Step);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);

//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"VininI_Trend");

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);

//---- the number of the indicator 3 horizontal levels   
   IndicatorSetInteger(INDICATOR_LEVELS,3);
//---- values of the indicator horizontal levels   
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,UpLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,0);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,DnLevel);
//---- the following colors are used for horizontal levels lines
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrMagenta);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrBlue);
//---- short dot-dash is used for the horizontal level line  
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- checking the number of bars to be enough for calculation
   if(rates_total<min_rates_total) return(0);

//---- declaration of variables with a floating point  
   double price_,xma,res,xres;
//---- Declaration of integer variables and getting the bars already calculated
   int first,bar,clr,sum;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
      first=0; // starting number for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price_=PriceSeries(IPC,bar,open,low,high,close);
      sum=0;

      for(int sm=0; sm<nsize; sm++)
        {
         xma=XMA1[sm].XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,period[sm],price_,bar,false);
         if(close[bar]>xma) sum++;
         else sum--;
        }

      res=100*sum/nsize;
      xres=XMA2.XMASeries(min_rates_,prev_calculated,rates_total,MA_Method2,Phase2,Length2,res,bar,false);
      IndBuffer[bar]=xres;

      clr=2;
      //----      
      if(xres>UpLevel) clr=4; else if(xres>0) clr=3;
      if(xres<DnLevel) clr=0; else if(xres<0) clr=1;
      //----
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
