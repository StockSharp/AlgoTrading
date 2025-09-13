//+------------------------------------------------------------------+
//|                                                Price Channel.mq4 |
//|                                                                  |
//|                                       http://forex.kbpauk.ru/    |
//+------------------------------------------------------------------+
 
#property link      "http://forex.kbpauk.ru/"

#property indicator_chart_window
#property indicator_buffers 3
#property indicator_color1 Red
#property indicator_color2 Blue
#property indicator_color3 DodgerBlue
//---- input parameters
extern int ChannelPeriod = 14;
//---- buffers
double UpBuffer[];
double DnBuffer[];
double MdBuffer[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
   string short_name;
//---- indicator line
   SetIndexStyle(0, DRAW_LINE);
   SetIndexStyle(1, DRAW_LINE);
   SetIndexStyle(2, DRAW_LINE);
   SetIndexBuffer(0, UpBuffer);
   SetIndexBuffer(1, DnBuffer);
   SetIndexBuffer(2, MdBuffer);
//---- name for DataWindow and indicator subwindow label
   short_name="Price Channel("+ChannelPeriod+")";
   IndicatorShortName(short_name);
   SetIndexLabel(0, "UpCh");
   SetIndexLabel(1, "DownCh");
   SetIndexLabel(2, "MidCh");
//----
   SetIndexDrawBegin(0, ChannelPeriod);
   SetIndexDrawBegin(1, ChannelPeriod);
   SetIndexDrawBegin(2, ChannelPeriod);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Price Channel                                                         |
//+------------------------------------------------------------------+
int start()
  {
   int i, counted_bars = IndicatorCounted();
   int    k;
   double high, low, price;
//----
   if(Bars <= ChannelPeriod) 
       return(0);
//---- initial zero
   if(counted_bars < 1)
      for(i = 1;i <= ChannelPeriod; i++) 
          UpBuffer[Bars-i] = 0.0;
//----
   i = Bars - ChannelPeriod - 1;
   if(counted_bars >= ChannelPeriod) 
       i = Bars - counted_bars - 1;
   while(i >= 0)
     {
       high = High[i]; 
       low = Low[i]; 
       k = i - 1 + ChannelPeriod;
       while(k >= i)
         {
           price = High[k];
           if(high < price) 
               high = price;
           price = Low[k];
           if(low > price)  
               low = price;
           k--;
         } 
       UpBuffer[i] = high;
       DnBuffer[i] = low;
       MdBuffer[i] = (high + low) / 2;
       i--;
     }
   return(0);
  }
//+------------------------------------------------------------------+