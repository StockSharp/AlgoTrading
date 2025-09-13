//+------------------------------------------------------------------+
//|                                            TrendContinuation.mq5 |
//|                                     Copyright © 2007, Doc Gaines |
//|                                      dr_richard_gaines@yahoo.com |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2007, Doc Gaines"
//---- link to the website of the author
#property link      "dr_richard_gaines@yahoo.com"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- two buffers are used for the indicator calculation and drawing
#property indicator_buffers 2
//---- one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 1 as a cloud
#property indicator_type1   DRAW_FILLING
//---- the following colors are used for the indicator
#property indicator_color1  clrRed,clrLime
//---- displaying of the bullish label of the indicator
#property indicator_label1  "TrendContinuation Minus;TrendContinuation Plus"

//+----------------------------------------------+
//|  CXMA class description                      |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh>
//+----------------------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1,XMA2;
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
enum Applied_price_ //Type of constant
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
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
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
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input uint NPeriod=20; //calculation period
input Smooth_Method XMethod=MODE_T3; //averaging method
input uint XPeriod=5; //averaging depth
input int XPhase=61; //averaging parameter,
                     //for JJMA, it varies within the range -100 ... +100 and influences on the quality of the transient period;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ IPC=PRICE_CLOSE;//price constant
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double PlusBuffer[];
double MinusBuffer[];
//---- Declaration of integer variables of data starting point
int min_rates_total,min_rates_,nsize;
//---- declaration of dynamic arrays that will further be 
// used as ring buffers
int Count[];
double Change_p[],Change_n[],CF_p[],CF_n[];
//+------------------------------------------------------------------+
//|  Recalculation of position of the newest element in the array    |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// Return the current value of the price series by the link
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
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_=int(NPeriod+1);
   min_rates_total=int(NPeriod+XMA1.GetStartBars(XMethod,XPeriod,XPhase));

//---- Memory allocation for arrays of variables
   nsize=int(NPeriod);
   ArrayResize(Count,nsize);
   ArrayResize(Change_p,nsize);
   ArrayResize(Change_n,nsize);
   ArrayResize(CF_p,nsize);
   ArrayResize(CF_n,nsize);

//---- Initialization of arrays of variables
   ArrayInitialize(Count,0.0);
   ArrayInitialize(Change_p,0.0);
   ArrayInitialize(Change_n,0.0);
   ArrayInitialize(CF_p,0.0);
   ArrayInitialize(CF_n,0.0);

//---- transformation of the PlusBuffer dynamic array into an indicator buffer
   SetIndexBuffer(0,PlusBuffer,INDICATOR_DATA);
//---- transformation of the MinusBuffer dynamic array into an indicator buffer
   SetIndexBuffer(1,MinusBuffer,INDICATOR_DATA);

//---- shifting the starting point for drawing indicator 1 by min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting values of the indicator that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"TrendContinuation");
//--- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
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
//---- checking for the sufficiency of the number of bars for the calculation
   if(rates_total<min_rates_total) return(0);

//---- Declaration of variables with a floating point  
   double dprice,k_p,k_n,ch_p,ch_n,cff_n,cff_p;
//---- Declaration of integer variables and getting the bars already calculated
   int first,bar,iii,i0,i1;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0) //checking for the first start of calculation of an indicator
      first=1; // starting number for calculation of all bars
   else first=prev_calculated-1; // starting index for the calculation of new bars

//---- Main calculation loop of the indicator
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      dprice=PriceSeries(IPC,bar,open,low,high,close)-PriceSeries(IPC,bar-1,open,low,high,close);
      i0=Count[0];
      i1=Count[1];
      Change_p[i0]=0.0;
      CF_p[i0]=0.0;
      Change_n[i0]=0.0;
      CF_n[i0]=0.0;

      if(dprice>0)
        {
         Change_p[i0]=-dprice;
         CF_p[i0]=Change_p[i0]+CF_p[i1];
        }
      else if(dprice<0)
        {
         Change_n[i0]=+dprice;
         CF_n[i0]=Change_n[i0]+CF_n[i1];
        }

      ch_p=0.0;
      ch_n=0.0;
      cff_p=0.0;
      cff_n=0.0;

      for(iii=0; iii<nsize; iii++) ch_p+=Change_p[iii];
      for(iii=0; iii<nsize; iii++) ch_n+=Change_n[iii];
      for(iii=0; iii<nsize; iii++) cff_p+=CF_p[iii];
      for(iii=0; iii<nsize; iii++) cff_n+=CF_n[iii];

      k_p=ch_p-cff_n;
      k_n=ch_n-cff_p;

      PlusBuffer[bar]=XMA1.XMASeries(min_rates_,prev_calculated,rates_total,XMethod,XPhase,XMethod,k_p,bar,false)/_Point;
      MinusBuffer[bar]=XMA2.XMASeries(min_rates_,prev_calculated,rates_total,XMethod,XPhase,XMethod,k_n,bar,false)/_Point;

      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,nsize);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
