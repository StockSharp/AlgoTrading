//+---------------------------------------------------------------------+
//|                                                          Oracle.mq5 |
//|                                     Copyright © 2009, Ivan Kornilov |
//|                                                    excelf@gmail.com |
//+---------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2009, Ivan Kornilov"
//---- author of the indicator
#property link      "excelf@gmail.com"
//---- description of the indicator
#property description "Indicator of forecast on three bars ahead!"
//---- drawing indicator in a separate window
#property indicator_separate_window
//----two buffers are used for calculation of drawing of the indicator
#property indicator_buffers 2
//---- two plots are used
#property indicator_plots   2
//+----------------------------------------------+
//| Oracle indicator drawing parameters          |
//+----------------------------------------------+
//---- drawing indicator 1 as a line
#property indicator_type1   DRAW_LINE
//---- Magenta color is used as the color of the bullish line of the indicator
#property indicator_color1  clrMagenta
//---- line of the indicator 1 is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- thickness of line of the indicator 1 is equal to 1
#property indicator_width1  1
//---- displaying of the bullish label of the indicator
#property indicator_label1  "Oracle"
//+----------------------------------------------+
//| Trigger indicator drawing parameters         |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_LINE
//---- DarkViolet color is used for the indicator bearish line
#property indicator_color2  clrDarkViolet
//---- the indicator 2 line is a continuous curve
#property indicator_style2  STYLE_SOLID
//---- indicator 2 line width is equal to 2
#property indicator_width2  2
//---- displaying of the bearish label of the indicator
#property indicator_label2  "Signal"
//+----------------------------------------------+
//|  Declaration of constants                    |
//+----------------------------------------------+
#define RESET 0 // the constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input uint OraclePeriod=55; //Oracle period
input ENUM_APPLIED_PRICE  applied_price=PRICE_CLOSE; //price type
input uint Smooth=8; //smoothing depth
input bool Recount=true; //redrawing              
input int Shift=3; // horizontal shift of the indicator in bars
//+----------------------------------------------+
//---- declaration of dynamic arrays that further 
//---- will be used as indicator buffers
double OracleBuffer[];
double SignalBuffer[];
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//----Declaration of variables for storing the indicators handles
int RSI_Handle,CCI_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=int(OraclePeriod+Smooth+1+4);

//---- getting handle of the RSI indicator
   RSI_Handle=iRSI(NULL,0,OraclePeriod,applied_price);
   if(RSI_Handle==INVALID_HANDLE) Print("Failed to get handle of the RSI indicator");

//---- getting handle of the CCI indicator
   CCI_Handle=iCCI(NULL,0,OraclePeriod,applied_price);
   if(CCI_Handle==INVALID_HANDLE) Print("Failed to get handle of the CCI indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,OracleBuffer,INDICATOR_DATA);
//---- shifting the indicator 1 horizontally by Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing shift of the beginning of counting of drawing the indicator 1 by OraclePeriod2
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,OraclePeriod+4);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(OracleBuffer,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,SignalBuffer,INDICATOR_DATA);
//---- shifting the indicator 2 horizontally by Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing shift of the beginning of counting of drawing the indicator 2 by min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SignalBuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"Oracle(",OraclePeriod,", ",Smooth,", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- determination of accuracy of displaying the indicator values
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
   if(BarsCalculated(RSI_Handle)<rates_total
      || BarsCalculated(CCI_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);

//---- declaration of local variables 
   int to_copy,limit,bar,maxbar,iii;
   double dDiv,Sum,RSI[],CCI[],Div[4];

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(RSI,true);
   ArraySetAsSeries(CCI,true);

   maxbar=int(rates_total-OraclePeriod-4-1);

//--- calculations of the necessary amount of data to be copied and
//----the limit starting number for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of calculation of an indicator
     {
      limit=maxbar; // starting index for calculation of all bars
     }
   else
     {
      limit=int(rates_total-prev_calculated); // starting number for calculation of new bars
      if(Recount) limit+=4;
     }
//----   
   to_copy=limit+1+4;

//---- copy newly appeared data into the arrays
   if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI)<=0) return(RESET);
   if(CopyBuffer(CCI_Handle,0,0,to_copy,CCI)<=0) return(RESET);

//---- indicator calculation loop with redrawing
   if(Recount)
      for(bar=limit; bar>=0 && !IsStopped(); bar--)
        {
         Div[0]=CCI[bar]-RSI[bar];

         dDiv=Div[0];
         if(bar>0) Div[1]=CCI[bar-1]-RSI[bar+1]-dDiv;
         else Div[1]=CCI[bar]-RSI[bar+1]-dDiv;

         dDiv+=Div[1];
         if(bar>1) Div[2]=CCI[bar-2]-RSI[bar+2]-dDiv;
         else if(bar>0) Div[2]=CCI[bar-1]-RSI[bar+2]-dDiv;
         else Div[2]=CCI[bar]-RSI[bar+2]-dDiv;

         dDiv+=Div[2];
         if(bar>2) Div[3]=CCI[bar-3]-RSI[bar+3]-dDiv;
         else if(bar>1) Div[3]=CCI[bar-2]-RSI[bar+3]-dDiv;
         else if(bar>0) Div[3]=CCI[bar-1]-RSI[bar+3]-dDiv;
         else Div[3]=CCI[bar]-RSI[bar+3]-dDiv;

         OracleBuffer[bar]=Div[ArrayMaximum(Div,0,WHOLE_ARRAY)]+Div[ArrayMinimum(Div,0,WHOLE_ARRAY)];

         if(bar>maxbar) SignalBuffer[bar]=EMPTY_VALUE;
         else
           {
            Sum=0.0;
            for(iii=0; iii<int(Smooth); iii++) Sum+=OracleBuffer[bar+iii];
            SignalBuffer[bar]=Sum/Smooth;
           }
        }

//---- indicator calculation loop without redrawing
   if(!Recount)
      for(bar=limit; bar>=0 && !IsStopped(); bar--)
        {
         Div[0]=CCI[bar]-RSI[bar];

         dDiv=Div[0];
         Div[1]=CCI[bar]-RSI[bar+1]-dDiv;

         dDiv+=Div[1];
         Div[2]=CCI[bar]-RSI[bar+2]-dDiv;

         dDiv+=Div[2];
         Div[3]=CCI[bar]-RSI[bar+3]-dDiv;

         OracleBuffer[bar]=Div[ArrayMaximum(Div,0,WHOLE_ARRAY)]+Div[ArrayMinimum(Div,0,WHOLE_ARRAY)];

         if(bar>maxbar) SignalBuffer[bar]=EMPTY_VALUE;
         else
           {
            Sum=0.0;
            for(iii=0; iii<int(Smooth); iii++) Sum+=OracleBuffer[bar+iii];
            SignalBuffer[bar]=Sum/Smooth;
           }
        }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
