//+------------------------------------------------------------------+
//|                                                 ColorStochNR.mq5 | 
//|                                      Copyright © 2010, Svinozavr |
//|                                                                  |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2010, Svinozavr"
//---- author of the indicator
#property link      ""
//---- indicator version number
#property version   "1.00"

#property description "The stochastic oscillator by the noise suppression, created as a color histogram"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window
//---- number of indicator buffers
#property indicator_buffers 5 
//---- only two plots are used
#property indicator_plots   2

//+----------------------------------------------+
//|  declaring constants                         |
//+----------------------------------------------+
#define RESET 0 // The constant for returning the indicator recalculation command to the terminal
//+----------------------------------------------+
//|  Indicator 1 drawing parameters              |
//+----------------------------------------------+
//---- drawing the indicator as a color histogram
#property indicator_type1   DRAW_COLOR_HISTOGRAM2
//---- the following colors are used in the histogram
#property indicator_color1 clrGray,clrLightSeaGreen,clrDodgerBlue,clrDeepPink,clrMagenta
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- the width of indicator line is 3
#property indicator_width1  3
//---- displaying the indicator label
#property indicator_label1  "Main"

//+----------------------------------------------+
//|  Indicator 2 drawing parameters              |
//+----------------------------------------------+
//---- drawing indicator as a three-colored line
#property indicator_type2   DRAW_COLOR_LINE
//---- the following colors are used for the indicator line
#property indicator_color2 clrGray,clrLime,clrMagenta
//---- the indicator line is a stroke
#property indicator_style2  STYLE_DASH
//---- Indicator line width is equal to 2
#property indicator_width2  2
//---- displaying the indicator label
#property indicator_label2  "Signal"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1  70.0
#property indicator_level2  50.0
#property indicator_level3  30.0
#property indicator_levelcolor Violet
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  Declaration of enumeration                  |
//+----------------------------------------------+
enum ENUM_MA_METHOD_
  {
   MODE_SMA_,       // Simple averaging
   MODE_EMA_        // Exponential averaging
  };
//+----------------------------------------------+
//|  INDICATOR INPUT PARAMETERS                  |
//+----------------------------------------------+
input uint Kperiod=5;  // K period
input uint Dperiod=3;  // D period
input uint Slowing=3;  // Slowing
input ENUM_MA_METHOD_ Dmethod=MODE_SMA_; // smoothing type
input ENUM_STO_PRICE PriceFild=STO_LOWHIGH; // stochastic calculation method
input uint Sens=0; // sensitivity in points
input int Shift=0; // horizontal shift of the indicator in bars
//+----------------------------------------------+

//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double UpSTOH[],DnSTOH[],SIGN[];
double ColorSTOH[],ColorSIGN[];
//---- Declaration of integer variables of data starting point
int min_rates_total;
//----
double sens; // sensitivity in prices
double kd; // coeff. EMA for the signal line
//+------------------------------------------------------------------+   
//| STOH indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=int(Kperiod+Dperiod+Slowing+1);

//---- Initialization of variables  
   sens=Sens*_Point; // sensitivity in prices
   if(Dmethod==MODE_EMA_) kd=2.0/(1+Dperiod); // κξύττ. EMA for the signal line

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,UpSTOH,INDICATOR_DATA);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(UpSTOH,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,DnSTOH,INDICATOR_DATA);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(DnSTOH,true);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(2,ColorSTOH,INDICATOR_COLOR_INDEX);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(ColorSTOH,true);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(3,SIGN,INDICATOR_DATA);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(SIGN,true);

//---- setting dynamic array as a color index buffer   
   SetIndexBuffer(4,ColorSIGN,INDICATOR_COLOR_INDEX);
//---- indexing elements in the buffer as time series
   ArraySetAsSeries(ColorSIGN,true);

//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- creating name for displaying if separate sub-window and in tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,"StochNR("+(string)Kperiod+","+(string)Dperiod+","+(string)Slowing+")");

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| STOH iteration function                                          | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- checking the number of bars to be enough for calculation
   if(rates_total<min_rates_total) return(RESET);

//---- Declaration of integer variables
   int limit,bar;
//---- declaration of variables with a floating point  
   double Main,prevMain;

//---- calculations of the necessary amount of data to be copied and
//the starting number limit for the bar recalculation loop
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total-1; // starting index for the calculation of all bars
      SIGN[limit+1]=50;
      for(bar=rates_total-1; bar>limit && !IsStopped(); bar--)
        {
         UpSTOH[bar]=50;
         DnSTOH[bar]=50;
        }
     }
   else limit=rates_total-prev_calculated; // starting index for the calculation of new bars 

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- Main calculation loop of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- main line
      Main=Stoch(Kperiod,Slowing,PriceFild,sens,high,low,close,bar);

      if(Main<50)
        {
         DnSTOH[bar]=Main;
         UpSTOH[bar]=50;
        }
      else
        {
         UpSTOH[bar]=Main;
         DnSTOH[bar]=50;
        }

      //---- signal line
      switch(Dmethod)
        {
         case MODE_EMA_: /* EMA */  SIGN[bar]=kd*Main+(1-kd)*SIGN[bar+1]; break;
         case MODE_SMA_: /* SMA */
           {
            int sh=int(bar+Dperiod);
            double OldMain=UpSTOH[sh]+DnSTOH[sh]-50;
            double sum=SIGN[bar+1]*Dperiod-OldMain;
            SIGN[bar]=(sum+Main)/Dperiod;
           }
        }
     }

   if(!prev_calculated) limit--;

//---- main loop of the Stoh indicator coloring
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ColorSTOH[bar]=0;
      Main=UpSTOH[bar]+DnSTOH[bar]-50;
      prevMain=UpSTOH[bar+1]+DnSTOH[bar+1]-50;

      if(Main>50)
        {
         if(Main>prevMain) ColorSTOH[bar]=1;
         if(Main<prevMain) ColorSTOH[bar]=2;
        }

      if(Main<50)
        {
         if(Main<prevMain) ColorSTOH[bar]=3;
         if(Main>prevMain) ColorSTOH[bar]=4;
        }
     }

//---- Main loop of the signal line coloring
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ColorSIGN[bar]=0;
      Main=UpSTOH[bar]+DnSTOH[bar]-50;

      if(Main>SIGN[bar]) ColorSIGN[bar]=1;
      if(Main<SIGN[bar]) ColorSIGN[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+ 
//| STOH iteration function                                          | 
//+------------------------------------------------------------------+ 
double Stoch(
             int Kperiod_, // %K
             int Slowing_, // slowing
             ENUM_STO_PRICE PriceFild_,// type of price
             double Sens_,// sensitivity in prices
             const double &High[],
             const double &Low[],
             const double &Close[],
             int index) // shift
//+------------------------------------------------------------------+ 
  {
//----
   double delta,sens2,s0;

//---- price extremums 
   double max=0.0;
   double min=0.0;
   double closesum=0.0;

   for(int j=index; j<index+Slowing_; j++)
     {
      if(PriceFild_==STO_CLOSECLOSE) // by Close
        {
         max+=Close[ArrayMaximum(Close,j,Kperiod_)];
         min+=Close[ArrayMinimum(Close,j,Kperiod_)];
        }
      else // by High/Low
        {
         max+=High[ArrayMaximum(High,j,Kperiod_)];
         min+=Low[ArrayMinimum(Low,j,Kperiod_)];
        }

      closesum+=Close[j];
     }

   delta=max-min;

   if(delta<Sens_)
     {
      sens2=Sens_/2;
      max+=sens2;
      min-=sens2;
     }

   delta=max-min;

   if(delta) s0=(closesum-min)/delta;
   else s0=1.0;
//----
   return(100*s0);
  }
//+------------------------------------------------------------------+
