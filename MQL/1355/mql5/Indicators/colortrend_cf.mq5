//+------------------------------------------------------------------+
//|                                                ColorTrend_CF.mq5 |
//|                                         CF = Continuation Factor |
//|               Converted by and Copyright: Ronald Verwer/ROVERCOM |
//|                                                         27/04/06 |
//+------------------------------------------------------------------+
//---- author of the indicator
#property copyright "Converted by and Copyright: Ronald Verwer/ROVERCOM"
#property link ""
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- two buffers are used for the indicator calculation and drawing
#property indicator_buffers 2
//---- 1 plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Filling drawing parameters       |
//+-----------------------------------+
//---- drawing indicator as a filling between two lines
#property indicator_type1   DRAW_FILLING
//---- MediumSeaGreen and DeepPi colors are used as the indicator filling colors
#property indicator_color1  MediumSeaGreen, DeepPink
//---- displaying the indicator label
#property indicator_label1 "Trend_CF"
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input int Period_=30;
//+----------------------------------------------+
//---- declaration of dynamic arrays that will further be 
// used as indicator buffers
double UpperBuffer[];
double LowerBuffer[];

int Count[];
//---- Declaration of integer variables of data starting point
int StartBar;
//---- Declare variables arrays for ring buffers
double x_p[],x_n[],y_p[],y_n[];
//+------------------------------------------------------------------+
//|  recalculation of position of the newest element in the ring     |
//|  buffer                                                          |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos
(
 int &CoArr[]// Return the current value of the price series by the link
 )
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----
   int numb;
   static int count=1;
   count--;
   if(count<0) count=Period_-1;

   for(int iii=0; iii<Period_; iii++)
     {
      numb=iii+count;
      if(numb>Period_-1) numb-=Period_;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- initialization of constants
   StartBar=Period_;
//---- memory allocation for arrays of variables   
   ArrayResize(x_p,Period_);
   ArrayResize(x_n,Period_);
   ArrayResize(y_p,Period_);
   ArrayResize(y_n,Period_);
   ArrayResize(Count,Period_);
//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,UpperBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBar);

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(1,LowerBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBar);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"Trend_CF(",Period_,")");
//---- creating name for displaying if separate sub-window and in tooltip
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- determine the accuracy of displaying indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const int begin,          // number of beginning of reliable counting of bars
                const double &price[]     // price array for calculation of the indicator
                )
  {
//---- checking the number of bars to be enough for calculation
   if(rates_total<StartBar+begin) return(0);

//---- declaration of local variables 
   int first,bar,bar0,bar1,barq;
   double chp,chn,cffp,cffn,dprice;

//---- calculation of the starting number first for the bar recalculation loop
   if(prev_calculated==0) // checking for the first start of the indicator calculation
        first=1+begin; // starting number for calculation of all bars
   else first=prev_calculated-1; // starting number for calculation of new bars

//---- main indicator calculation loop
   for(bar=first; bar<rates_total; bar++)
     {
      dprice=price[bar]-price[bar-1];

      bar0=Count[0];
      bar1=Count[1];

      if(dprice>0)
        {
         x_p[bar0]=dprice;
         y_p[bar0]=x_p[bar0]+y_p[bar1];
         x_n[bar0]=0;
         y_n[bar0]=0;
        }
      else
        {
         x_n[bar0]=-dprice;
         y_n[bar0]=x_n[bar0]+y_n[bar1];
         x_p[bar0]=0;
         y_p[bar0]=0;
        }

      if(bar<StartBar+begin)
       {
        if(bar<rates_total-1) Recount_ArrayZeroPos(Count);
        continue;
       }
      
      chp=0;
      chn=0;
      cffp=0;
      cffn=0;
      
      for(int q=Period_-1; q>=0; q--)
        {
         barq=Count[q];

         chp+=x_p[barq];
         chn+=x_n[barq];
         cffp+=y_p[barq];
         cffn+=y_n[barq];
        }

      UpperBuffer[bar]=(chp-cffn)/_Point;
      LowerBuffer[bar]=(chn-cffp)/_Point;
      //----
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count);
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
