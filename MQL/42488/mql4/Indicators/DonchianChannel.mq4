//+------------------------------------------------------------------+
//|                                              DonchianChannel.mq4 |
//|                        Copyright 2021, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
#property indicator_chart_window
#property  indicator_buffers 3
#property indicator_levelstyle DRAW_LINE

#property indicator_color1 clrAqua
#property indicator_color2 clrAqua
#property indicator_color3 clrAqua

#property indicator_width1 2
#property indicator_width2 2
#property indicator_width3 2

input int Channelperiod = 20;
input int Shift = 0;

double UpperBand[];
double MiddleBand[];
double LowerBand[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
   SetIndexBuffer(0, UpperBand);
   SetIndexBuffer(1, MiddleBand);
   SetIndexBuffer(2, LowerBand);
   string short_name="Donchian("+IntegerToString(Channelperiod)+")";
   IndicatorShortName(short_name);
   SetIndexDrawBegin(0, Channelperiod);
   SetIndexDrawBegin(1, Channelperiod);
   SetIndexDrawBegin(2, Channelperiod);
   if ((Channelperiod <=1) || (Shift < 0))
   {
   MessageBox("Wrong input parameters");
   return(INIT_PARAMETERS_INCORRECT);
   }
   
   return(INIT_SUCCEEDED);
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
int i,limit;
   if((rates_total<= Channelperiod) || (Channelperiod <= 1))
      return(0);
   ArraySetAsSeries(UpperBand, true);
   ArraySetAsSeries(MiddleBand, true);
   ArraySetAsSeries(LowerBand, true);
   
   for(i=0; i<rates_total - Channelperiod; i++)
      {
         UpperBand[i] = High[ArrayMaximum(High, Channelperiod, i + Shift)];
         LowerBand[i] = Low[ArrayMinimum(Low, Channelperiod, i + Shift)];
         MiddleBand[i] = (High[ArrayMaximum(High, Channelperiod, i + Shift)] + Low[ArrayMinimum(Low, Channelperiod, i + Shift)])/2 ;
      }
   
   return(rates_total);
  }
   
   
   
   
