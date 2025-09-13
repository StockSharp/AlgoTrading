//+------------------------------------------------------------------+
//|                                                    UltraFatl.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//| Place the SmoothAlgorithms.mqh file                              |
//| to the directory: terminal_data_folder\MQL5\Include              |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2011, Nikolay Kositsin"
//---- link to the website of the author
#property link "farria@mail.redcom.ru"
//---- indicator version number
#property version   "1.01"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- three buffers are used for calculation and drawing the indicator
#property indicator_buffers 3
//---- one plot is used
#property indicator_plots   1
//+-----------------------------------+ 
//|  Declaration of constants         |
//+-----------------------------------+ 
#define RESET 0 // The constant for getting the command for the indicator recalculation back to the terminal
//+-----------------------------------+
//|  Filling drawing parameters       |
//+-----------------------------------+
//---- drawing indicator as a filling between two lines
#property indicator_type1   DRAW_COLOR_HISTOGRAM2
//---- the following colors are used for the indicator
#property indicator_color1  Gray,Magenta,HotPink,Red,Brown,LimeGreen,Teal,Lime,PaleGreen
//---- indicator line width is equal to 4
#property indicator_width1 4
//---- displaying the indicator label
#property indicator_label1 "Ultra Fatl"
//+-----------------------------------+
//|  CXMA class description           |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA and CFATL classes from the SmoothAlgorithms.mqh file
CXMA XMA[];
CFATL Fatl;
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
/*enum Smooth_Method - enumeration is declared in SmoothAlgorithms.mqh
  {
   MODE_SMA_,  // SMA
   MODE_EMA_,  // EMA
   MODE_SMMA_, // SMMA
   MODE_LWMA_, // LWMA
   MODE_JJMA,  // JJMA
   MODE_JurX,  // JurX
   MODE_ParMA, // ParMA
   MODE_T3,    // T3
   MODE_VIDYA, // VIDYA
   MODE_AMA,   // AMA
  }; */
enum Applied_price_      // Type of constant
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPLE,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_   // TrendFollow_2 Price 
  };
//+-----------------------------------+
//|  enumeration declaration          |
//+-----------------------------------+  
enum WIDTH
  {
   Width_1=1, // 1
   Width_2,   // 2
   Width_3,   // 3
   Width_4,   // 4
   Width_5    // 5
  };
//+-----------------------------------+
//|  enumeration declaration          |
//+-----------------------------------+
enum STYLE
  {
   SOLID_,       // Solid line
   DASH_,        // Dashed line
   DOT_,         // Dotted line
   DASHDOT_,     // Dot-dash line
   DASHDOTDOT_   // Dot-dash line with double dots
  };
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input ENUM_APPLIED_PRICE Applied_price=PRICE_CLOSE; // used price
//----
input Smooth_Method W_Method=MODE_JJMA;             // Smoothing method
input int StartLength=3;                            // Initial smoothing period                    
input int WPhase=100;                               // Smoothing parameter
//----  
input uint Step=5;                                  // Period change step
input uint StepsTotal=10;                           // Number of period changes
//----
input Smooth_Method SmoothMethod=MODE_JJMA;         // Smoothing method
input int SmoothLength=3;                           // Smoothing depth
input int SmoothPhase=100;                          // Smoothing parameter
input Applied_price_ IPC=PRICE_CLOSE_;              //price constant
//----                          
input uint UpLevel=80;                              // Overbought level, %%
input uint DnLevel=20;                              // Oversold level, %%
input color UpLevelsColor=Blue;                     // Overbought level color
input color DnLevelsColor=Blue;                     // Oversold level color
input STYLE Levelstyle=DASH_;                       // Levels style
input WIDTH  LevelsWidth=Width_1;                   // Levels width                       
//+----------------------------------------------+
//---- declaration of dynamic arrays that
//---- will be used as indicator buffers
double BullsBuffer[];
double BearsBuffer[];
double ColorBuffer[];
//---- declaration of the variables array for storing WPR indicator signal lines periods
int period[];
//---- declaration of variables arrays for the RSI indicator values storage
double invelue0[],invelue1[];
//----
double dUpLevel,dDnLevel;
//---- declaration of the integer variables for the start of data calculation
uint StTot1,StTot2;
int min_rates_total,min_rates_fatl,min_rates_xma;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- memory allocation for arrays of variables
   int size=int(StepsTotal+3);
   if(ArrayResize(XMA,size)<size) Print("Failed to distribute the memory for XMA[] array");
   size-=2;
   if(ArrayResize(invelue0,size)<size) Print("Failed to distribute the memory for invelue0[] array");
   if(ArrayResize(invelue1,size)<size) Print("Failed to distribute the memory for invelue1[] array");
   if(ArrayResize(period,size)<size) Print("Failed to distribute the memory for period[] array");

   ZeroMemory(invelue0);
   ZeroMemory(invelue1);
   ZeroMemory(period);

//---- initialization of variables
   for(int sm=int(StepsTotal); sm>=0 && !IsStopped(); sm--) period[sm]=int(StartLength+sm*Step);
   dUpLevel=StepsTotal*UpLevel/100.0;
   dDnLevel=StepsTotal*DnLevel/100.0;

//---- setting alerts for invalid values of external parameters
   XMA[0].XMALengthCheck("StartLength", StartLength);
   XMA[0].XMAPhaseCheck("WPhase", WPhase, W_Method);
   XMA[0].XMALengthCheck("SmoothLength", SmoothLength);
   XMA[0].XMAPhaseCheck("SmoothPhase", SmoothPhase, SmoothMethod);

//---- initialization of variables of the start of data calculation
   min_rates_fatl=40;
   min_rates_xma=min_rates_fatl+XMA[0].GetStartBars(W_Method,StartLength+Step*StepsTotal,WPhase)+1;
   min_rates_total=min_rates_xma+XMA[0].GetStartBars(SmoothMethod,SmoothLength,SmoothPhase);
   StTot1=StepsTotal+1;
   StTot2=StepsTotal+2;

//---- set BullsBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(0,BullsBuffer,INDICATOR_DATA);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(BullsBuffer,true);   
   
//---- set BearsBuffer[] dynamic array as an indicator buffer
   SetIndexBuffer(1,BearsBuffer,INDICATOR_DATA);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(BearsBuffer,true);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(2,ColorBuffer,INDICATOR_COLOR_INDEX);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(ColorBuffer,true);
   
//---- shifting the starting point of the indicator 1 drawing by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- initializations of variable for indicator short name
   string shortname="Ultra Fatl";
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,2);

//---- lines drawing parameters  
   IndicatorSetInteger(INDICATOR_LEVELS,2);

   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,dUpLevel);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,UpLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,Levelstyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,0,LevelsWidth);

   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,dDnLevel);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,DnLevelsColor);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,Levelstyle);
   IndicatorSetInteger(INDICATOR_LEVELWIDTH,1,LevelsWidth);
//----
   return(0);
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
//---- checking for the sufficiency of bars for the calculation
   if(rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int limit,bar,maxbar0,maxbar1,maxbar2;
   double upsch,dnsch,fatl,price;

//---- calculation of maxbar initial index for the XMASeries() function
   maxbar0=rates_total-1;
   maxbar1=maxbar0-min_rates_fatl;
   maxbar2=maxbar0-min_rates_xma;

//---- calculation of the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit=maxbar0;                       // starting index for calculation of all bars
   else limit=rates_total-prev_calculated; // starting index for calculation of new bars

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);

//---- main loop of the indicator calculation
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      fatl=Fatl.FATLSeries(maxbar0,prev_calculated,rates_total,price,bar,true);

      for(int sm=int(StepsTotal); sm>=0 && !IsStopped(); sm--)
         invelue0[sm]=XMA[sm].XMASeries(maxbar1,prev_calculated,rates_total,W_Method,WPhase,period[sm],fatl,bar,true);

      if(bar>maxbar2)
        {
         if(bar) ArrayCopy(invelue1,invelue0,0,0,WHOLE_ARRAY);
         continue;
        }

      upsch=0;
      dnsch=0;
      for(int sm=int(StepsTotal); sm>=0 && !IsStopped(); sm--)
         if(invelue0[sm]>invelue1[sm]) upsch++;
      else                          dnsch++;

      BullsBuffer[bar]=XMA[StTot1].XMASeries(maxbar2,prev_calculated,rates_total,SmoothMethod,SmoothPhase,SmoothLength,upsch,bar,true);
      BearsBuffer[bar]=XMA[StTot2].XMASeries(maxbar2,prev_calculated,rates_total,SmoothMethod,SmoothPhase,SmoothLength,dnsch,bar,true);

      if(bar) ArrayCopy(invelue1,invelue0,0,0,WHOLE_ARRAY);
     }

//---- calculation of the 'limit' starting index for the bars recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit--;
      
//---- main loop of the indicator calculation
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ColorBuffer[bar]=0;

      if(BullsBuffer[bar]>BearsBuffer[bar])
        {
         if(BullsBuffer[bar]>dUpLevel || BearsBuffer[bar]<dDnLevel)
           {
            if(BullsBuffer[bar+1]<=BullsBuffer[bar]) ColorBuffer[bar]=7;
            else ColorBuffer[bar]=8;
           }
         else
           {
            if(BullsBuffer[bar+1]<=BullsBuffer[bar]) ColorBuffer[bar]=5;
            else ColorBuffer[bar]=6;
           }
        }

      if(BullsBuffer[bar]<BearsBuffer[bar])
        {
         if(BullsBuffer[bar]<dDnLevel || BearsBuffer[bar]>dUpLevel)
           {
            if(BearsBuffer[bar+1]<=BearsBuffer[bar]) ColorBuffer[bar]=1;
            else ColorBuffer[bar]=2;
           }
         else
           {
            if(BearsBuffer[bar+1]<=BearsBuffer[bar]) ColorBuffer[bar]=3;
            else ColorBuffer[bar]=4;
           }
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
