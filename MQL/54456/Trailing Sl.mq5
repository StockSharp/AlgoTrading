//+------------------------------------------------------------------+
//|                                                  Trailing Sl.mq5 |
//|                 Contact owner Whatsapp..Telegram +27 79 345 5275 |
//|                       https://www.mql5.com/en/users/trickster247 |
//+------------------------------------------------------------------+
#property copyright "Contact owner Whatsapp..Telegram +27 79 345 5275"
#property link      "https://www.mql5.com/en/users/trickster247"
#property version   "1.00"

#include <Trade\Trade.mqh>
input double Lots = 0.2;
input double Setloss = 1500;
input double TakeProfit = 5000;
input int TslPoints = 1000; //Trailing Stop Loss (10 points = 1 pip)
input int TslTriggerPoints = 1500; //points in profit to activate Trailing Sl (10 points = 1 pip)
input int InpMagic = 123456; //Magic Number

CTrade trade; // Trade object for order management
COrderInfo ord;
CPositionInfo pos;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
    TrailStop();
  }
//+------------------------------------------------------------------+
void TrailStop(){
    
    double sl = 0;
    double tp = 0;
    double ask = SymbolInfoDouble(_Symbol,SYMBOL_ASK);
    double bid = SymbolInfoDouble(_Symbol,SYMBOL_BID);
    
    for(int i=PositionsTotal()-1; i>=0; i--){
       if(pos.SelectByIndex(i)){
          ulong ticket = pos.Ticket();
          if(pos.Magic()==InpMagic && pos.Symbol()==_Symbol){
             if(pos.PositionType()==POSITION_TYPE_BUY){
               if(bid-pos.PriceOpen()>TslTriggerPoints*_Point){
                  tp = pos.TakeProfit();
                  sl = bid - (TslPoints * _Point);
                  if(sl > pos.StopLoss() && sl!=0){
                     trade.PositionModify(ticket,sl,tp);                     
                 }
              }             
           } else if(pos.PositionType()==POSITION_TYPE_SELL){
                   if(ask+(TslTriggerPoints*_Point)<pos.PriceOpen()){
                      tp = pos.TakeProfit();
                      sl = ask + (TslPoints * _Point);
                      if(sl < pos.StopLoss() && sl!=0){
                         trade.PositionModify(ticket,sl,tp);
                 }  
              }
           }           
        }
     }    
  }
}