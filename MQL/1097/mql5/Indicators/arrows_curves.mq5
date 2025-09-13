//+------------------------------------------------------------------+
//|                                                Arrows&Curves.mq5 |
//|                  Copyright © 2007, Victor G. Lukashuk aka lukas1 |
//|                                                    lukas1@ngs.ru |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Copyright © 2007, Victor G. Lukashuk aka lukas1"
//---- link to the website of the author
#property link      "lukas1@ngs.ru"
//---- indicator version number
#property version   "1.00"
//---- drawing the indicator in the main window
#property indicator_chart_window 
//----eight buffers are used for calculation of drawing of the indicator
#property indicator_buffers 8
//---- Only 8 graphical plots are used
#property indicator_plots   8
//+----------------------------------------------+
//|  Parameters of drawing the bearish indicator |
//+----------------------------------------------+
//---- drawing the indicator 1 as a symbol
#property indicator_type1   DRAW_ARROW
//---- magenta color is used as the color of the bearish indicator line
#property indicator_color1  Magenta
//---- thickness of line of the indicator 1 is equal to 4
#property indicator_width1  4
//---- displaying the bearish label of the indicator line
#property indicator_label1  "Sell"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 2 as a line
#property indicator_type2   DRAW_ARROW
//---- lime color is used as the color of the indicator bullish line
#property indicator_color2  Lime
//---- thickness of the indicator line 2 is equal to 4
#property indicator_width2  4
//---- displaying of the bullish label of the indicator
#property indicator_label2 "Buy"
//+----------------------------------------------+
//|  Parameters of drawing the bearish indicator |
//+----------------------------------------------+
//---- drawing the indicator 3 as a symbol
#property indicator_type3   DRAW_ARROW
//---- magenta color is used as the color of the bearish indicator line
#property indicator_color3  Magenta
//---- the indicator 3 line width is equal to 4
#property indicator_width3  4
//---- displaying the bearish label of the indicator line
#property indicator_label3  "SellStop"
//+----------------------------------------------+
//|  Bullish indicator drawing parameters        |
//+----------------------------------------------+
//---- drawing the indicator 4 as a symbol
#property indicator_type4   DRAW_ARROW
//---- lime color is used as the color of the indicator bullish line
#property indicator_color4  Lime
//---- thickness of the indicator 4 line is equal to 4
#property indicator_width4  4
//---- displaying of the bullish label of the indicator
#property indicator_label4 "BuyStop"
//+--------------------------------------------+
//|  Indicator levels drawing parameters       |
//+--------------------------------------------+
//---- drawing the levels as lines
#property indicator_type5   DRAW_LINE
#property indicator_type6   DRAW_LINE
#property indicator_type7   DRAW_LINE
#property indicator_type8   DRAW_LINE
//---- the four colors are used for the levels
#property indicator_color5  Orange
#property indicator_color6  MediumSeaGreen
#property indicator_color7  MediumSeaGreen
#property indicator_color8  Orange
//---- Bollinger Bands are dott-dash curves
#property indicator_style5 STYLE_DASHDOTDOT
#property indicator_style6 STYLE_DASHDOTDOT
#property indicator_style7 STYLE_DASHDOTDOT
#property indicator_style8 STYLE_DASHDOTDOT
//---- Bollinger Bands width is equal to 1
#property indicator_width5  1
#property indicator_width6  1
#property indicator_width7  1
#property indicator_width8  1
//---- display the labels of Bollinger Bands levels
#property indicator_label5  "BUY from here"
#property indicator_label6  "BuyStop"
#property indicator_label7  "SellStop"
#property indicator_label8  "SELL from here"

//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input int SSP     = 20;   //period of linear turn of the indicator
input int Channel = 0;    //decrease of the channel range. Must to be. in the range of 0-50
input int Ch_Stop = 30;   //decrease of the stoping channel (added to the main)
input int relay   = 10;   //lines shift back in history to 4 bar 
//+----------------------------------------------+

//---- declaration of dynamic arrays that further
//---- will be used as indicator buffers
double BuyBuffer[];
double SellBuffer[];
double HBuffer[];
double LBuffer[];
double HSBuffer[];
double LSBuffer[];
double BuyStopBuffer[],SellStopBuffer[];
//---
int StartBars;
bool uptrend_,old_,uptrend2_,old2_;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- initialization of global variables 
   StartBars=SSP+1+relay;

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- create a label to display in DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"Sell");
//---- indicator symbol
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SellBuffer,true);
//---- restriction to draw empty values for the indicator
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//--- create a label to display in DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"Buy");
//---- indicator symbol
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(BuyBuffer,true);
//---- restriction to draw empty values for the indicator
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(2,SellStopBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,StartBars);
//--- create a label to display in DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"SellStop");
//---- indicator symbol
   PlotIndexSetInteger(2,PLOT_ARROW,251);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SellStopBuffer,true);
//---- restriction to draw empty values for the indicator
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(3,BuyStopBuffer,INDICATOR_DATA);
//---- shifting the start of drawing of the indicator 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,StartBars);
//--- create a label to display in DataWindow
   PlotIndexSetString(3,PLOT_LABEL,"BuyStop");
//---- indicator symbol
   PlotIndexSetInteger(3,PLOT_ARROW,251);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(BuyStopBuffer,true);
//---- restriction to draw empty values for the indicator
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);

//---- set dynamic arrays as indicator buffers
   SetIndexBuffer(4,HBuffer,INDICATOR_DATA);
   SetIndexBuffer(5,HSBuffer,INDICATOR_DATA);
   SetIndexBuffer(6,LSBuffer,INDICATOR_DATA);
   SetIndexBuffer(7,LBuffer,INDICATOR_DATA);
//---- set the position, from which the Bollinger Bands drawing starts
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,StartBars);
   PlotIndexSetInteger(5,PLOT_DRAW_BEGIN,StartBars);
   PlotIndexSetInteger(6,PLOT_DRAW_BEGIN,StartBars);
   PlotIndexSetInteger(7,PLOT_DRAW_BEGIN,StartBars);
//--- create labels to display in Data Window
   PlotIndexSetString(4,PLOT_LABEL,"BUY from here");
   PlotIndexSetString(5,PLOT_LABEL,"BuyStop");
   PlotIndexSetString(6,PLOT_LABEL,"SellStop");
   PlotIndexSetString(7,PLOT_LABEL,"SELL from here");
//---- restriction to draw empty values for the indicator
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,0);
   PlotIndexSetDouble(5,PLOT_EMPTY_VALUE,0);
   PlotIndexSetDouble(6,PLOT_EMPTY_VALUE,0);
   PlotIndexSetDouble(7,PLOT_EMPTY_VALUE,0);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(HBuffer,true);
   ArraySetAsSeries(HSBuffer,true);
   ArraySetAsSeries(LSBuffer,true);
   ArraySetAsSeries(LBuffer,true);

//---- Setting the format of accuracy of displaying the indicator
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- name for the data window and the label for sub-windows 
   string short_name="Arrows&Curves";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- checking the number of bars to be enough for the calculation
   if(rates_total<StartBars) return(0);

//---- declaration of local variables 
   int limit,bar;
   double High,Low,smin,smax,smin2,smax2,Close;
   static bool uptrend,old,uptrend2,old2;

//--- calculations of the necessary amount of data to be copied and
//the "limit" starting index for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-StartBars; // starting index for calculation of all bars
     }
   else
     {
      limit=rates_total-prev_calculated; // starting index for calculation of new bars
     }

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- restore values of the variables
   uptrend=uptrend_;
   uptrend2=uptrend2_;
   old=old_;
   old2=old2_;

//---- main loop of the indicator calculation
   for(bar=limit; bar>=0; bar--)
     {
      //---- memorize values of the variables before running at the current bar
      if(rates_total!=prev_calculated && bar==0)
        {
         uptrend_=uptrend;
         uptrend2_=uptrend2;
         old_=old;
         old2_=old2;
        }

      Close= close[bar];
      High = high[iHighest(high,SSP,bar+relay)];
      Low  = low [iLowest (low, SSP,bar+relay)];
      smax = High -(Low-High)*Channel/ 100;           // smax below High considering Channel coeff.
      smin = Low+(High-Low)*Channel / 100;            // smin above Low considering Channel coeff.
      smax2= High -(High-Low)*(Channel+Ch_Stop)/ 100; // smax below High considering Chan+Ch_Stop coeff.
      smin2= Low+(High-Low)*(Channel+Ch_Stop) / 100;  // smin above Low considering Channel coeff.
      BuyBuffer[bar]=0;
      SellBuffer[bar]=0;
      BuyStopBuffer[bar]=0;
      SellStopBuffer[bar]=0;
      //----
      if(Close<smin && Close<smax && uptrend2==true && bar!=0) uptrend=false;
      if( Close > smax  && Close > smin   && uptrend2 == false && bar!=0 ) uptrend  = true;
      if((Close > smax2 || Close > smin2) && uptrend  == false && bar!=0 ) uptrend2 = false;
      if((Close<smin2 || Close<smax2) && uptrend==true && bar!=0) uptrend2=true;
      //---- second call does not switch modes "uptrend"
      //---- but used signal on a cross
      if(close[bar]<smin && close[bar]<smax && uptrend2==false && bar!=0)
        {
         SellBuffer[bar]=Low;
         uptrend2=true;
        }
      //---- second call does not switch modes "uptrend"
      //---- but used signal on a cross
      if(Close>smax && Close>smin && uptrend2==true && bar!=0)
        {
         BuyBuffer[bar]=High;
         uptrend2=false;
        }
      //----
      if(uptrend != old && uptrend == false) SellBuffer[bar] = Low;
      if(uptrend != old && uptrend == true ) BuyBuffer[bar] = High;
      //----
      if(uptrend2 != old2 && uptrend2 == true ) BuyStopBuffer[bar] = smax2;
      if(uptrend2 != old2 && uptrend2 == false) SellStopBuffer[bar] = smin2;
      //----
      old=uptrend;
      old2=uptrend2;
      //----
      HBuffer[bar]=smax;
      LBuffer[bar]=smin;
      HSBuffer[bar]=smax2;
      LSBuffer[bar]=smin2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
//|  searching index of the highest bar                              |
//+------------------------------------------------------------------+
int iHighest(const double &array[],// array for searching the maximum element index
             int count,// the number of the array elements (from a current bar to the index descending), 
                                   // along which the searching must be performed.
             int startPos //the initial bar index (shift relative to a current bar), 
                                   // the search for the greatest value begins from
             )
  {
//----
   int index=startPos;

//----checking correctness of the initial index
   if(startPos<0)
     {
      Print("Bad value in the iHighest function, startPos = ",startPos);
      return(0);
     }

   double max=array[startPos];

//---- searching for an index
   for(int i=startPos; i<startPos+count; i++)
     {
      if(array[i]>max)
        {
         index=i;
         max=array[i];
        }
     }
//---- returning of the greatest bar index
   return(index);
  }
//+------------------------------------------------------------------+
//|  searching index of the lowest bar                               |
//+------------------------------------------------------------------+
int iLowest(const double &array[],  // array for searching the minimum element index
            int count,              // the number of the array elements (from a current bar to the index descending), 
                                    // along which the searching must be performed.
            int startPos            // the initial bar index (shift relative to a current bar), 
                                    // the search for the lowest value begins from
            )
  {
//----
   int index=startPos;

//----checking correctness of the initial index
   if(startPos<0)
     {
      Print("Bad value in the iLowest function, startPos = ",startPos);
      return(0);
     }

   double min=array[startPos];

//---- searching for an index
   for(int i=startPos; i<startPos+count; i++)
     {
      if(array[i]<min)
        {
         index=i;
         min=array[i];
        }
     }
//---- returning of the lowest bar index
   return(index);
  }
//+------------------------------------------------------------------+