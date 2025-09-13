//+------------------------------------------------------------------+
//|                                                super-signals.mq4 |
//|                Copyright © 2006, Nick Bilak, beluck[AT]gmail.com |
//| Modified by Robert Hill to do alert or send email                |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, Nick Bilak"
#property link      "http://www.forex-tsd.com/"

#property indicator_chart_window
#property indicator_buffers 2
#property indicator_color1 Red
#property indicator_width1 2
#property indicator_color2 Lime
#property indicator_width2 2

extern int SignalGap = 4;

extern bool SoundON=true;
extern bool EmailON=false;

int dist=24;
double b1[];
double b2[];
int flagval1 = 0;
int flagval2 = 0;

int init()  {
   SetIndexStyle(0,DRAW_ARROW,STYLE_SOLID,1);
   SetIndexStyle(1,DRAW_ARROW,STYLE_SOLID,1);
   SetIndexArrow(1,233);
   SetIndexArrow(0,234);
   SetIndexBuffer(0,b1);
   SetIndexBuffer(1,b2);
   return(0);
}
int start() {
   int counted_bars=IndicatorCounted();
   int k,i,j,limit,hhb,llb;
   
   if (counted_bars<0) return(-1);
   if (counted_bars>0) counted_bars--;
   limit=Bars-1;
   if(counted_bars>=1) limit=Bars-counted_bars-1;
   if (limit<0) limit=0;

   for (i=limit;i>=0;i--)   {
      hhb = Highest(NULL,0,MODE_HIGH,dist,i-dist/2);
      llb = Lowest(NULL,0,MODE_LOW,dist,i-dist/2);

      if (i==hhb)
      {
         if (i == 1 && flagval1==0)
         {
           flagval1=1;
           flagval2=0;
           if (SoundON) Alert("BUY signal at Ask=",Ask,"\n Bid=",Bid,"\n Time=",TimeToStr(CurTime(),TIME_DATE)," ",TimeHour(CurTime()),":",TimeMinute(CurTime()),"\n Symbol=",Symbol()," Period=",Period());
           if (EmailON) SendMail("BUY signal alert","BUY signal at Ask="+DoubleToStr(Ask,4)+", Bid="+DoubleToStr(Bid,4)+", Date="+TimeToStr(CurTime(),TIME_DATE)+" "+TimeHour(CurTime())+":"+TimeMinute(CurTime())+" Symbol="+Symbol()+" Period="+Period());
         }
         b1[i]=High[hhb]+SignalGap*Point;
      }
      if (i==llb)
      {
         if (i == 1 && flagval2==0)
         {
           flagval2=1;
           flagval1=0;
           if (SoundON) Alert("SELL signal at Ask=",Ask,"\n Bid=",Bid,"\n Date=",TimeToStr(CurTime(),TIME_DATE)," ",TimeHour(CurTime()),":",TimeMinute(CurTime()),"\n Symbol=",Symbol()," Period=",Period());
           if (EmailON) SendMail("SELL signal alert","SELL signal at Ask="+DoubleToStr(Ask,4)+", Bid="+DoubleToStr(Bid,4)+", Date="+TimeToStr(CurTime(),TIME_DATE)+" "+TimeHour(CurTime())+":"+TimeMinute(CurTime())+" Symbol="+Symbol()+" Period="+Period());
         }
         b2[i]=Low[llb]-SignalGap*Point;
      }
   }
   return(0);
}


