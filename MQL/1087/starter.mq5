//+------------------------------------------------------------------+
//|                                                      starter.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "The Expert Advisor developed by unknown author is rewritten from MQL4 and was originally published here http://codebase.mql4.com/ru/461 by Collector (http://www.mql4.com/ru/users/Collector)"
#property version   "1.00"

/*
   The Expert Advisor is based on the following indicators: Laguerre (http://www.mql5.com/ru/code/432), CCI and MA.
   
*/

#define IND1 "Laguerre"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;

//--- input parameters
input double               Lots              =  0.1;           /*Lots*/             // Lot; if the value is 0, MaximumRisk value is used
input double               MaximumRisk       =  0.05;          /*MaximumRisk*/      // Risk (it is used if Lots=0)
input int                  DecreaseFactor    =  0;             /*DecreaseFactor*/   // Lot reduction factor after losing trades. 0 - reduction disabled. The smaller the value, the greater the reduction. Where it is impossible to reduce the lot size, the minimum lot position is opened.
input int                  StopLoss          =  100;           /*StopLoss*/         // Stop Loss in points
input int                  TakeProfit        =  0;             /*TakeProfit*/       // Take Profit in points
input bool                 VirtualSLTP       =  true;          /*VirtualSLTP*/      // Stop Loss and Take Profit are not set. Instead, a position is closed upon reaching loss or profit as specified in the StopLoss and TakeProfit parameters.
input double               LagGamma          =  0.7;           /*LagGamma*/         // Laguerre indicator parameter
input int                  CCIPeriod         =  14;	         /*CCIPeriod*/        // CCI period
input ENUM_APPLIED_PRICE   CCIPrice          =  PRICE_CLOSE;	/*CCIPrice*/         // CCI price
input double               CCILevel          =  5;	            /*CCILevel*/         // CCI level
input int                  MAPeriod          =  5;	            /*MAPeriod*/         // MA period
input int                  MAShift           =  0;	            /*MAShift*/          // MA shift
input ENUM_MA_METHOD       MAMethod          =  MODE_EMA;	   /*MAMethod*/         // MA method
input ENUM_APPLIED_PRICE   MAPrice           =  PRICE_MEDIAN;	/*MAPrice*/          // MA price
input int                  Shift             =  0;             /*Shift*/            // The bar on which the indicator values are checked: 0 - new forming bar, 1 - first completed bar


int LHandle=INVALID_HANDLE;
int CCIHandle=INVALID_HANDLE;
int MAHandle=INVALID_HANDLE;

double lg0[1], cci0[1], ma0[1], ma1[1];

datetime ctm[1];
datetime LastTime;
double lot,slv=0,msl,tpv=0,mtp;

bool _VirtualSLTP;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   _VirtualSLTP=VirtualSLTP;
   if(_VirtualSLTP && StopLoss<=0 && TakeProfit<=0){
      _VirtualSLTP=false;
   }

   // Loading indicators...
   
   LHandle=iCustom(_Symbol,PERIOD_CURRENT,IND1,LagGamma);
   CCIHandle=iCCI(NULL,PERIOD_CURRENT,CCIPeriod,CCIPrice);
   MAHandle=iMA(NULL,PERIOD_CURRENT,MAPeriod,MAShift,MAMethod,MAPrice);

   if(LHandle==INVALID_HANDLE || CCIHandle==INVALID_HANDLE || MAHandle==INVALID_HANDLE){
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
   if(LHandle!=INVALID_HANDLE)IndicatorRelease(LHandle);
   if(CCIHandle!=INVALID_HANDLE)IndicatorRelease(CCIHandle);
   if(MAHandle!=INVALID_HANDLE)IndicatorRelease(MAHandle);
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
      bool CloseBuy=SignalCloseBuy();
      bool CloseSell=SignalCloseSell();
      bool OpenBuy=SignalOpenBuy();
      bool OpenSell=SignalOpenSell();

      // Closing
      if(Pos.Select(_Symbol)){
         if(CloseBuy && Pos.PositionType()==POSITION_TYPE_BUY){
            if(!Sym.RefreshRates()){
               return;  
            }
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3)){
               return;
            }
         }
         if(CloseSell && Pos.PositionType()==POSITION_TYPE_SELL){
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
            if(OpenBuy && !OpenSell && !CloseBuy){ 
               if(!Sym.RefreshRates())return;         
               if(!SolveLots(lot))return;
                  if(!VirtualSLTP){
                     slv=SolveBuySL(StopLoss);
                     tpv=SolveBuyTP(TakeProfit);
                  }
                  if(CheckBuySL(slv) && CheckBuyTP(tpv)){
                     Trade.SetDeviationInPoints(Sym.Spread()*3);
                     if(!Trade.Buy(lot,_Symbol,0,slv,tpv,"")){
                        return;
                     }
                  }
                  else{
                     Print("Cannot open a Buy position, nearing the Stop Loss or Take Profit");
                  }         
            }
            // Sell
            if(OpenSell && !OpenBuy && !CloseSell){
               if(!Sym.RefreshRates())return;         
               if(!SolveLots(lot))return;
                  if(!VirtualSLTP){
                     slv=SolveBuySL(StopLoss);
                     tpv=SolveBuyTP(TakeProfit);
                  }
                  if(CheckSellSL(slv) && CheckSellTP(tpv)){
                     Trade.SetDeviationInPoints(Sym.Spread()*3);
                     if(!Trade.Sell(lot,_Symbol,0,slv,tpv,"")){
                        return;
                     }
                  }
                  else{
                     Print("Cannot open a Sell position, nearing the Stop Loss or Take Profit");
                  }          
            }
      }            
      LastTime=ctm[0];
   }
   
   Virtual_SLTP();

}

void Virtual_SLTP(){
   if(!_VirtualSLTP)return;
   if(!Pos.Select(_Symbol)){
      return;
   }         
   if(!Sym.RefreshRates()){
      return;  
   }   
   double nsl,tmsl,psl;  
   switch(Pos.PositionType()){
      case POSITION_TYPE_BUY:
         if(
            (TakeProfit>0 && Sym.NormalizePrice(Sym.Bid()-Pos.PriceOpen()-Sym.Point()*TakeProfit)>=0) || 
            (StopLoss>0 && Sym.NormalizePrice(Pos.PriceOpen()-Sym.Bid()-Sym.Point()*StopLoss)>=0)
         ){
            Trade.PositionClose(_Symbol,Sym.Spread()*3);
         }
      break;
      case POSITION_TYPE_SELL:
         if(
            (TakeProfit>0 && Sym.NormalizePrice(Pos.PriceOpen()-Sym.Ask()-Sym.Point()*TakeProfit)>=0) || 
            (StopLoss>0 && Sym.NormalizePrice(Sym.Ask()-Pos.PriceOpen()-Sym.Point()*StopLoss)>=0)
         ){
            Trade.PositionClose(_Symbol,Sym.Spread()*3);
         }    
      break;
   }
}

//+------------------------------------------------------------------+
//|   Function for copying indicator data and price                   |
//+------------------------------------------------------------------+
bool Indicators(){
   if(
      CopyBuffer(LHandle,0,Shift,1,lg0)==-1 ||
      CopyBuffer(CCIHandle,0,Shift,1,cci0)==-1 ||
      CopyBuffer(MAHandle,0,Shift,1,ma0)==-1 ||
      CopyBuffer(MAHandle,0,Shift+1,1,ma1)==-1
   ){
      return(false);
   }
   return(true);
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool SignalOpenBuy(){
   return(lg0[0]==0 && ma0[0]>ma1[0] && cci0[0]<-CCILevel);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                           |
//+------------------------------------------------------------------+
bool SignalOpenSell(){
   return(lg0[0]==1 && ma0[0]<ma1[0] && cci0[0]>CCILevel);
}

//+------------------------------------------------------------------+
//|   Function for determining buy closing signals                  |
//+------------------------------------------------------------------+
bool SignalCloseBuy(){
   return(lg0[0]>0.9);
}

//+------------------------------------------------------------------+
//|   Function for determining sell closing signals                  |
//+------------------------------------------------------------------+
bool SignalCloseSell(){
   return(lg0[0]<0.1);
}

//+------------------------------------------------------------------+
//|   Function for calculating the Stop Loss for a buy position                               |
//+------------------------------------------------------------------+
double SolveBuySL(int StopLossPoints){
   if(StopLossPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Ask()-Sym.Point()*StopLossPoints));
}

//+------------------------------------------------------------------+
//|   Function for calculating the Take Profit for a buy position                            |
//+------------------------------------------------------------------+
double SolveBuyTP(int TakeProfitPoints){
   if(TakeProfitPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Ask()+Sym.Point()*TakeProfitPoints));   
}

//+------------------------------------------------------------------+
//|   Function for calculating the Stop Loss for a sell position                               |
//+------------------------------------------------------------------+
double SolveSellSL(int StopLossPoints){
   if(StopLossPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Bid()+Sym.Point()*StopLossPoints));
}

//+------------------------------------------------------------------+
//|   Function for calculating the Take Profit for a sell position                             |
//+------------------------------------------------------------------+
double SolveSellTP(int TakeProfitPoints){
   if(TakeProfitPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Bid()-Sym.Point()*TakeProfitPoints));   
}

//+------------------------------------------------------------------+
//|   Function for calculating the minimum Stop Loss for a buy position                  |
//+------------------------------------------------------------------+
double BuyMSL(){
   return(Sym.NormalizePrice(Sym.Bid()-Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for calculating the minimum Take Profit for a buy position                |
//+------------------------------------------------------------------+
double BuyMTP(){
   return(Sym.NormalizePrice(Sym.Ask()+Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for calculating the minimum Stop Loss for a sell position                 |
//+------------------------------------------------------------------+
double SellMSL(){
   return(Sym.NormalizePrice(Sym.Ask()+Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for calculating the minimum Take Profit for a sell position               |
//+------------------------------------------------------------------+
double SellMTP(){
   return(Sym.NormalizePrice(Sym.Bid()-Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for checking the Stop Loss for a buy position                                 |
//+------------------------------------------------------------------+
bool CheckBuySL(double StopLossPrice){
   if(StopLossPrice==0)return(true);
   return(StopLossPrice<BuyMSL());
}

//+------------------------------------------------------------------+
//|   Function for checking the Take Profit for a buy position                               |
//+------------------------------------------------------------------+
bool CheckBuyTP(double TakeProfitPrice){
   if(TakeProfitPrice==0)return(true);
   return(TakeProfitPrice>BuyMTP());
}

//+------------------------------------------------------------------+
//|   Function for checking the Stop Loss for a sell position                                 |
//+------------------------------------------------------------------+
bool CheckSellSL(double StopLossPrice){
   if(StopLossPrice==0)return(true);
   return(StopLossPrice>SellMSL());
}

//+------------------------------------------------------------------+
//|   Function for checking the Take Profit for a sell position                              |
//+------------------------------------------------------------------+
bool CheckSellTP(double TakeProfitPrice){
   if(TakeProfitPrice==0)return(true);
   return(TakeProfitPrice<SellMTP());
}


//+------------------------------------------------------------------+
//|   Function for determining the lot based on the trade results               |
//+------------------------------------------------------------------+
bool SolveLots(double & aLots){
      if(Lots==0){
         aLots=fLotsNormalize(AccountInfoDouble(ACCOUNT_FREEMARGIN)*MaximumRisk/1000.0);        
      }
      else{
         aLots=Lots;         
      }

   bool rv=true;   
   rv=DecreaseLots(aLots);

   return(rv);
}

//+------------------------------------------------------------------+
//|   Function for reducing the lot based on the trade results                |
//+------------------------------------------------------------------+
bool DecreaseLots(double & aLots){
      if(DecreaseFactor<=0){
         return(true);
      }
      if(!HistorySelect(0,TimeCurrent())){
         return(false);
      }
   int losses=0;       
      for(int i=HistoryDealsTotal()-1;i>=0;i--){
         if(!Deal.SelectByIndex(i))return(false);
         if(Deal.Symbol()!=_Symbol)continue;
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
