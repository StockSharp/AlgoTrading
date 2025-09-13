//+---------------------------------------------------------------------+ 
//|                                               ColorMomentum_AMA.mq5 | 
//|                                Copyright © 2010,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| For the indicator to work, place the file SmoothAlgorithms.mqh      |
//| in the directory: terminal_data_folder\MQL5\Include                 |
//+---------------------------------------------------------------------+ 
#property copyright "Copyright © 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- indicator version number
#property version   "1.10"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers
#property indicator_buffers 5 
//+-----------------------------------+
//|  Indicator drawing parameters     |
//+-----------------------------------+
//---- only two plots are used
#property indicator_plots   2
//---- drawing the indicator as a color histogram
#property indicator_type1   DRAW_COLOR_HISTOGRAM
#property indicator_type2   DRAW_COLOR_HISTOGRAM
#property indicator_color1  Blue, Green
#property indicator_color2  Magenta, Red
#property indicator_width1  2
#property indicator_width2  2
#property indicator_label1  "Momentum_AMA Upper"
#property indicator_label2  "Momentum_AMA Lower"
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
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
input int ALength=8; // period of Momentum
input int ama_period=9; // period of AMA
input int fast_ma_period=2; // period of fast MA
input int slow_ma_period=30; // period of slow MA
input Applied_price_ IPC=PRICE_CLOSE;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input double G=2.0; // a power the smoothing constant is raised to
input int Shift=0; // horizontal shift of the indicator in bars
//---+
//---- indicator buffers
double IndMomentum[];
double UpColorsBuffer[];
double LoColorsBuffer[];
double UpperBuffer[];
double LowerBuffer[];

int min_rates_total;
//+------------------------------------------------------------------+
//| The iPriceSeries function description                            |
//| Description of the function iPriceSeriesAlert                    |
//| CMomentum and CAMA classes description                           |
//+------------------------------------------------------------------+ 
#include <SmoothAlgorithms.mqh>  
//+------------------------------------------------------------------+    
//| Momentum indicator initialization function                       | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//----  
   min_rates_total=ALength+ama_period+2;
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(4,IndMomentum,INDICATOR_CALCULATIONS);
//----   
   SetIndexBuffer(0,UpperBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,UpColorsBuffer,INDICATOR_COLOR_INDEX);
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);
   SetIndexBuffer(2,LowerBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,LoColorsBuffer,INDICATOR_COLOR_INDEX);
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"Momentum_AMA( ALength = ",ALength,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- declaration of variable of the class CJurX from the file SmoothAlgorithms.mqh
   CMomentum Mom;
//---- setting alerts for invalid values of external parameters
   Mom.MALengthCheck("Length", ALength);
   Mom.MALengthCheck("ama_period", ama_period);
   Mom.MALengthCheck("fast_ma_period", fast_ma_period);
   Mom.MALengthCheck("slow_ma_period", slow_ma_period);
//---- end of initialization
  }
//+------------------------------------------------------------------+  
//| Momentum iteration function                                      | 
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
   double price,momentum,amomentum;
//---- Declaration of integer variables and getting the bars already calculated
   int first,bar;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated==0) // checking for the first start of the indicator calculation
      first=0; // starting number for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- declaration of variables of the classes CMomentum and CAMA from the file SmoothAlgorithms.mqh
   static CMomentum Mom;
   static CAMA AMA;

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total; bar++)
     {
      //---- Call of the PriceSeries function to get incrementation of the input price dprice_
      price=PriceSeries(IPC,bar,open,low,high,close);

      //---- Indicator calculation  
      momentum=Mom.MomentumSeries(0,prev_calculated,rates_total,ALength,price,bar,false);
      amomentum=AMA.AMASeries(ALength+1,prev_calculated,rates_total,ama_period,fast_ma_period,slow_ma_period,G,momentum,bar,false);

      //---- Loading the obtained value in the indicator buffer
      amomentum/=_Point;
      IndMomentum[bar]=amomentum;
      //----
      UpperBuffer[bar]=0.0;
      UpColorsBuffer[bar]=0.0;
      LowerBuffer[bar]=0.0;
      LoColorsBuffer[bar]=0.0;
      //----
      if(bar<=min_rates_total) continue;

      if(amomentum>0)
        {
         UpperBuffer[bar]=amomentum;
         if(amomentum>IndMomentum[bar-1]) UpColorsBuffer[bar]=1;
        }
      else
        {
         LowerBuffer[bar]=amomentum;
         if(amomentum<IndMomentum[bar-1]) LoColorsBuffer[bar]=1;
        }

     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+ 
