//+------------------------------------------------------------------+
//|                                                         iVLT.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description ""
#property version   "1.00"

/*
   The indicator for the VLT_TRADER expert.  
   
*/

#property indicator_separate_window
#property indicator_buffers 3
#property indicator_plots   3
//--- plot VOL
#property indicator_label1  "VOL"
#property indicator_type1   DRAW_LINE
#property indicator_color1  clrGreen
#property indicator_style1  STYLE_SOLID
#property indicator_width1  1
//--- plot MAX_VOL
#property indicator_label2  "MIN_VOL"
#property indicator_type2   DRAW_LINE
#property indicator_color2  clrBlue
#property indicator_style2  STYLE_SOLID
#property indicator_width2  1
//--- plot Label4
#property indicator_label3  "Signal"
#property indicator_type3   DRAW_ARROW
#property indicator_color3  clrDarkOrange
#property indicator_style3  STYLE_SOLID
#property indicator_width3  1

input int period=10;

//--- indicator buffers
double         VOLBuffer[];
double         MAX_VOLBuffer[];
double         Label4Buffer[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- indicator buffers mapping
   SetIndexBuffer(0,VOLBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,MAX_VOLBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,Label4Buffer,INDICATOR_DATA);
//--- setting a code from the Wingdings charset as the property of PLOT_ARROW
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate (const int rates_total,
                 const int prev_calculated,
                 const datetime & time[],
                 const double & open[],
                 const double & high[],
                 const double & low[],
                 const double & close[],
                 const long & tick_volume[],
                 const long & volume[],
                 const int & spread[]
               ){
   static bool error=true;
   int start;
   int start1;
   int start2;   
      if(prev_calculated==0){
         error=true;
      }
      if(error){
         start=0;
         start1=start+period+1;
         start2=start1+1;
         error=false;
      }
      else{
         start=prev_calculated-1;
         start1=start;
         start2=start1;
      }
      for(int i=start;i<rates_total;i++){
         VOLBuffer[i]=high[i]-low[i];
      }
      for(int i=start1;i<rates_total;i++){
         MAX_VOLBuffer[i]=VOLBuffer[ArrayMinimum(VOLBuffer,i-period,period)];
      }      
      for(int i=start2;i<rates_total;i++){
         Label4Buffer[i]=EMPTY_VALUE;
            if(VOLBuffer[i]<MAX_VOLBuffer[i] && !(VOLBuffer[i-1]<MAX_VOLBuffer[i-1])){
               Label4Buffer[i]=MAX_VOLBuffer[i];
            }
      }
   return(rates_total);               
}

