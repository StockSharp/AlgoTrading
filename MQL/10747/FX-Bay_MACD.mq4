//+---------------------------------------------------------------------------+
//|                                                 Custom MACD-Histogram.mq4 |
//|                      Copyright © 2010, FXBay - Vietnamese FX Trading Team |
//|                                                      http://www.fxbay.net |
//+---------------------------------------------------------------------------+
#property  copyright "Copyright © 2010, FXBay - Vietnamese FX Trading Team"
#property  link      "http://www.fxbay.net/"

//---- indicator settings
#property  indicator_separate_window
#property  indicator_buffers 3
#property  indicator_color1  DarkTurquoise
#property  indicator_color2  Red
#property  indicator_color3  Gray
#property  indicator_width1  1

//---- indicator parameters
extern int FastEMA=12;
extern int SlowEMA=26;
extern int SignalEMA=9;

//---- indicator buffers
double     MACD_Buffer[];
double     SIGNAL_Buffer[];
double     HISTOGRAM_Buffer[];

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int init() {
//---- drawing settings
   SetIndexStyle(0,DRAW_LINE);
   SetIndexStyle(1,DRAW_LINE);
   SetIndexStyle(2,DRAW_HISTOGRAM);
   
   SetIndexDrawBegin(1,SignalEMA);
   IndicatorDigits(Digits+1);
   
//---- indicator buffers mapping
   SetIndexBuffer(0,MACD_Buffer);
   SetIndexBuffer(1,SIGNAL_Buffer);
   SetIndexBuffer(2,HISTOGRAM_Buffer);
   
//---- name for DataWindow and indicator subwindow label
   IndicatorShortName("FXBay - MACD ("+FastEMA+","+SlowEMA+","+SignalEMA+")");
   SetIndexLabel(0,"MACD");
   SetIndexLabel(1,"Signal");
   SetIndexLabel(2,"Histogram");
   
//---- initialization done
   return(0);
}

//+------------------------------------------------------------------+
//| Moving Averages Convergence/Divergence                           |
//+------------------------------------------------------------------+
int start() {

	int i;
	
	int limit;
	int counted_bars = IndicatorCounted();
	if(counted_bars < 0) return(-1);   //---- check for possible errors
	if(counted_bars > 0) counted_bars--;   //---- the last counted bar will be recounted
	limit = Bars - counted_bars;

//---- Draw MACD line
	for(i=0; i<limit; i++) {	//-- loop from the current bar to the first bar
		MACD_Buffer[i] = iMA(NULL,0,FastEMA,0,MODE_EMA,PRICE_CLOSE,i) - iMA(NULL,0,SlowEMA,0,MODE_EMA,PRICE_CLOSE,i);
   }

//---- Draw Signal line and Histogram
	for(i=0; i<limit; i++) {	//-- same loop above
		SIGNAL_Buffer[i] = iMAOnArray(MACD_Buffer,Bars,SignalEMA,0,MODE_EMA,i);
		HISTOGRAM_Buffer[i] = MACD_Buffer[i] - SIGNAL_Buffer[i];
	}
	
//---- Done
   return(0);
}

//+-----------------------------------------------------------------+