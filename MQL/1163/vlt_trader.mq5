//+------------------------------------------------------------------+
//|                                                   VLT_TRADER.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Rewritten from MQL4. The author is - http://www.mql4.com/ru/users/fortrader, link to original - http://codebase.mql4.com/ru/2977"
#property version   "1.00"

#define IND "iVLT"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/PositionInfo.mqh>
#include <Trade/OrderInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;
COrderInfo Order;

input int      period            =  9;       /*period*/           // Indicator period
input int      PendingLevel      =  100;     /*PendingLevel*/     // Setting level of the penging orders from High/Low of the previous bar
input double   Lots              =  0.1;     /*Lots*/             // Lot
input int      StopLoss          =  550;     /*StopLoss*/         // Stoploss in points, 0 - without stoploss
input int      TakeProfit        =  550;     /*TakeProfit*/       // Takeprofit in points, 0 - without takeprofit

int Handle=INVALID_HANDLE;
double ind[1],h[1],l[1];
datetime ctm[1];
datetime LastTime;
double lot,slv,mslv,tpv,mtpv;
string gvp;
bool CheckDelete=true;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   CheckDelete=true;

   // Preparation of global variables names
   gvp=MQL5InfoString(MQL5_PROGRAM_NAME)+"_"+_Symbol+"_"+IntegerToString(PeriodSeconds()/60)+"_"+IntegerToString(AccountInfoInteger(ACCOUNT_LOGIN));
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_DEMO)gvp=gvp+"_d";
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_REAL)gvp=gvp+"_r";
   if(MQL5InfoInteger(MQL5_TESTING))gvp=gvp+"_t";
   DeleteGV();

   // Loading indicators...
   
   Handle=iCustom(_Symbol,PERIOD_CURRENT,IND,period);

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
   DeleteGV();   
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
      bool Signal;
      if(!Indicators(Signal)){
         return;
      }   
      if(Signal && !Pos.Select(_Symbol)){
         int StopCount;
         if(!CountStopOrders(StopCount)){
            return;
         }            
         if(StopCount==0){
            GlobalVariableSet(gvp+"bs",ctm[0]);
            GlobalVariableSet(gvp+"ss",ctm[0]);
         }
         
      }
      LastTime=ctm[0];
   }
   
   if(GlobalVariableCheck(gvp+"bs")){
      if(GlobalVariableGet(gvp+"bs")==ctm[0]){
         if(CopyHigh(_Symbol,PERIOD_CURRENT,1,1,h)==-1)return;
         if(!Sym.RefreshRates())return;  
         double BuyStopPrice=Sym.NormalizePrice(h[0]+Sym.Point()*(PendingLevel+Sym.Spread()));
         double MinBuyStopPrice=Sym.NormalizePrice(Sym.Ask()+Sym.Point()*(Sym.StopsLevel()+1));
         BuyStopPrice=MathMax(BuyStopPrice,MinBuyStopPrice);
         slv=0;
            if(StopLoss>0){
               slv=Sym.NormalizePrice(BuyStopPrice-Sym.Point()*StopLoss);
               mslv=Sym.NormalizePrice(BuyStopPrice-Sym.Point()*(Sym.Spread()+Sym.StopsLevel()+1));
               slv=MathMin(slv,mslv);
            }
         tpv=0;
            if(TakeProfit>0){
               tpv=Sym.NormalizePrice(BuyStopPrice+Sym.Point()*TakeProfit);
               mtpv=Sym.NormalizePrice(BuyStopPrice+Sym.Point()*(Sym.StopsLevel()+1));
               tpv=MathMax(tpv,mtpv);
            }
            if(Trade.BuyStop(Lots,BuyStopPrice,_Symbol,slv,tpv)){
               GlobalVariableDel(gvp+"bs");
               CheckDelete=true;
            }
            else{
               return;
            }
      }
      else{
         GlobalVariableDel(gvp+"bs");
      }
   }
   
   if(GlobalVariableCheck(gvp+"ss")){
      if(GlobalVariableGet(gvp+"ss")==ctm[0]){
         if(CopyLow(_Symbol,PERIOD_CURRENT,1,1,l)==-1)return;
         if(!Sym.RefreshRates())return;  
         double SellStopPrice=Sym.NormalizePrice(l[0]-Sym.Point()*PendingLevel);
         double MinSellStopPrice=Sym.NormalizePrice(Sym.Bid()-Sym.Point()*(Sym.StopsLevel()+1));
         SellStopPrice=MathMin(SellStopPrice,MinSellStopPrice);
         slv=0;
            if(StopLoss>0){
               slv=Sym.NormalizePrice(SellStopPrice+Sym.Point()*StopLoss);
               mslv=Sym.NormalizePrice(SellStopPrice+Sym.Point()*(Sym.Spread()+Sym.StopsLevel()+1));
               slv=MathMax(slv,mslv);
            }
         tpv=0;
            if(TakeProfit>0){
               tpv=Sym.NormalizePrice(SellStopPrice-Sym.Point()*TakeProfit);
               mtpv=Sym.NormalizePrice(SellStopPrice-Sym.Point()*(Sym.StopsLevel()+1));
               tpv=MathMin(tpv,mtpv);
            }
            if(Trade.SellStop(Lots,SellStopPrice,_Symbol,slv,tpv)){
               GlobalVariableDel(gvp+"ss");
               CheckDelete=true;
            }
            else{
               return;
            }
      }
      else{
         GlobalVariableDel(gvp+"ss");
      }   
   }
   
   if(CheckDelete){
      if(Pos.Select(_Symbol)){
         int StopCount;
         if(!CountStopOrders(StopCount)){
            return;
         }  
         if(StopCount>0){
            if(!DeleteStops()){
               return;
            }
         }
         else{
            CheckDelete=false;
         }            
      }
   }

}

//+------------------------------------------------------------------+
//|   Function to delete stoporders                                  |
//+------------------------------------------------------------------+
bool DeleteStops(){
   bool rv=true;
      for(int i=OrdersTotal()-1;i>=0;i--){
         if(Order.SelectByIndex(i)){
            if(Order.Symbol()==_Symbol){
               if(Order.OrderType()==ORDER_TYPE_BUY_STOP || Order.OrderType()==ORDER_TYPE_SELL_STOP){
                  if(!Trade.OrderDelete(Order.Ticket())){
                     rv=false;
                  }
               }
            }
         }
         else{
            rv=false;
         }                           
      }
   return(rv);
}


//+------------------------------------------------------------------+
//|   Function to calculate stoporders                               |
//+------------------------------------------------------------------+
bool CountStopOrders(int & aCount){
   aCount=0;
      for(int i=OrdersTotal()-1;i>=0;i--){
         if(!Order.SelectByIndex(i))return(false);
         if(Order.Symbol()!=_Symbol)continue;
         if(Order.OrderType()==ORDER_TYPE_BUY_STOP || Order.OrderType()==ORDER_TYPE_SELL_STOP)aCount++;
      }
   return(true);
}

//+------------------------------------------------------------------+
//|   Function of data copy for indicators and price                 |
//+------------------------------------------------------------------+
bool Indicators(bool & aSignal){
   if(CopyBuffer(Handle,2,1,1,ind)==-1)return(false);
   aSignal=(ind[0]!=EMPTY_VALUE);
   return(true);
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool SignalOpenBuy(){

   return(false);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                          |
//+------------------------------------------------------------------+
bool SignalOpenSell(){

   return(false);
}

//+------------------------------------------------------------------+
//|   Function for determining buy close signals                     |
//+------------------------------------------------------------------+
bool SignalCloseBuy(){

   return (false);
}

//+------------------------------------------------------------------+
//|   Function for determining sell close signals                    |
//+------------------------------------------------------------------+
bool SignalCloseSell(){

   return (false);
}

//+------------------------------------------------------------------+
//|   Function for calculation the buy stoploss                      |
//+------------------------------------------------------------------+
double SolveBuySL(int StopLossPoints){
   if(StopLossPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Ask()-Sym.Point()*StopLossPoints));
}

//+------------------------------------------------------------------+
//|   Function for calculation the buy takeprofit                    |
//+------------------------------------------------------------------+
double SolveBuyTP(int TakeProfitPoints){
   if(TakeProfitPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Ask()+Sym.Point()*TakeProfitPoints));   
}

//+------------------------------------------------------------------+
//|   Function for calculation the sell stoploss                     |
//+------------------------------------------------------------------+
double SolveSellSL(int StopLossPoints){
   if(StopLossPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Bid()+Sym.Point()*StopLossPoints));
}

//+------------------------------------------------------------------+
//|   Function for calculation the sell takeprofit                   |
//+------------------------------------------------------------------+
double SolveSellTP(int TakeProfitPoints){
   if(TakeProfitPoints==0)return(0);
   return(Sym.NormalizePrice(Sym.Bid()-Sym.Point()*TakeProfitPoints));   
}

//+------------------------------------------------------------------+
//|   Function for calculation the minimum stoploss of buy           |
//+------------------------------------------------------------------+
double BuyMSL(){
   return(Sym.NormalizePrice(Sym.Bid()-Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for calculation the minimum takeprofit of buy         |
//+------------------------------------------------------------------+
double BuyMTP(){
   return(Sym.NormalizePrice(Sym.Ask()+Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for calculation the minimum stoploss of sell          |
//+------------------------------------------------------------------+
double SellMSL(){
   return(Sym.NormalizePrice(Sym.Ask()+Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for calculation the minimum takeprofit of sell        |
//+------------------------------------------------------------------+
double SellMTP(){
   return(Sym.NormalizePrice(Sym.Bid()-Sym.Point()*Sym.StopsLevel()));
}

//+------------------------------------------------------------------+
//|   Function for checking the buy stoploss                         |
//+------------------------------------------------------------------+
bool CheckBuySL(double StopLossPrice){
   if(StopLossPrice==0)return(true);
   return(StopLossPrice<BuyMSL());
}

//+------------------------------------------------------------------+
//|   Function for checking the buy takeprofit                       |
//+------------------------------------------------------------------+
bool CheckBuyTP(double TakeProfitPrice){
   if(TakeProfitPrice==0)return(true);
   return(TakeProfitPrice>BuyMTP());
}

//+------------------------------------------------------------------+
//|   Function for checking the sell stoploss                        |
//+------------------------------------------------------------------+
bool CheckSellSL(double StopLossPrice){
   if(StopLossPrice==0)return(true);
   return(StopLossPrice>SellMSL());
}

//+------------------------------------------------------------------+
//|   Function for checking the sell takeprofit                      |
//+------------------------------------------------------------------+
bool CheckSellTP(double TakeProfitPrice){
   if(TakeProfitPrice==0)return(true);
   return(TakeProfitPrice<SellMTP());
}


//+------------------------------------------------------------------+
//|   The function which define the lot by the result of trade       |
//+------------------------------------------------------------------+
bool SolveLots(double & aLots){
   aLots=Lots;         
   bool rv=true;   
   return(rv);
}

//+------------------------------------------------------------------+
//|   Lot normalization function                                     |
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

//+------------------------------------------------------------------+
//|   Function to delete the global variables with gvp prefix        | 
//+------------------------------------------------------------------+
void DeleteGV(){
   if(MQL5InfoInteger(MQL5_TESTING)){
      for(int i=GlobalVariablesTotal()-1;i>=0;i--){
         if(StringFind(GlobalVariableName(i),gvp,0)==0){
            GlobalVariableDel(GlobalVariableName(i));
         }
      }
   }
}   
