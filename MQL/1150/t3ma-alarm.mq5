//+------------------------------------------------------------------+
//|                                                   T3MA-ALARM.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property version   "1.00"
#property description "Rewritten from MQL4. Link to original - http://codebase.mql4.com/ru/7589, author is Martingeil (http://www.mql4.com/ru/users/Martingeil)"

/*

   The author: http://www.mql4.com/ru/users/Martingeil
   The original: http://codebase.mql4.com/ru/7589
   
   How it works: The moving average is calculated, and then it is smoothed again. Arrows show the positions, where the smoothed moving average change its direction.
   
*/

#property indicator_chart_window
#property indicator_buffers 4
#property indicator_plots   3
//--- plot Label1
#property indicator_label1  "MA"
#property indicator_type1   DRAW_LINE
#property indicator_color1  clrNONE
#property indicator_style1  STYLE_SOLID
#property indicator_width1  1
//--- plot Label2
#property indicator_label2  "Buy"
#property indicator_type2   DRAW_ARROW
#property indicator_color2  clrDodgerBlue
#property indicator_style2  STYLE_SOLID
#property indicator_width2  1
//--- plot Label3
#property indicator_label3  "Sell"
#property indicator_type3   DRAW_ARROW
#property indicator_color3  clrRed
#property indicator_style3  STYLE_SOLID
#property indicator_width3  1
//--- input parameters
input int                              MAPeriod       =  4;	            /*MAPeriod*/   // MA period
input int                              MAShift        =  0;	            /*MAShift*/    // MA shift
input ENUM_MA_METHOD                   MAMethod       =  MODE_EMA;	   /*MAMethod*/   // MA method
input ENUM_APPLIED_PRICE               MAPrice        =  PRICE_CLOSE;	/*MAPrice*/    // MA price


int MAHandle1=INVALID_HANDLE;
int MAHandle2=INVALID_HANDLE;


//--- indicator buffers
double         BufMA[];
double         BufBuy[];
double         BufSell[];
double         BufDir[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit(){
   MAHandle1=iMA(NULL,PERIOD_CURRENT,MAPeriod,0,MAMethod,MAPrice);
      if(MAHandle1!=INVALID_HANDLE){
         MAHandle2=iMA(NULL,PERIOD_CURRENT,MAPeriod,0,MAMethod,MAHandle1);
      }         
   
      if(MAHandle2==INVALID_HANDLE){
         Alert("Failed to loading the indicator, try again");
         return(-1);
      }
//--- indicator buffers mapping
   SetIndexBuffer(0,BufMA,INDICATOR_DATA);
   SetIndexBuffer(1,BufBuy,INDICATOR_DATA);
   SetIndexBuffer(2,BufSell,INDICATOR_DATA);
   SetIndexBuffer(3,BufDir,INDICATOR_CALCULATIONS);
//--- setting a code from the Wingdings charset as the property of PLOT_ARROW
   PlotIndexSetInteger(1,PLOT_ARROW,233);
   PlotIndexSetInteger(2,PLOT_ARROW,234);   
   
   PlotIndexSetInteger(1,PLOT_ARROW_SHIFT,10);
   PlotIndexSetInteger(2,PLOT_ARROW_SHIFT,-10);   


   PlotIndexSetInteger(0,PLOT_SHIFT,MAShift);
   
   
//---
   return(0);
  }
void OnDeinit(const int reason){  
   Comment("");
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
      if(prev_calculated==0){
         error=true;
      }
      if(error){
         start=0;
         start1=start+MAShift+2;
         error=false;
      }
      else{
         start=prev_calculated-1;
         start1=start;
      }
      if(CopyBuffer(MAHandle2,0,0,rates_total-start,BufMA)==-1){
         error=true;
         return(0);
      }
      for(int i=start1;i<rates_total;i++){
         BufBuy[i]=0;
         BufSell[i]=0;
         BufDir[i]=BufDir[i-1];
         if(BufMA[i-MAShift]>BufMA[i-MAShift-1])BufDir[i]=1;
         if(BufMA[i-MAShift]<BufMA[i-MAShift-1])BufDir[i]=-1;
         if(BufDir[i]==1 && BufDir[i-1]==-1)BufBuy[i]=low[i];
         if(BufDir[i]==-1 && BufDir[i-1]==1)BufSell[i]=high[i];
      }
      if(BufDir[rates_total-1]==1){
         Comment("\n SWAPLONG = ",DoubleToString(SymbolInfoDouble(Symbol(),SYMBOL_SWAP_LONG),2),"   SWAPSHORT = ",DoubleToString(SymbolInfoDouble(Symbol(),SYMBOL_SWAP_SHORT),2),"\n BUY TREND ",DoubleToString(close[rates_total-1],_Digits));
      }            
      else if(BufDir[rates_total-1]==-1){
         Comment("\n SWAPLONG = ",DoubleToString(SymbolInfoDouble(Symbol(),SYMBOL_SWAP_LONG),2),"   SWAPSHORT = ",DoubleToString(SymbolInfoDouble(Symbol(),SYMBOL_SWAP_SHORT),2),"\n SELL TREND ",DoubleToString(close[rates_total-1],_Digits));
      }        
   return(rates_total);               
}

