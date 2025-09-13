/*------------------------------------------------------------------+
 |                                                       RSI_MA.mq4 |
 |                                                 Copyright © 2010 |
 +------------------------------------------------------------------*/
#property link      "basisforex@gmail.com"
//-----
#property indicator_separate_window
#property indicator_minimum 0
#property indicator_maximum 100
#property indicator_buffers 1
#property indicator_color1 Red
#property indicator_level1 20
#property indicator_level2 80
#property indicator_levelcolor White
//---- input parameters
extern int RSIPeriod=14;
//---- buffers
double RSIBuffer[];
double PosBuffer[];
double NegBuffer[];
double RSIBuf[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
   string short_name;
//---- 2 additional buffers are used for counting.
   IndicatorBuffers(4);
   SetIndexBuffer(1,PosBuffer);
   SetIndexBuffer(2,NegBuffer);
   SetIndexBuffer(3,RSIBuf);
//---- indicator line
   SetIndexStyle(0,DRAW_LINE, STYLE_SOLID, 4);
   SetIndexBuffer(0,RSIBuffer);
//---- name for DataWindow and indicator subwindow label
   short_name="RSI-MA("+RSIPeriod+")";
   IndicatorShortName(short_name);
   SetIndexLabel(0,short_name);
//----
   SetIndexDrawBegin(0,RSIPeriod);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Relative Strength Index                                          |
//+------------------------------------------------------------------+
int start()
  {
   int    i,counted_bars=IndicatorCounted();
   double rel,negative,positive;
   int maDif;
//----
   if(Bars<=RSIPeriod) return(0);
//---- initial zero
   if(counted_bars<1)
      for(i=1;i<=RSIPeriod;i++) RSIBuffer[Bars-i]=0.0;
//----
   i=Bars-RSIPeriod-1;
   if(counted_bars>=RSIPeriod) i=Bars-counted_bars-1;
   while(i>=0)
     {
      double sumn=0.0,sump=0.0;
      if(i==Bars-RSIPeriod-1)
        {
         int k=Bars-2;
         //---- initial accumulation
         while(k>=i)
           {
            rel=Close[k]-Close[k+1];
            if(rel>0) sump+=rel;
            else      sumn-=rel;
            k--;
           }
         positive=sump/RSIPeriod;
         negative=sumn/RSIPeriod;
        }
      else
        {
         //---- smoothed moving average
         rel=Close[i]-Close[i+1];
         if(rel>0) sump=rel;
         else      sumn=-rel;
         positive=(PosBuffer[i+1]*(RSIPeriod-1)+sump)/RSIPeriod;
         negative=(NegBuffer[i+1]*(RSIPeriod-1)+sumn)/RSIPeriod;
        }
      PosBuffer[i]=positive;
      NegBuffer[i]=negative;
      if(negative==0.0) RSIBuf[i]=0.0;
      else RSIBuf[i]=100.0-100.0/(1+positive/negative);
      maDif = (iMA(NULL, 0, RSIPeriod, 0, MODE_EMA, PRICE_WEIGHTED, i) - iMA(NULL, 0, RSIPeriod, 0, MODE_EMA, PRICE_WEIGHTED, i + 1)) / Point;
      RSIBuffer[i] = RSIBuf[i] * maDif;
      if(RSIBuffer[i] > 99) RSIBuffer[i] = 99;
      if(RSIBuffer[i] < 1) RSIBuffer[i] = 1;
      i--;
     }
     Comment("Digit= ", Digits);
//----
   return(0);
  }
//+------------------------------------------------------------------+