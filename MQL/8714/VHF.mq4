
 
#property indicator_separate_window
#property indicator_buffers 1
#property indicator_color1 Yellow
 
//---- indicator parameters
extern int VHF_Period=28;
 
//---- indicator buffers
double ExtMapBuffer[];
 
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init()
  {
 
   SetIndexStyle(0,DRAW_LINE,STYLE_SOLID,2);
   SetIndexBuffer(0,ExtMapBuffer);
 
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   int limit;
   int counted_bars=IndicatorCounted();
   if(counted_bars<0) return(-1);
   if(counted_bars>0) counted_bars-=1;
   limit=Bars-counted_bars;
 
   for(int i=0; i<limit; i++)
   {
      ExtMapBuffer[i] = VHF(VHF_Period,i);
    }
    return(0);
  }
  
  
  double VHF(int period, int bar)
  {
      double HCP = High[Highest(NULL,0,MODE_CLOSE,period,bar)];
      double LCP = Low[Lowest(NULL,0,MODE_CLOSE,period,bar)];
      double Numerator = MathAbs(HCP-LCP);
      double Denominator ;
 
      for(int i=bar; i<bar+period; i++)
          Denominator = Denominator + MathAbs( Close [i] - Close[i+1]) ;
 
       return (Numerator/Denominator);
  }