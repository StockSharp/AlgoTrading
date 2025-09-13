//+------------------------------------------------------------------+
//|                                            ATR Trailing Stop.mq4 |
//|                                                                  |
//|                                                                   |
//+------------------------------------------------------------------+
#property  copyright "Copyright Team Aphid"
#property  link      ""
//---- indicator settings
#property  indicator_chart_window
#property  indicator_buffers 2
#property  indicator_color1  Blue
#property  indicator_color2  Red

//---- indicator parameters
extern int BackPeriod   = 1000;
extern int ATRPeriod   = 3;
extern double Factor = 3;
extern bool TypicalPrice = false;

//---- indicator buffers
double     ind_buffer1[];
double     ind_buffer2[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
//---- drawing settings  
   SetIndexStyle(0,DRAW_LINE,EMPTY,2);
   SetIndexDrawBegin(0,ATRPeriod);
   SetIndexBuffer(0,ind_buffer1);
   SetIndexStyle(1,DRAW_LINE,EMPTY,2);
   SetIndexDrawBegin(1,ATRPeriod);
   SetIndexBuffer(1,ind_buffer2);

   IndicatorDigits(MarketInfo(Symbol(),MODE_DIGITS)+2);
//---- name for DataWindow and indicator subwindow label
   IndicatorShortName("ATR Trailing Stop("+ATRPeriod+" * "+Factor+")");
   SetIndexLabel(0,"Support");
   SetIndexLabel(1,"Resistance");
//---- initialization done
   return(0);
  }
//+------------------------------------------------------------------+
//| Moving Averages Convergence/Divergence                           |
//+------------------------------------------------------------------+
int start()
  {
   int limit;
   int counted_bars=IndicatorCounted();
   double PrevUp, PrevDn;
   double CurrUp, CurrDn;
   double PriceLvl;
   double LvlUp = 0;
   double LvlDn = 1000;
   int Dir = 1;
   int InitDir;
//---- check for possible errors
   if(counted_bars<0) return(-1);
//---- last counted bar will be recounted
   if(counted_bars>0) counted_bars--;
   limit=Bars-counted_bars;
//---- fill in buffervalues
   InitDir = 0;
   for(int i=BackPeriod; i>=0; i--)
   {
      if (TypicalPrice) PriceLvl = (High[i] + Low[i] + Close[i])/3;
      else PriceLvl = Close[i];  

      if(InitDir == 0) {
         CurrUp=Close[i] - (iATR(NULL,0,ATRPeriod,i) * Factor);
         PrevUp=Close[i-1] - (iATR(NULL,0,ATRPeriod,i-1) * Factor);
         CurrDn=Close[i] + (iATR(NULL,0,ATRPeriod,i) * Factor);
         PrevDn=Close[i-1] + (iATR(NULL,0,ATRPeriod,i-1) * Factor);
           
         if (CurrUp > PrevUp) Dir = 1;
         LvlUp = CurrUp;
         if (CurrDn < PrevDn) Dir = -1;
         LvlDn = CurrDn;
         InitDir = 1;
       
      }
      
      CurrUp=PriceLvl - (iATR(NULL,0,ATRPeriod,i) * Factor);
      CurrDn=PriceLvl + (iATR(NULL,0,ATRPeriod,i) * Factor);

      if (Dir == 1) {
         if (CurrUp > LvlUp) {
            ind_buffer1[i] = CurrUp;
            LvlUp = CurrUp;
         }
         else {
            ind_buffer1[i] = LvlUp;
         }
         ind_buffer2[i] = EMPTY_VALUE;
         if (Low[i] < ind_buffer1[i]) {
            Dir = -1;
            LvlDn = 1000;
         }
      }
      
      if (Dir == -1) {
         if (CurrDn < LvlDn) {
            ind_buffer2[i] = CurrDn;
            LvlDn = CurrDn;
         }
         else {
            ind_buffer2[i] = LvlDn;
         }
         ind_buffer1[i] = EMPTY_VALUE;
         if (High[i] > ind_buffer2[i]) {
            Dir = 1;
            LvlUp = 0;
         }
      }
 
   }  
   

//---- done
   return(0);
  }