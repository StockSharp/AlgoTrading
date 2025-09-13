//+------------------------------------------------------------------+
//|                                                        Lucky.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description ""
#property version   "1.00"

#property description "Expert rewritten from MQ4, the author is Serg_ASV (http://www.mql4.com/ru/users/Serg_ASV), link to original - http://codebase.mql4.com/ru/1555"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CSymbolInfo Sym;
CPositionInfo Pos;
   
//--- input parameters
input double      Lots              =  0.1;  /*Lots*/    // Volume of position
input int         Shift             =  30;   /*Shift*/   // Value of the jump of price to open the position
input int         Limit             =  180;  /*Limit*/   // Value of the loss in points to close the position

bool first;
double a,b;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   first=false;

   if(!Sym.Name(_Symbol)){
      Alert("Failed to initialize CSymbolInfo, try again");    
      return(-1);
   }

   Print("Expert initialization was completed");
   
   return(0);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason){

}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

   if(!Sym.RefreshRates()){
      return;  
   }

   if(first){
      a=Sym.Ask(); 
      b=Sym.Bid(); 
      first=false; 
      return;
   } 
   
   if(Pos.Select(_Symbol)){
      if(Pos.Profit()>0){
         Trade.PositionClose(_Symbol,Sym.Spread()*3);
      }
      else{
         if(Pos.PositionType()==POSITION_TYPE_BUY){
            if((Pos.PriceOpen()-Sym.Ask())/Sym.Point()> Limit){
               Trade.PositionClose(_Symbol,Sym.Spread()*3);            
            }
         }
         else if(Pos.PositionType()==POSITION_TYPE_SELL){
            if((Sym.Bid()-Pos.PriceOpen())/Sym.Point()> Limit){
               Trade.PositionClose(_Symbol,Sym.Spread()*3);            
            }
         }
      }
   }
   else{
      if(Sym.Ask()-a>=Shift*Sym.Point()){
         Trade.SetDeviationInPoints(Sym.Spread()*3);
         Trade.Sell(Lots,_Symbol,0,0,0,"");
      } 
      if(b-Sym.Bid()>=Shift*Sym.Point()){
         Trade.SetDeviationInPoints(Sym.Spread()*3);
         Trade.Buy(Lots,_Symbol,0,0,0,"");
      } 
   }      

   a=Sym.Ask(); 
   b=Sym.Bid(); 
}
