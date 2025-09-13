//+------------------------------------------------------------------+
//|                                                   EMA_RSI_VA.mq5 |
//|                                                          Integer |
//|                          https://login.mql5.com/en/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link      "https://login.mql5.com/en/users/Integer"
#property version   "1.00"
#property indicator_chart_window
#property indicator_buffers 3
#property indicator_plots   1
//--- plot Label1
#property indicator_label1  "Label1"
#property indicator_type1   DRAW_LINE
#property indicator_color1  clrRed
#property indicator_style1  STYLE_SOLID
#property indicator_width1  1
//--- input parameters
input int                RSIPeriod=14;                     // RSI period
input double             EMAPeriods        =  14;          // EMA period
input ENUM_APPLIED_PRICE Price             =  PRICE_CLOSE; // Applied price

int RSIHand;
int MAHand;
//--- indicator buffers
double Label1Buffer[];
double RSIBuf[];
double MABuf[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
   SetIndexBuffer(0,Label1Buffer,INDICATOR_DATA);
   SetIndexBuffer(1,RSIBuf,INDICATOR_CALCULATIONS);
   SetIndexBuffer(2,MABuf,INDICATOR_CALCULATIONS);

   RSIHand=iRSI(NULL,PERIOD_CURRENT,RSIPeriod,Price);
   MAHand=iMA(NULL,PERIOD_CURRENT,1,0,0,Price);
   if(RSIHand==INVALID_HANDLE || MAHand==INVALID_HANDLE)
     {
      Alert("Error in creation of indicator, please try again");
      return(-1);
     }

   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,RSIPeriod*2);
   PlotIndexSetString(0,PLOT_LABEL,MQL5InfoString(MQL5_PROGRAM_NAME));

   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
   static bool error=true;
   int start;
   if(prev_calculated==0)
     {
      error=true;
     }
   if(error)
     {
      start=RSIPeriod*2;
      Label1Buffer[start-1]=close[start-1];
      error=false;
     }
   else
     {
      start=prev_calculated-1;
     }
   if(CopyBuffer(RSIHand,0,0,rates_total-start,RSIBuf)==-1 || 
      CopyBuffer(MAHand,0,0,rates_total-start,MABuf)==-1
      )
     {
      error=true;
      return(0);
     }
   for(int i=start;i<rates_total;i++)
     {
      double RSvoltl=MathAbs(RSIBuf[i]-50)+1.0;
      double multi=(5.0+100.0/RSIPeriod)/(0.06+0.92*RSvoltl+0.02*MathPow(RSvoltl,2));
      double pdsx=multi*EMAPeriods;
      if(pdsx<1.0)
        {
         pdsx=1.0;
        }
      Label1Buffer[i]=MABuf[i]*2.0/(pdsx+1.0)+Label1Buffer[i-1]*(1.0-2.0/(pdsx+1.0));
     }
   return(rates_total);
  }
//+------------------------------------------------------------------+
