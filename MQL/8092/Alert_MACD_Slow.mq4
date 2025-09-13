//+------------------------------------------------------------------+ 
//|                                                  MACD Sample.mq4 | 
//|                      Copyright © 2005, MetaQuotes Software Corp. | 
//|                                       http://www.metaquotes.net/ | 
//+------------------------------------------------------------------+ 

extern double TakeProfit = 30; 
extern double StopLoss  =  20; 
extern double Lots = 0.1; 
extern double TrailingStop = 30; 
extern double MACDOpenLevel=3; 
extern double MACDCloseLevel=2; 
extern double MATrendPeriod=26; 

//mia aggiunta 
int NumOrder ; 

//+------------------------------------------------------------------+ 
//|                                                                  | 
//+------------------------------------------------------------------+ 
int start() 
  { 
   double MacdCurrent, MacdPrevious; 
   double Macd_1, Macd_2, Macd_3, Macd_4, Ma_Quick, Ma_Slow; 
   double MaCurrent, MaPrevious; 
   double top, bottom;
   int cnt, ticket, total;
    
// initial data checks 
// it is important to make sure that the expert works with a normal 
// chart and the user did not make any mistakes setting external 
// variables (Lots, StopLoss, TakeProfit, 
// TrailingStop) in our case, we check TakeProfit 
// on a chart of less than 100 bars 
   if(Bars<100) 
     { 
      Print("bars less than 100"); 
      return(0);  
     } 
     
     if( isNewBar()){  NumOrder = 0;} 
      
   if(TakeProfit<10) 
     { 
      Print("TakeProfit less than 10"); 
      return(0);  // check TakeProfit 
     } 
// to simplify the coding and speed up access 
// data are put into internal variables 
   Macd_1 = iMACD(NULL,0,3,20,9,PRICE_CLOSE,MODE_MAIN,1); 
   Macd_2 = iMACD(NULL,0,3,20,9,PRICE_CLOSE,MODE_MAIN,2); 
   Macd_3 = iMACD(NULL,0,3,20,9,PRICE_CLOSE,MODE_MAIN,3); 
   Macd_4 = iMACD(NULL,0,3,20,9,PRICE_CLOSE,MODE_MAIN,4); 
   Ma_Quick = iMA(NULL,0,20,0,MODE_EMA,PRICE_CLOSE,0); 
   Ma_Slow  = iMA(NULL,0,65,0,MODE_EMA,PRICE_CLOSE,0); 
   top      = High[iHighest(NULL,0,MODE_HIGH,10,1)];
   bottom   = Low[iLowest(NULL,0,MODE_LOW,10,1)];


   total=OrdersTotal(); 
   //if(total<1) 
  //   { 
      // no opened orders identified 
      if(AccountFreeMargin()<(1000*Lots)) 
        { 
         Print("We have no money. Free Margin = ", AccountFreeMargin()); 
         return(0);  
        } 
        
        
      // check for long position (BUY) possibility 
      if(Macd_1 > Macd_2 && Macd_2 < Macd_3 && Ma_Quick > Ma_Slow && Ask > High[1] && Macd_2 < 0
         || 
         Macd_2 > Macd_3 && Macd_3 < Macd_4 && Ma_Quick > Ma_Slow && Ask > High[2] && Macd_3 < 0 ) 
        { 
        
       if( NumOrder == 0){ 
        
        Alert("SET UP LONG");
           } 
         
        } 
        
        
       
      // check for short position (SELL) possibility 
      if(Macd_1 < Macd_2 && Macd_2 > Macd_3 && Ma_Quick < Ma_Slow && Bid < Low[1] && Macd_2 > 0
         || 
         Macd_2 < Macd_3 && Macd_3 > Macd_4 && Ma_Quick < Ma_Slow && Bid < Low[2] && Macd_3 > 0  )
        { 
        
        
        
        if( NumOrder == 0){ 
        
         Alert("SET UP SHORT_VALUE");
        
        
           } 
        
        } 
      return(0); 
      } 
    
   
// the end. 











bool isNewBar() 
{ 
   static datetime lastbar=0; 
   datetime curbar = Time[0]; 
   if(lastbar!=curbar) 
   { 

      lastbar=curbar; 
      return (true); 
   } 
   else 
   { 
      return(false); 
   } 
} 