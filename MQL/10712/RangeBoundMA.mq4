//+------------------------------------------------------------------+
//|                                                 RangeBoundMA.mq4 |
//|                               Shenzhen City YuHe E-commerce Ltd. |
//|               find the biggest difference price between the 3 SMA|
//|    when it is still in some range, we say it is in the rangebound|
//+------------------------------------------------------------------+
#property  copyright "Shenzhen City YuHe E-commerce Ltd. Author: He Yigeng"
#property  link      ""
//---- indicator settings
#property  indicator_separate_window
#property  indicator_buffers 1
#property  indicator_color1  Gold
#property  indicator_width1  2
//---- indicator parameters
extern int FastSMA=38;
extern int MidSMA=140;
extern int SlowSMA=210;
extern int SignalSMA=9;
//---- indicator buffers
double     OsmaBuffer[];
double     MacdBuffer[];
double     SignalBuffer[];
double     MacdBufferFS[];
double     MacdBufferFM[];
double     MacdBufferMS[];
double     MAXmaBuffer[];
double     FS[];
double     MS[];
double     FM[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- 2 additional buffers are used for counting.
   IndicatorBuffers(3);
//---- drawing settings
   SetIndexStyle(0,DRAW_LINE);
   SetIndexDrawBegin(0,SignalSMA);
   IndicatorDigits(Digits+2);
//---- 3 indicator buffers mapping
   SetIndexBuffer(0,OsmaBuffer);
   SetIndexBuffer(1,MacdBuffer);
   SetIndexBuffer(2,SignalBuffer);
//---- name for DataWindow and indicator subwindow label
   IndicatorShortName("RangeBoundMA("+FastSMA+","+MidSMA+","+SlowSMA+")");
   SetIndexLabel(0,"RangeBoundMA("+FastSMA+","+MidSMA+","+SlowSMA+")");
//---- initialization done
   return(0);
  }
//+------------------------------------------------------------------+
//| Moving Average of Oscillator                                     |
//+------------------------------------------------------------------+
int start()
  {
   int limit;
   int counted_bars=IndicatorCounted();
//---- last counted bar will be recounted
   if(counted_bars>0) counted_bars--;
   limit=Bars-counted_bars;
//---- macd counted in the 1-st additional buffer
   for(int i=0; i<limit; i++)
      {
      if(iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i)>=iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i)
         &&iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i)>iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i))
         MacdBuffer[i]=(iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i)-iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i));
      if(iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i)>=iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i)
         &&iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i)>iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i))
         MacdBuffer[i]=(iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i)-iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i));
      if(iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i)>=iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i)
         &&iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i)>iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i))
         MacdBuffer[i]=(iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i)-iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i));
      if(iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i)>=iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i)
         &&iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i)>iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i))
         MacdBuffer[i]=(iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i)-iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i));
      if(iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i)>=iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i)
         &&iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i)>iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i))
         MacdBuffer[i]=(iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i)-iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i));
      if(iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i)>=iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i)
         &&iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i)>iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i))
         MacdBuffer[i]=(iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i)-iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i));
      if(iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i)==iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i)
         &&iMA(NULL,0,MidSMA,0,MODE_SMA,PRICE_CLOSE,i)==iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i))
         MacdBuffer[i]=(iMA(NULL,0,SlowSMA,0,MODE_SMA,PRICE_CLOSE,i)-iMA(NULL,0,FastSMA,0,MODE_SMA,PRICE_CLOSE,i));
      }
//---- main loop
   for(i=0; i<limit; i++)
     //MacdBuffer[i]=MAXmaBuffer[i];
     OsmaBuffer[i]=MacdBuffer[i]/Point/10;
     //OsmaBuffer[i]=MacdBuffer[i]-SignalBuffer[i];

    
//---- done
   return(0);
}
//+------------------------------------------------------------------+

