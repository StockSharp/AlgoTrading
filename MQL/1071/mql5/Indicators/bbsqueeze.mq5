//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2011, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+
/*
 * The operation of the indicator requires
 * SmoothAlgorithms.mqh
 * to be placed in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+ 
//|                                                    BBSqueeze.mq5 | 
//|                                     Copyright © 2005, Nick Bilak |
//|                                              beluck[AT]gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, Nick Bilak"
#property link      "http://metatrader.50webs.com/" 
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers is 4
#property indicator_buffers 4 
//---- only two plots are used
#property indicator_plots   4

//+-----------------------------------+ 
//|  declaration of constants              |
//+-----------------------------------+
#define RESET  0   // The constant for returning the indicator recalculation command to the terminal
//+-----------------------------------+
//|  Indicator drawing parameters   |
//+-----------------------------------+
//---- drawing the indicator as a histogram
#property indicator_type1 DRAW_HISTOGRAM
//---- Teal color is used
#property indicator_color1 Teal
//---- indicator line is a solid line
#property indicator_style1 STYLE_SOLID
//---- indicator line width is 2
#property indicator_width1 2
//---- displaying the indicator label
#property indicator_label1 "Uptrend"

//---- drawing the indicator as a histogram
#property indicator_type2 DRAW_HISTOGRAM
//---- Magenta color is used
#property indicator_color2 Magenta
//---- indicator line is a solid line
#property indicator_style2 STYLE_SOLID
//---- indicator line width is 2
#property indicator_width2 2
//---- displaying the signal line label
#property indicator_label2  "Downtrend"

//+-----------------------------------+
//|  Indicator drawing parameters   |
//+-----------------------------------+
//---- drawing the indicator as labels
#property indicator_type3 DRAW_ARROW
//---- Blue color is used
#property indicator_color3 Blue
//---- indicator line width is 2
#property indicator_width3 2
//---- displaying the indicator label
#property indicator_label3 "Strong trend"

//---- drawing the indicator as labels
#property indicator_type4 DRAW_ARROW
//---- MediumPurple color is used
#property indicator_color4 MediumPurple
//---- indicator line width is 2
#property indicator_width4 2
//---- displaying the signal line label
#property indicator_label4  "Weak trend"
//+-----------------------------------+
//|  Description of averaging classes      |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from SmoothAlgorithms.mqh
CXMA XMA1;
//+-----------------------------------+
//|  declaration of enumerations          |
//+-----------------------------------+
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
   PRICE_TRENDFOLLOW1_   //TrendFollow_2 Price 
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
//|  INDICATOR INPUT PARAMETERS     |
//+-----------------------------------+
input Smooth_Method BB_Method=MODE_EMA_; //histogram smoothing method
input int BB_Period = 20; //Bollinger period
input int BB_Phase= 100;  //Bollinger smoothing parameter,
                          //for JJMA, it varies within the range -100 ... +100 and influences the quality of the transient process;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input double BB_Deviation=2.0; //Bollinger deviation
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//price constant
/* , used for the indicator calculation ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input double ATR_Period=20; //ATR period
input double ATR_Factor=1.5; //ATR coefficient
//+-----------------------------------+
//---- Declaration of integer variables of data starting point
int min_rates_total,min_rates_xma;
//---- Declaration of global variables
int Count[];
double Xma[];
//---- declaration of integer variables for indicators handles
int ATR_Handle,STD_Handle;
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double UpHistBuffer[],DnHistBuffer[],UpArrBuffer[],DnArrBuffer[];
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- Initialization of variables of data starting point
   min_rates_xma=XMA1.GetStartBars(BB_Method,BB_Period,BB_Phase);
   min_rates_total=int(MathMax(min_rates_xma,ATR_Period));

//---- getting the iStdDev indicator handle
   STD_Handle=iStdDev(NULL,0,BB_Period,0,MODE_SMA,PRICE_CLOSE);
   if(STD_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get the iStdDev indicator handle");
      return(1);
     }

//---- getting the ATR indicator handle
   ATR_Handle=iATR(NULL,0,15);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get the iATR indicator handle");
      return(1);
     }

//---- Memory allocation for arrays of variables
   if(ArrayResize(Count,BB_Period)<BB_Period) Print("Failed to allocate memory for the Count[] array");
   if(ArrayResize(Xma,BB_Period)<BB_Period) Print("Failed to allocate memory for the Xma[] array");
   ZeroMemory(Count);
   ZeroMemory(Xma);

//---- setting dynamic array as indicator buffer
   SetIndexBuffer(0,UpHistBuffer,INDICATOR_DATA);
//---- shifting the starting point of the indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that will be invisible on the chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(UpHistBuffer,true);

//---- setting dynamic array as indicator buffer
   SetIndexBuffer(1,DnHistBuffer,INDICATOR_DATA);
//---- shifting the starting point of the indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that will be invisible on the chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(DnHistBuffer,true);

//---- setting dynamic array as indicator buffer
   SetIndexBuffer(2,UpArrBuffer,INDICATOR_DATA);
//---- shifting the starting point of the indicator drawing
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that will be invisible on the chart
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indicator symbol
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(UpArrBuffer,true);

//---- setting dynamic array as indicator buffer
   SetIndexBuffer(3,DnArrBuffer,INDICATOR_DATA);
//---- shifting the starting point of the indicator drawing
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that will be invisible on the chart
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indicator symbol
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(DnArrBuffer,true);

//--- creating a name to be displayed in a separate subwindow and in a tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,"BBSqueeze");
//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- end of initialization
   return(0);
  }
//+------------------------------------------------------------------+  
//| Custom iteration function                                        | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // history in bars at the current tick
                const int prev_calculated,// history in bars at the previous tick
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
//---- Checking for the sufficiency of the number of bars for the calculation
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(STD_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);

//---- declaring local variables 
   int to_copy,limit,bar,maxbar,maxbar1;
   double STD[],ATR[],lregress,bbs,price_;

   maxbar=rates_total-1;
   maxbar1=maxbar-min_rates_xma-2*BB_Period;

//---- calculation of the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
      limit=rates_total-1; // starting index for the calculation of all bars
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars

//---- calculation of the necessary amount of data to be copied
   to_copy=limit+1;
//---- copying new data into the STD[] and ATR[] arrays
   if(CopyBuffer(STD_Handle,0,0,to_copy,STD)<=0) return(RESET);
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);

//---- indexing array elements as time series  
   ArraySetAsSeries(STD,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);

//---- Main indicator calculation loop
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      price_=PriceSeries(AppliedPrice,bar,open,low,high,close);
      Xma[Count[0]]=XMA1.XMASeries(maxbar,prev_calculated,rates_total,BB_Method,BB_Phase,BB_Period,price_,bar,true);
      if(bar>maxbar1)
        {
         Recount_ArrayZeroPos(Count,BB_Period);
         continue;
        }

      lregress=LinearRegressionValue(BB_Period,bar,open,low,high,close);

      if(lregress<0)
        {
         UpHistBuffer[bar]=EMPTY_VALUE;
         DnHistBuffer[bar]=lregress;
        }
      else
        {
         UpHistBuffer[bar]=lregress;
         DnHistBuffer[bar]=EMPTY_VALUE;
        }

      bbs=BB_Deviation*STD[bar]/(ATR[bar]*ATR_Factor);

      if(bbs<1)
        {
         DnArrBuffer[bar]=0;
         UpArrBuffer[bar]=EMPTY_VALUE;
        }
      else
        {
         UpArrBuffer[bar]=0;
         DnArrBuffer[bar]=EMPTY_VALUE;
        }

      if(bar) Recount_ArrayZeroPos(Count,BB_Period);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+    
//| linear regression calculation                                        | 
//+------------------------------------------------------------------+ 
double LinearRegressionValue
(
 int Len,
 int index,
 const double &Open[],
 const double &Low[],
 const double &High[],
 const double &Close[]
 )
  {
//----
   double SumBars,Sum1,Sum2,SumY,Slope,SumSqrBars;
   double dxma,Num1,Num2,HH,LL,Intercept,LinearRegValue;

   SumY=0;
   Sum1=0;
//----
   SumBars=Len *(Len-1)*0.5;
   SumSqrBars=(Len-1)*Len *(2*Len-1)/6;
//----
   for(int x=0; x<Len;x++)
     {
      HH=Low[index+x];
      LL=High[index+x];

      for(int y=x; y<x+Len; y++)
        {
         HH=MathMax(HH,High[index+y]);
         LL=MathMin(LL,Low[index+y]);
        }

      dxma=Close[index+x]-((HH+LL)/2+Xma[Count[x]])/2;
      Sum1+=x*dxma;
      SumY+=dxma;
     }

   Sum2=SumBars*SumY;
   Num1=Len*Sum1-Sum2;
   Num2=SumBars*SumBars-Len*SumSqrBars;
//----
   if(Num2!=0.0) Slope=Num1/Num2;
   else Slope=0;

   Intercept=(SumY-Slope*SumBars)/Len;
   LinearRegValue=Intercept+Slope*(Len-1);
//----     
   return(LinearRegValue);
  }
//+------------------------------------------------------------------+
//|  recalculating position of the newest element in the array               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos
(
 int &CoArr[],// Return the current value of the price series by the link
 uint Size
 )
// Recount_ArrayZeroPos(count, Length)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max1=int(Size-1);
   Max2=int(Size);

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
