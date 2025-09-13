//+------------------------------------------------------------------+
//|                                        ColorCoeffofLine_true.mq5 |
//|                                       Ramdass - Conversion only  |
//+------------------------------------------------------------------+
#property copyright "Ramdass - Conversion only"
#property link ""
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers 2
#property indicator_buffers 2 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Parameters of indicator drawing  |
//+-----------------------------------+
//---- drawing indicator as a five-color histogram
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- five colors are used in the histogram
#property indicator_color1 Gray,Lime,Blue,Red,Magenta
//---- Indicator line is a solid one
#property indicator_style1 STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1 2

//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
//---- Indicator input parameters
input int SMMAPeriod=5; //period of averaging
//+----------------------------------------------+

//---- declaration of dynamic arrays that further 
//---- will be used as indicator buffers
double ExtBuffer[],ColorExtBuffer[];
//---- declaration of a variable for storing handle of the indicator
int Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- transformation of the dynamic array ExtBuffer into an indicator buffer
   SetIndexBuffer(0,ExtBuffer,INDICATOR_DATA);

//---- shifting the beginning of drawing of the indicator MAPeriod
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,2*SMMAPeriod+4);

//---- set dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorExtBuffer,INDICATOR_COLOR_INDEX);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,SMMAPeriod+4);
   
//---- indexing the elements in buffers as timeseries
   ArraySetAsSeries(ExtBuffer,true);
   ArraySetAsSeries(ColorExtBuffer,true);

//---- Get indicator's handle
   Handle=iMA(NULL,0,SMMAPeriod,3,MODE_SMMA,PRICE_MEDIAN);
   if(Handle==INVALID_HANDLE)
      Print(" Failed to get handle of the SMMA indicator");
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
   if(BarsCalculated(Handle)<rates_total || rates_total<2*SMMAPeriod-1)
      return(0);

//---- declaration of local variables 
   int to_copy,limit1,limit2,Count,bar,cnt,iii,ndot=SMMAPeriod;
   double Sum,SMMA[],TYVar,ZYVar,TIndicatorVar,ZIndicatorVar,M,N,AY,AIndicator;
   
//---- indexing elements in arrays as timeseries
   ArraySetAsSeries(SMMA,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- Calculate the "limit" starting number for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0) // checking for the first start of calculation of an indicator
     {
      limit1=rates_total-SMMAPeriod-ndot-1; // starting index for calculation of all bars
      limit2=limit1-1;
      to_copy=rates_total-SMMAPeriod;
     }
   else
     {
      limit1=rates_total-prev_calculated; // starting number for calculation of new bars
      limit2=limit1; //  starting index for calculation of new bars
      to_copy=limit1+ndot+1;
     }

//---- copy newly appeared data in the SMMA[] array
   if(CopyBuffer(Handle,0,0,to_copy,SMMA)<=0) return(0);

//---- main cycle of calculation of the indicator
   for(bar=limit1; bar>=0; bar--)
     {
      TYVar = 0;
      ZYVar = 0;
      N = 0;
      M = 0;
      TIndicatorVar = 0;
      ZIndicatorVar = 0;

      //---- cycle of values summing
      for(cnt=ndot; cnt>=1; cnt--) // n=5 -  by five points
        {
         iii = bar + cnt - 1;
         Sum = (high[iii] + low[iii]) / 2;
         Count=SMMAPeriod+1-cnt;
         //ZYVar += Sum * Count; 
         ZYVar+=((high[bar+cnt-1]+low[bar+cnt-1])/2)*(6-cnt);
         TYVar+= Sum;
         N+=cnt*cnt; //equal to 55
         M+=cnt; //equal to 15
         ZIndicatorVar += SMMA[iii] * Count;
         TIndicatorVar += SMMA[iii];
        }

      AY=(TYVar+(N-2*ZYVar)*ndot/M)/M;
      AIndicator = (TIndicatorVar + (N - 2 * ZIndicatorVar) * ndot / M) / M;
      if(Symbol()=="EURUSD" || Symbol()=="GBPUSD" || Symbol()=="USDCAD" || Symbol()=="USDCHF"
         || Symbol()=="EURGBP" || Symbol()=="EURCHF" || Symbol()=="AUDUSD"
         || Symbol()=="GBPCHF")
        {ExtBuffer[bar]=(-1000)*MathLog(AY/AIndicator);}
      else {ExtBuffer[bar]=(1000)*MathLog(AY/AIndicator);}
     }
//---- Main cycle of the indicator coloring
   for(bar=limit2; bar>=0; bar--)
     {
      ColorExtBuffer[bar]=0;

      if(ExtBuffer[bar]>0)
        {
         if(ExtBuffer[bar]>ExtBuffer[bar+1]) ColorExtBuffer[bar]=1;
         if(ExtBuffer[bar]<ExtBuffer[bar+1]) ColorExtBuffer[bar]=2;
        }

      if(ExtBuffer[bar]<0)
        {
         if(ExtBuffer[bar]<ExtBuffer[bar+1]) ColorExtBuffer[bar]=3;
         if(ExtBuffer[bar]>ExtBuffer[bar+1]) ColorExtBuffer[bar]=4;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
