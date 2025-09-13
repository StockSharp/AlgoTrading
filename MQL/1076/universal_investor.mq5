//+------------------------------------------------------------------+
//|                                           Universal_Investor.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Rewritten from MQL4. Link to the original publication - http://codebase.mql4.com/ru/324"
#property version   "1.00"

//--- input parameters
#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;

input int    MovingPeriod       = 23;    /*MovingPeriod*/     // Smoothing period
input double MaximumRisk        = 0.05;  /*MaximumRisk*/      // Risk (it is used if Lots=0)
input double Lots               = 0.1;   /*Lots*/             // Lot
input int    DecreaseFactor     = 0;     /*DecreaseFactor*/   // Lot reduction factor after losing trades. 0 - reduction disabled. The smaller the value, the greater the reduction. Where it is impossible to reduce the lot size, the minimum lot position is opened.

int emah;
int lwmah;
double ema1[1],ema2[1],lwma1[1],lwma2[1];

datetime ctm[1];
datetime LastTime;
double lot;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   emah=iMA(_Symbol,PERIOD_CURRENT,MovingPeriod,0,MODE_EMA,PRICE_CLOSE);
   lwmah=iMA(_Symbol,PERIOD_CURRENT,MovingPeriod,0,MODE_LWMA,PRICE_CLOSE);

   if(emah==INVALID_HANDLE || lwmah==INVALID_HANDLE){
      Alert("Error when loading the indicator, please try again");
      return(-1);
   }   
   
   if(!Sym.Name(_Symbol)){
      Alert("CSymbolInfo initialization error, please try again");    
      return(-1);
   }

   Print("Initialization of the Expert Advisor complete");
   
   return(0);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason){
   if(emah!=INVALID_HANDLE)IndicatorRelease(emah);
   if(lwmah!=INVALID_HANDLE)IndicatorRelease(lwmah);
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

   if(CopyTime(_Symbol,PERIOD_CURRENT,0,1,ctm)==-1){
      return;
   }
   if(ctm[0]!=LastTime){
      
      // Indicators
      if(!Indicators()){
         return;
      }  
      
      bool OpBuy=OpenBuy();
      bool OpSell=OpenSell();
      bool ClBuy=CloseBuy();
      bool ClSell=CloseSell();

      // Closing
      if(Pos.Select(_Symbol)){
         if(ClBuy && Pos.PositionType()==POSITION_TYPE_BUY){
            if(!Sym.RefreshRates()){
               return;  
            }
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3)){
               return;
            }
         }
         if(ClSell && Pos.PositionType()==POSITION_TYPE_SELL){
            if(!Sym.RefreshRates()){
               return;  
            }         
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3)){
               return;
            }
         }         
      }
      
      // Opening
      if(!Pos.Select(_Symbol)){
            // Buy
            if(OpBuy && !OpSell && !ClBuy){ 
               if(!Sym.RefreshRates())return;         
               if(!LotsOptimized(lot))return;
               Trade.SetDeviationInPoints(Sym.Spread()*3);
               if(!Trade.Buy(lot,_Symbol,0,0,0,""))return;
            }
            // Sell
            if(OpSell && !OpBuy && !ClSell){ 
               if(!Sym.RefreshRates())return;         
               if(!LotsOptimized(lot))return;
               Trade.SetDeviationInPoints(Sym.Spread()*3);
               if(!Trade.Sell(lot,_Symbol,0,0,0,""))return;
            }
      }            
      LastTime=ctm[0];
   }
   
}

//+------------------------------------------------------------------+
//| Function for getting indicator values                           |
//+------------------------------------------------------------------+
bool Indicators(){

   if(
      CopyBuffer(emah,0,1,1,ema1)==-1 ||
      CopyBuffer(emah,0,2,1,ema2)==-1 ||
      CopyBuffer(lwmah,0,1,1,lwma1)==-1 || 
      CopyBuffer(lwmah,0,2,1,lwma2)==-1
   ){
      return(false);
   }   
   return(true);   
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool OpenBuy(){
   return(lwma1[0]>ema1[0] && lwma1[0]>lwma2[0] && ema1[0]>ema2[0]);
}
//+------------------------------------------------------------------+
//|   Function for determining sell signals                           |
//+------------------------------------------------------------------+
bool OpenSell(){
   return(lwma1[0]<ema1[0] && lwma1[0]<lwma2[0] && ema1[0]<ema2[0]);
}

//+------------------------------------------------------------------+
//|   Function for determining buy closing signals                           |
//+------------------------------------------------------------------+
bool CloseBuy(){
   return(lwma1[0]<ema1[0]);
}

//+------------------------------------------------------------------+
//|   Function for determining sell closing signals                           |
//+------------------------------------------------------------------+
bool CloseSell(){
   return(lwma1[0]>ema1[0]);
}
//+------------------------------------------------------------------+
//|   Function for determining the lot based on the trade results               |
//+------------------------------------------------------------------+
bool LotsOptimized(double & aLots){
      if(Lots==0){
         aLots=fLotsNormalize(AccountInfoDouble(ACCOUNT_FREEMARGIN)*MaximumRisk/1000.0);        
      }
      else{
         aLots=Lots;         
      }
      if(DecreaseFactor<=0){
         return(true);
      }
      if(!HistorySelect(0,TimeCurrent())){
         return(false);
      }
   int losses=0;       
      for(int i=HistoryDealsTotal()-1;i>=0;i--){
         if(!Deal.SelectByIndex(i))return(false);
         if(Deal.DealType()!=DEAL_TYPE_BUY && Deal.DealType()!=DEAL_TYPE_SELL)continue;
         if(Deal.Entry()!=DEAL_ENTRY_OUT)continue;
         if(Deal.Profit()>0)break;
         if(Deal.Profit()<0)losses++;
                  
      }
      if(losses>1){
         aLots=fLotsNormalize(aLots-aLots*losses/DecreaseFactor);      
      }         
   return(true);      
   
}

//+------------------------------------------------------------------+
//|   Lot normalization function                                      |
//+------------------------------------------------------------------+
double fLotsNormalize(double aLots){
   aLots-=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   aLots/=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
   aLots=MathRound(aLots);
   aLots*=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
   aLots+=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   aLots=NormalizeDouble(aLots,2);
   aLots=MathMin(aLots,SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX));
   aLots=MathMax(aLots,SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN));   
   return(aLots);
}
