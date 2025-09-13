//+------------------------------------------------------------------+
//|                                       ArtificialIntelligence.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description ""
#property version   "1.00"
#property description "Expert rewritten from MQ4, the author is Reshetov (http://www.mql4.com/ru/users/Reshetov), link to original - http://codebase.mql4.com/ru/756"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;

/*

   The author: Reshetov, http://www.mql4.com/ru/users/Reshetov
   
   The original: http://codebase.mql4.com/ru/756

   Brief description: Expert based on the AC indicator and linear perceptron.

   How it works:
   
   AC indicator is used, 4 values of the bars are taken with him: Shift, Shift+7, Shift+14, Shift+21. Each values of the indicator are multiplied by the weight which obtained as (100-x(n)), where x(n) - is optimized variable x1, x2, x2, x3. After multiplying the values are added and we get value at the output of the perceptron. When the perceptron output value is greater than 0 - open the buying , when the value is less than 0 - selling. 
   
   Position is opened with stoploss (stoploss required) and without takeprofit. When reaching the profit position which more at spread than stoplos values, and we have the opposite signal, then performed the coup of position by opening the positionthe with big volume If we don't have a opposite signal, then stoploss is permuted at the same level as it had been at the opening position (by calculations we have profit by the amount of spread). 
   
   If we can not opened a opposite position (because the stoploss value is near), then position will be closed,  and on the next tick will be another attempt to open a position..

   Stoploss is required because, closing of position or moving the stoploss is performed only when the position is in profit,  we also need a way out in case of loss.   


*/
   
//--- input parameters
input double   Lots              =  0.1;     /*Lots*/             // Lot
input int      StopLoss          =  850;     /*StopLoss*/         // Stoploss in points.
input int      Shift             =  1;       /*Shift*/            // Bar on which indicators are checked: 0 - shaped bar, 1 - the first shaped bar
input int      x1                =  135;     /*x1*/               // Weights of perceptron from 0 to 200
input int      x2                =  127;
input int      x3                =  16;
input int      x4                =  93;

int Handle=INVALID_HANDLE;
double a1[1],a2[1],a3[1],a4[1];

datetime ctm[1];
datetime LastTime;
double lot,slv,msl,mtp;
double perc;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   // Loading indicators...
   
   Handle=iAC(_Symbol,PERIOD_CURRENT);

   if(Handle==INVALID_HANDLE){
      Alert("Failed to loading the indicator, try again");
      return(-1);
   }   
   
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
   if(Handle!=INVALID_HANDLE)IndicatorRelease(Handle);
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

   if(CopyTime(_Symbol,PERIOD_CURRENT,0,1,ctm)==-1){
      return;
   }
   
   if(Shift==0 || ctm[0]!=LastTime){
      
         // Indicators
         if(!Indicators()){
            return;
         }   
      
      // Signals
      bool OpenBuy=SignalOpenBuy();
      bool OpenSell=SignalOpenSell();
      
         if(OpenBuy || OpenSell){
            if(!Sym.RefreshRates())return;              
         
         
            if(Pos.Select(_Symbol)){
               switch(Pos.PositionType()){
                  case POSITION_TYPE_BUY:
                     if(Sym.Bid()>(Pos.StopLoss()+(StopLoss*2+Sym.Spread())*Sym.Point())){
                        
                           if(OpenSell){
                              slv=SolveSellSL(StopLoss);
                                 if(CheckSellSL(slv)){                           
                                    lot=NormalizeDouble(Pos.Volume()+Lots,2);
                                    Trade.SetDeviationInPoints(Sym.Spread()*3);
                                       if(!Trade.Sell(lot,_Symbol,0,slv,0,"")){
                                          return;
                                       }
                                 }
                                 else{
                                    Print("Can not do the buy-sell rotation of position, running close");
                                    Trade.PositionClose(_Symbol,Sym.Spread()*3);
                                    return;
                                 }
              
                           }
                           else{
                              slv=SolveBuySL(StopLoss);
                                 if(CheckBuySL(slv)){  
                                    Trade.PositionModify(_Symbol,slv,0);
                                 }
                           }
                     }
                  break;
                  case POSITION_TYPE_SELL:
                     if(Sym.Ask()<(Pos.StopLoss()-(StopLoss*2+Sym.Spread())*Sym.Point())){
                           if(OpenBuy){
                              slv=SolveBuySL(StopLoss);
                                 if(CheckBuySL(slv)){
                                    lot=NormalizeDouble(Pos.Volume()+Lots,2);
                                    Trade.SetDeviationInPoints(Sym.Spread()*3);
                                       if(!Trade.Buy(Lots,_Symbol,0,slv,0,"")){
                                          return;
                                       }
                                 }
                                 else{
                                    Print("Can not do the buy-sell rotation of position, running close");
                                    Trade.PositionClose(_Symbol,Sym.Spread()*3);
                                    return;
                                 }                                    
                           }
                           else{
                              slv=SolveSellSL(StopLoss);
                                 if(CheckSellSL(slv)){  
                                    Trade.PositionModify(_Symbol,slv,0);
                                 }
                           }                 
                     }
                  break;
               }
            }
            else{
               if(OpenBuy && !OpenSell){ 
                  slv=SolveBuySL(StopLoss);
                     if(CheckBuySL(slv)){
                        Trade.SetDeviationInPoints(Sym.Spread()*3);
                        
                        if(!Trade.Buy(Lots,_Symbol,0,slv,0,"")){
                           return;
                        }
                     }
                     else{
                        Print("Buy position does not open, stoploss or takeprofit is near");
                     }         
               }
               // Sell
               if(OpenSell && !OpenBuy){
                  slv=SolveSellSL(StopLoss);
                     if(CheckSellSL(slv)){
                        Trade.SetDeviationInPoints(Sym.Spread()*3);
                        if(!Trade.Sell(Lots,_Symbol,0,slv,0,"")){
                           return;
                        }
                     }
                     else{
                        Print("Sell position does not open, stoploss or takeprofit is near");
                     }          
               }
            }
         }
      LastTime=ctm[0];
   }
}

//+------------------------------------------------------------------+
//| The PERCEPTRON - a perceiving and recognizing function           |
//+------------------------------------------------------------------+
double perceptron(){
   double w1 = x1 - 100;
   double w2 = x2 - 100;
   double w3 = x3 - 100;
   double w4 = x4 - 100;
   return(w1*a1[0]+w2*a2[0]+w3*a3[0]+w4*a4[0]);
}

//+------------------------------------------------------------------+
//|   Function of data copy for indicators and price                 |
//+------------------------------------------------------------------+
bool Indicators(){
   if(
      CopyBuffer(Handle,0,Shift,1,a1)==-1 ||
      CopyBuffer(Handle,0,Shift+7,1,a2)==-1 ||
      CopyBuffer(Handle,0,Shift+14,1,a3)==-1 ||
      CopyBuffer(Handle,0,Shift+21,1,a4)==-1
   ){
      return(false);
   }      
   perc=perceptron();
   return(true);
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool SignalOpenBuy(){
   return(perc>0);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                          |
//+------------------------------------------------------------------+
bool SignalOpenSell(){
   return(perc<0);
}

//+------------------------------------------------------------------+
//|   Function for calculation the buy stoploss                      |
//+------------------------------------------------------------------+
double SolveBuySL(int StopLossPoints){
   if(StopLossPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Ask()-Sym.Point()*StopLossPoints));
}

//+------------------------------------------------------------------+
//|   Function for calculation the sell stoploss                     |
//+------------------------------------------------------------------+
double SolveSellSL(int StopLossPoints){
   if(StopLossPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Bid()+Sym.Point()*StopLossPoints));
}

//+------------------------------------------------------------------+
//|   Function for calculation the minimum stoploss of buy           |
//+------------------------------------------------------------------+
double BuyMSL(){
   return(Sym.NormalizePrice(Sym.Bid()-Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for calculation the minimum stoploss of sell          |
//+------------------------------------------------------------------+
double SellMSL(){
   return(Sym.NormalizePrice(Sym.Ask()+Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for checking the buy stoploss                         |
//+------------------------------------------------------------------+
bool CheckBuySL(double StopLossPrice){
   if(StopLossPrice==0)return(true);
   return(StopLossPrice<BuyMSL());
}

//+------------------------------------------------------------------+
//|   Function for checking the sell stoploss                        |
//+------------------------------------------------------------------+
bool CheckSellSL(double StopLossPrice){
   if(StopLossPrice==0)return(true);
   return(StopLossPrice>SellMSL());
}
