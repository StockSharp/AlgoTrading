//+------------------------------------------------------------------+
//|                                               BullsBearsEyes.mq5 | 
//|                   Copyright © 2007, EmeraldKing, transport_david | 
//|                                                                  | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, EmeraldKing, transport_david"
#property link "http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/"
#property description "BullsBearsEyes"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers
#property indicator_buffers 1 
//---- only one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//|  Indicator drawing parameters                |
//+----------------------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- BlueViolet color is used for the indicator line
#property indicator_color1 BlueViolet
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- Indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "BullsBearsEyes"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level2 1.0
#property indicator_level3 0.75
#property indicator_level4 0.50
#property indicator_level5 0.25
#property indicator_level6 0.0
#property indicator_levelcolor LimeGreen
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  Declaration of constants                    |
//+----------------------------------------------+
#define RESET 0 // the constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input int period=13; //period of the indicator averaging
input double gamma=0.6; //indicator smoothing ratio
input int    Shift=0;   //horizontal shift of the indicator in bars
//+----------------------------------------------+

//---- declaration of a dynamic array that further 
// will be used as an indicator buffer
double BullsBearsEyes[];

//---- declaration of integer variables for the indicators handles
int Bears_Handle,Bulls_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+   
//| BullsBearsEyes indicator initialization function                 | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=int(period+1);

//---- getting handle of the iBearsPower indicator
   Bears_Handle=iBearsPower(NULL,0,int(period));
   if(Bears_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iBearsPower indicator");

//---- getting handle of the iBullsPower indicator
   Bulls_Handle=iBullsPower(NULL,0,int(period));
   if(Bulls_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iBullsPower indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,BullsBearsEyes,INDICATOR_DATA);
//---- moving the indicator 1 horizontally
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(BullsBearsEyes,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"BullsBearsEyes(",period,", ",gamma,", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| BullsBearsEyes iteration function                                | 
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
//---- checking the number of bars to be enough for the calculation
   if(BarsCalculated(Bears_Handle)<rates_total
      || BarsCalculated(Bulls_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- declaration of variables with a floating point  
   double result,Bears[],Bulls[],CU,CD;
   double L0,L1,L2,L3,L0A,L1A,L2A,L3A;
   static double L0_,L1_,L2_,L3_;
//---- Declaration of integer variables and getting already calculated bars
   int limit,bar,to_copy;

//--- calculations of the necessary amount of data to be copied and
//the "limit" starting index for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=rates_total-min_rates_total-1; // starting index for calculation of all bars
     }
   else limit=rates_total-prev_calculated; // starting index for calculation of new bars
   to_copy=limit+1;

//---- indexing elements in arrays as time series  
   ArraySetAsSeries(Bears,true);
   ArraySetAsSeries(Bulls,true);

//--- copy newly appeared data in the array  
   if(CopyBuffer(Bears_Handle,0,0,to_copy,Bears)<=0) return(RESET);
   if(CopyBuffer(Bulls_Handle,0,0,to_copy,Bulls)<=0) return(RESET);

//---- restore values of the variables
   L0=L0_;
   L1=L1_;
   L2=L2_;
   L3=L3_;

//---- indicators values recalculation
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //---- memorize values of the variables before running at the current bar
      if(rates_total!=prev_calculated && bar==0)
        {
         L0_=L0;
         L1_=L1;
         L2_=L2;
         L3_=L3;
        }

      L0A=L0;
      L1A=L1;
      L2A=L2;
      L3A=L3;

      L0=(1.0-gamma)*(Bears[bar]+Bulls[bar])+gamma*L0A;
      L1=-gamma*L0+L0A+gamma*L1A;
      L2=-gamma*L1+L1A+gamma*L2A;
      L3=-gamma*L2+L2A+gamma*L3A;
      
      CU=0.0;
      CD=0.0;
      result=0.0;
      if(L0>=L1) CU=L0-L1; else CD=L1-L0;
      if(L1>=L2) CU+=L1-L2; else CD+=L2-L1;
      if(L2>=L3) CU+=L2-L3; else CD+=L3-L2;
      if(CU+CD!=0) result=CU/(CU+CD);
      
      BullsBearsEyes[bar]=result;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
