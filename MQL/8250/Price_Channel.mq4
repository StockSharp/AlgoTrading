//+------------------------------------------------------------------+
//|                                                Price Channel.mq4 |
//|                               Copyright © 2004, Poul_Trade_Forum |
//|                                                         Aborigen |
//|                                          http://forex.kbpauk.ru/ |
//+------------------------------------------------------------------+
#property link      "http://forex.kbpauk.ru/"

#property indicator_chart_window
#property indicator_buffers 3
#property indicator_color1 DodgerBlue
#property indicator_color2 DodgerBlue
#property indicator_color3 DodgerBlue
//---- input parameters
extern int ChannelPeriod=15;
extern int Shift=0;
//---- buffers
double UpBuffer[];
double DnBuffer[];
double MdBuffer[];
double Up[],Dn[],Md[];



//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
   string short_name;
   
ArraySetAsSeries(Up,true);
ArraySetAsSeries(Dn,true);
ArraySetAsSeries(Md,true);
//---- indicator line
   SetIndexStyle(0,DRAW_LINE);
   SetIndexStyle(1,DRAW_LINE);
   SetIndexStyle(2,DRAW_LINE,2);
   SetIndexBuffer(0,UpBuffer);
   SetIndexBuffer(1,DnBuffer);
   SetIndexBuffer(2,MdBuffer);
//---- name for DataWindow and indicator subwindow label
   short_name="Price Channel("+ChannelPeriod+")";
   IndicatorShortName(short_name);
   SetIndexLabel(0,"Up Channel");
   SetIndexLabel(1,"Down Channel");
   SetIndexLabel(2,"Middle Channel");
//----
   SetIndexDrawBegin(0,ChannelPeriod+Shift);
   SetIndexDrawBegin(1,ChannelPeriod+Shift);
   SetIndexDrawBegin(2,ChannelPeriod+Shift);

   SetIndexEmptyValue(0,0);
   SetIndexEmptyValue(1,0);
   SetIndexEmptyValue(2,0);

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Price Channel                                                         |
//+------------------------------------------------------------------+
int start()
  {
   int i,counted_bars=IndicatorCounted();
   int    k;
   double high,low,price;
//----
   if(Bars<=ChannelPeriod) return(0);
//---- initial zero
   if(counted_bars<1)
      for(i=1;i<=ChannelPeriod;i++) UpBuffer[Bars-i]=0.0;

ArrayResize(Up, Bars);
ArrayResize(Dn, Bars);
ArrayResize(Md, Bars);
//----
   i=Bars-ChannelPeriod-1;
   if(counted_bars>=ChannelPeriod) i=Bars-counted_bars;
   while(i>=0)
     {
       high=High[i]; low=Low[i]; k=i-1+ChannelPeriod;
      while(k>=i)
        {
         price=High[k];
         if(high<price) high=price;
         price=Low[k];
         if(low>price)  low=price;
         k--;
        } 
     Up[i]=high;
     Dn[i]=low;
     Md[i]=(high+low)/2;
     UpBuffer[i]=Up[i+Shift];
     DnBuffer[i]=Dn[i+Shift];
     MdBuffer[i]=Md[i+Shift];//iMAOnArray(Md,Bars,5,0,MODE_SMA,i);///Md[i+Shift];
      i--;
     }
   return(0);
  }
//+------------------------------------------------------------------+