//+------------------------------------------------------------------+
//|                                         Custom Aroon Up & Dn.mq4 |
//|                                                        rafcamara |
//|                 Upgraded by Andriy Moraru from www.earnforex.com |
//+------------------------------------------------------------------+
#property  copyright "rafcamara"
#property  link      "rafcamara@yahoo.com"
#property version "1.0"
#property strict
//---- indicator settings
#property  indicator_separate_window
#property  indicator_buffers 2
#property  indicator_color1  DodgerBlue
#property  indicator_color2  Red

//---- indicator parameters
extern int AroonPeriod= 14;
extern bool MailAlert = false;  //Alerts will be mailed to address set in MT4 options
extern bool SoundAlert = false; //Alerts will sound on indicator cross

//---- indicator buffers
double     AroonUpBuffer[];
double     AroonDnBuffer[];

int LastBars=0;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- 2 additional buffers are used for counting.
   IndicatorBuffers(4);
   SetIndexBuffer(0,AroonUpBuffer);
   SetIndexBuffer(1,AroonDnBuffer);

//---- drawing settings
   SetIndexStyle(0,DRAW_LINE,STYLE_SOLID,1);
   SetIndexStyle(1,DRAW_LINE,STYLE_SOLID,1);
   SetIndexDrawBegin(0,200);
   SetIndexDrawBegin(1,200);
   IndicatorDigits(1);

//---- name for DataWindow and indicator subwindow label
   IndicatorShortName("Aroon Up & Dn("+(string)AroonPeriod+")");
//---- initialization done
   return(0);
  }
//+------------------------------------------------------------------+
//| Aroon Up & Dn                                                    |
//+------------------------------------------------------------------+
int start()
  {
//   double   AroonUp,AroonDn;
   int      ArPer,limit,i;
   int      UpBarDif,DnBarDif;
   int      counted_bars=IndicatorCounted();
   ArPer=AroonPeriod;                  //Short name

//---- check for possible errors
   if(counted_bars<0) return(-1);
   if(AroonPeriod<1) return(-1);
//---- initial zero
   if(counted_bars<1)
     {
      for(i=1;i<=ArPer;i++) AroonUpBuffer[Bars-i]=0.0;
      for(i=1;i<=ArPer;i++) AroonDnBuffer[Bars-i]=0.0;
     }

//---- last counted bar will be recounted
   if(counted_bars>0) counted_bars--;
   limit=Bars-counted_bars;

//----Calculation---------------------------
   for(i=0; i<limit; i++)
     {
      int HH=Highest(NULL,0,MODE_HIGH,ArPer,i);   //Periods from HH  	   
      int LL=Lowest(NULL,0,MODE_LOW,ArPer,i);        //Periods from LL

      UpBarDif=i-HH;                          //Period substraction
      DnBarDif=i-LL;                             //Period substraction

      AroonUpBuffer[i]=100+(100/ArPer)*(UpBarDif);            //Adjusted Aroon Up
      AroonDnBuffer[i]=100+(100/ArPer)*(DnBarDif);            //Adjusted Aroon Down

      if(LastBars!=Bars)
        {
         if((AroonUpBuffer[0]>AroonDnBuffer[0]) && (AroonUpBuffer[1]<=AroonDnBuffer[1]))
           {
            if(MailAlert) SendMail("Aroon Up & Down Indicator Alert","The indicator produced a cross (Blue ABOVE Red) on "+(string)Year()+"-"+(string)Month()+"-"+(string)Day()+" "+(string)Hour()+":"+(string)Minute());
            if(SoundAlert) Alert("Aroon Up & Down produced a cross (Blue ABOVE Red)");
           }
         else if((AroonUpBuffer[0]<AroonDnBuffer[0]) && (AroonUpBuffer[1]>=AroonDnBuffer[1]))
           {
            if(MailAlert) SendMail("Aroon Up & Down Indicator Alert","The indicator produced a cross (Blue BELOW Red) on "+(string)Year()+"-"+(string)Month()+"-"+(string)Day()+" "+(string)Hour()+":"+(string)Minute());
            if(SoundAlert) Alert("Aroon Up & Down produced a cross (Blue BELOW Red)");
           }
         LastBars=Bars;
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
