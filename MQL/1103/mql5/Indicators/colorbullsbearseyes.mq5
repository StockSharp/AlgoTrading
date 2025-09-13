//+------------------------------------------------------------------+
//|                                          ColorBullsBearsEyes.mq5 | 
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
#property indicator_buffers 3 
//---- only one plot is used
#property indicator_plots   1
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level5 0.0
#property indicator_levelcolor LimeGreen
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  Indicator drawing parameters                |
//+----------------------------------------------+
//---- drawing indicator as a three-colored line
#property indicator_type1 DRAW_COLOR_LINE
//---- the following colors are used in a three-colored line
#property indicator_color1 clrBlue,clrRed,clrLime
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- Indicator line width is equal to 2
#property indicator_width1  2
//---- displaying the indicator label
#property indicator_label1  "BullsBearsEyes"
//+----------------------------------------------+
//|  Declaration of constants                    |
//+----------------------------------------------+
#define RESET 0 // the constant for getting the command for the indicator recalculation back to the terminal
//+----------------------------------------------+
//|  Declaration of enumerations                 |
//+----------------------------------------------+
enum AlgMode
  {
   twist,//changing direction
   breakdown1,  //breakdown of the middle line
   breakdown2,  //breakdown of the overbought and oversold levels
   breakdown3   //exit of the overbought and oversold + breakdown of the overbought and oversold levels
  };
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input int period=13; //period of the indicator averaging
input double gamma=0.6; //indicator smoothing ratio
input AlgMode Mode=breakdown2; //algorithm for calculating the input signal
input uint HighLevel=75; //overbought level
input uint MiddleLevel=50; //middle level
input uint LowLevel=25; //oversold level
input int Shift=0;   //horizontal shift of the indicator in bars
//+----------------------------------------------+

//---- declaration of dynamic arrays that further 
//---- will be used as indicator buffers
double BullsBearsEyes[],ColorBuffer[],SignBuffer[];

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
   min_rates_total=int(period+4);

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

//---- set dynamic array as a color index buffer   
   SetIndexBuffer(1,ColorBuffer,INDICATOR_COLOR_INDEX);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(ColorBuffer,true);

//---- set dynamic array as a buffer of data storing   
   SetIndexBuffer(2,SignBuffer,INDICATOR_CALCULATIONS);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(SignBuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   StringConcatenate(shortname,"BullsBearsEyes(",period,", ",gamma,", ",Shift,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);

//---- the number of the indicator 5 horizontal levels   
   IndicatorSetInteger(INDICATOR_LEVELS,5);
//---- values of the indicator horizontal levels
   IndicatorSetDouble(INDICATOR_LEVELVALUE,0,100);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,1,HighLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,2,MiddleLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,3,LowLevel);
   IndicatorSetDouble(INDICATOR_LEVELVALUE,4,0);
//---- the following colors are used for horizontal levels lines
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,0,clrLimeGreen);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,1,clrMagenta);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,2,clrGray);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,3,clrMagenta);
   IndicatorSetInteger(INDICATOR_LEVELCOLOR,4,clrLimeGreen);
//---- short dot-dash is used for the horizontal level line
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,0,STYLE_DASH);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,1,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,2,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,3,STYLE_DASHDOTDOT);
   IndicatorSetInteger(INDICATOR_LEVELSTYLE,4,STYLE_DASH);
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
   static int trend_;
//---- Declaration of integer variables
   int limit,bar,to_copy,trend;

//--- calculations of the necessary amount of data to be copied and
//the "limit" starting index for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of the indicator calculation
     {
      limit=int(rates_total-period-1); // starting index for calculation of all bars
      trend_=0;
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
   if(Mode==breakdown3) trend=trend_;

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
      result*=100;

      BullsBearsEyes[bar]=result;
     }

   if(prev_calculated>rates_total || prev_calculated<=0) limit-=min_rates_total;
//---- Main cycle of the indicator coloring
   switch(Mode)
     {
      case twist:
         for(bar=limit; bar>=0 && !IsStopped(); bar--)
           {
            int bar1=bar+1;
            ColorBuffer[bar]=0;
            if(BullsBearsEyes[bar1]>BullsBearsEyes[bar]) ColorBuffer[bar]=1;
            if(BullsBearsEyes[bar1]<BullsBearsEyes[bar]) ColorBuffer[bar]=2;
           }
         break;

      case breakdown1:
         for(bar=limit; bar>=0 && !IsStopped(); bar--)
           {
            ColorBuffer[bar]=0;
            if(BullsBearsEyes[bar]<MiddleLevel) ColorBuffer[bar]=1;
            if(BullsBearsEyes[bar]>MiddleLevel) ColorBuffer[bar]=2;
           }
         break;

      case breakdown2:
         for(bar=limit; bar>=0 && !IsStopped(); bar--)
           {
            ColorBuffer[bar]=0;
            if(BullsBearsEyes[bar]<LowLevel) ColorBuffer[bar]=1;
            if(BullsBearsEyes[bar]>HighLevel) ColorBuffer[bar]=2;
           }
         break;

      case breakdown3:
         for(bar=limit; bar>=0 && !IsStopped(); bar--)
           {
            int bar1=bar+1;
            if(BullsBearsEyes[bar1]>=HighLevel&&BullsBearsEyes[bar]<HighLevel) trend=-1;
            if(BullsBearsEyes[bar1]>=LowLevel&&BullsBearsEyes[bar]<LowLevel) trend=-1;

            if(BullsBearsEyes[bar1]<=LowLevel&&BullsBearsEyes[bar]>LowLevel) trend=+1;
            if(BullsBearsEyes[bar1]<=HighLevel&&BullsBearsEyes[bar]>HighLevel) trend=+1;
            if(bar==1) trend_=trend;

            ColorBuffer[bar]=0;
            if(trend<0) ColorBuffer[bar]=1;
            if(trend>0) ColorBuffer[bar]=2;
           }
         break;
     }

   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      SignBuffer[bar]=0;
      int bar1=bar+1;

      if(SignBuffer[bar1]>0 && ColorBuffer[bar]==1) SignBuffer[bar]=-2;
      else if(SignBuffer[bar1]<=0 && ColorBuffer[bar]==1 || SignBuffer[bar+1]<0 && !ColorBuffer[bar]) SignBuffer[bar]=-1;

      if(SignBuffer[bar+1]<0 && ColorBuffer[bar]==2) SignBuffer[bar]=+2;
      else if(SignBuffer[bar1]>=0 && ColorBuffer[bar]==2 || SignBuffer[bar1]>0 && !ColorBuffer[bar]) SignBuffer[bar]=+1;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
