//+------------------------------------------------------------------+
//|                                            MoveStoploss4Taah.mq5 |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023,WamekEA."
#property link      "eawamek@gmail.com"
#property version   "1.2"
#property description "<<<< Moves the stoploss along with ask/bid at a predetermined distance >>>>>"

#include <Trade/Trade.mqh>

//create instance of the trade
CTrade trade;

input bool AutoTrail= true;
input int Distance2Trail=300;


double When2Trail;
string mConinfo; 
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
 TesterHideIndicators(true);
 
    if( !TerminalInfoInteger(TERMINAL_TRADE_ALLOWED) ){ Alert("Please enable Auto trading"); return(0);}
    
    else  {
      if(!MQLInfoInteger(MQL_TRADE_ALLOWED)){
         Alert("Automated trading is forbidden in the program settings for ",__FILE__); return(0);}
     }
    
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   ObjectDelete(0,"WAMEK1");

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
  AtrMaxMin();
  
   MoveStopLoss();
   
 StringConcatenate(mConinfo,"Distance2MoveStopLoss: ",DoubleToString(When2Trail/Point(),0)+"pips");
 DrawLABEL(0,"WAMEK1",mConinfo, 12*50,40,DodgerBlue);
 
  }
//+------------------------------------------------------------------+


void MoveStopLoss(){

 if(AutoTrail==true) When2Trail =NormalizeDouble( 0.85*AtrMax,Digits());
   else When2Trail=NormalizeDouble( Distance2Trail*Point(),Digits());


for(int t=PositionsTotal()-1;t>=0;t--) 
  if(PositionSelectByTicket(PositionGetTicket(t)) ) 
     if(PositionGetString( POSITION_SYMBOL)==Symbol()){ 
    
   // Record open price,stoploss, and takeprofit 
   double opn = PositionGetDouble(POSITION_PRICE_OPEN),
          stl = PositionGetDouble(POSITION_SL),
          tp = PositionGetDouble(POSITION_TP);
     
       if( PositionGetInteger(POSITION_TYPE)== ORDER_TYPE_BUY) {
               double bid=SymbolInfoDouble(Symbol(),SYMBOL_BID);
                  if(bid>opn && stl<bid-When2Trail)
                       trade.PositionModify(PositionGetTicket(t),bid-When2Trail,tp);
          }
           
       

         if( PositionGetInteger(POSITION_TYPE)== ORDER_TYPE_SELL) {
         double ask=SymbolInfoDouble(Symbol(),SYMBOL_ASK);
             if(ask<opn && (stl>ask+When2Trail || stl==0 ))
                  trade.PositionModify(PositionGetTicket(t),ask+When2Trail,tp);
         }
       }
}



//---AtrMaxMin--

double AtrMin,AtrMax;

void AtrMaxMin(){

double AtrArray[], Atr;
ArraySetAsSeries(AtrArray,true);
 //Compute the ATR values
 double AtrGet= iATR(Symbol(),PERIOD_CURRENT,7);

 AtrMin =876532009;
 AtrMax =-876532009;
 
 for(int a=1;a<=30;a++){
 
 //Gets data of the stored ATR values in quantity required and store in AtrArray
  CopyBuffer(AtrGet,0,a,1,AtrArray);

 //Retrive the quantity required and stored in Atr
  Atr =  AtrArray[0];
  
 AtrMax = MathMax(AtrMax,Atr);
 AtrMin = MathMin(AtrMin,Atr);

 }
 
}




void DrawLABEL(long Id, string ObjName, string Info, float X, float Y, color clr)
{
if(ObjectFind(Id,ObjName)==-1){
      ObjectCreate(Id, ObjName,OBJ_LABEL,0,0,0);
      ObjectSetInteger(Id,ObjName,OBJPROP_CORNER,0);
      ObjectSetInteger(Id,ObjName,OBJPROP_XDISTANCE,X);
      ObjectSetInteger(Id,ObjName,OBJPROP_YDISTANCE,Y);
      ObjectSetInteger(Id,ObjName,OBJPROP_FONTSIZE,10);
      ObjectSetInteger(Id,ObjName,OBJPROP_COLOR,clr);
      }

   ObjectSetString(Id,ObjName,OBJPROP_TEXT,Info); 

}