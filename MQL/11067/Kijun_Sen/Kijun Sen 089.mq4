//+------------------------------------------------------------------+
//|                                                 Kijun Sen 55.mq4 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//| Mod and Update : File45 August 2015                              |
//| https://www.mql5.com/en/users/file45/publications                |
//|                                     Copyright © 2004, AlexSilver |
//|                                                  http://viac.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "2.00"
#property strict
#property indicator_chart_window
#property indicator_buffers 1
#property indicator_color1  LimeGreen

input int Kijun=89; // Kijun Sen Period

int ShiftKijun=0, a_begin;

double Kijun_Buffer[];

int OnInit()
{
   SetIndexStyle(0,DRAW_LINE);
   SetIndexBuffer(0,Kijun_Buffer);
   SetIndexDrawBegin(0,Kijun+ShiftKijun-1);
   SetIndexShift(0,ShiftKijun);
   SetIndexLabel(0,"Kijun Sen+");

   return(INIT_SUCCEEDED);
}

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
   int    i,k;
   int    counted_bars=IndicatorCounted();
   double highz,lowz,price;

   if(Bars<=Kijun) return(0);
//---- initial zero
   if(counted_bars<1)
   {
      for(i=1;i<=Kijun;i++) 
      Kijun_Buffer[Bars-i]=0;
   }
//---- Kijun Sen
   i=Bars-Kijun;
   if(counted_bars>Kijun) i=Bars-counted_bars-1;
   while(i>=0)
   {
      highz=High[i]; lowz=Low[i]; k=i-1+Kijun;
      while(k>=i)
      {
         price=High[k];
         if(highz<price) highz=price;
         price=Low[k];
         if(lowz>price) lowz=price;
         k--;
      }
      Kijun_Buffer[i+ShiftKijun]=(highz+lowz)/2;
      i--;
   } 
   i=ShiftKijun-1;
   while(i>=0)
   {
      highz=High[0]; lowz=Low[0]; k=Kijun-ShiftKijun+i;
      while(k>=0)
      {
         price=High[k];
         if(highz<price) highz=price;
         price=Low[k];
         if(lowz>price) lowz=price;
         k--;
      }
      Kijun_Buffer[i]=(highz+lowz)/2;
      i--;
   }
   return(rates_total);
}
 
