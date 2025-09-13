//+------------------------------------------------------------------+
//|                                                          AMA.mq4 |
//|                      Copyright © 2004, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2004, by konKop,wellx"
#property link      "http://www.metaquotes.net"

#property indicator_chart_window
#property indicator_buffers 3
#property indicator_color1 Sienna
#property indicator_color2 DeepSkyBlue
#property indicator_color3 Gold

//---- input parameters
extern int       periodAMA=9;
extern int       nfast=2;
extern int       nslow=30;
extern double    G=2.0;
extern double    dK=2.0; 

//---- buffers
double kAMAbuffer[];
double kAMAupsig[];
double kAMAdownsig[];

//+------------------------------------------------------------------+

int    cbars=0, prevbars=0, prevtime=0;

double slowSC,fastSC;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- indicators
   SetIndexStyle(0,DRAW_LINE,0,2);
   SetIndexStyle(1,DRAW_ARROW);
   SetIndexArrow(1,159);
   SetIndexStyle(2,DRAW_ARROW);
   SetIndexArrow(2,159);
   //SetIndexDrawBegin(0,nslow+nfast);
   SetIndexBuffer(0,kAMAbuffer);
   SetIndexBuffer(1,kAMAupsig);
   SetIndexBuffer(2,kAMAdownsig);
   
   
   IndicatorDigits(4);
   
   //slowSC=0.064516;
   //fastSC=0.2;
   //cbars=IndicatorCounted();
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int start()
  {
   int    i,pos=0;
   double noise=0.000000001,AMA,AMA0,signal,ER;
   double dSC,ERSC,SSC,ddK;
   
   if (prevbars==Bars) return(0);
    
//---- TODO: add your code here
   slowSC=(2.0 /(nslow+1));
   fastSC=(2.0 /(nfast+1));
   cbars=IndicatorCounted();
   if (Bars<=(periodAMA+2)) return(0);
//---- check for possible errors
   if (cbars<0) return(-1);
//---- last counted bar will be recounted
   if (cbars>0) cbars--;
   pos=Bars-periodAMA-2;
   AMA0=Close[pos+1];
   while (pos>=0)
     {
      if(pos==Bars-periodAMA-2) AMA0=Close[pos+1];
      signal=MathAbs(Close[pos]-Close[pos+periodAMA]);
      noise=0.000000001;
      for(i=0;i<periodAMA;i++)
       {
        noise=noise+MathAbs(Close[pos+i]-Close[pos+i+1]);
       }
      ER =signal/noise;
      dSC=(fastSC-slowSC);
      ERSC=ER*dSC;
      SSC=ERSC+slowSC;
      AMA=AMA0+(MathPow(SSC,G)*(Close[pos]-AMA0));
      kAMAbuffer[pos]=AMA;

      ddK=(AMA-AMA0);
      if ((MathAbs(ddK)) > (dK*Point) && (ddK > 0)) kAMAupsig[pos] =AMA; else kAMAupsig[pos]=0;
      if ((MathAbs(ddK)) > (dK*Point) && (ddK < 0)) kAMAdownsig[pos]=AMA; else kAMAdownsig[pos]=0; 

     
      AMA0=AMA;
      pos--;
     }
//----
   prevbars=Bars;
   return(0);
  }

