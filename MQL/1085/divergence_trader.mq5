//+------------------------------------------------------------------+
//|                                                 TradeChannel.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "This Expert Advisor developed by Ron Thompson, http://www.lightpatch.com/forex, is rewritten from MQL4 and was originally published here http://codebase.mql4.com/ru/490 by  Collector (http://www.mql4.com/ru/users/Collector)"
#property version   "1.00"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>
#include <Trade/AccountInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;
CAccountInfo Acc;

//--- input parameters
input double   Lots           =  0.1;           /*Lots*/             // Position volume
input bool     MultyOpen      =  false;         /*MultyOpen*/        // Permission to add volume to a position 
input double   MaxVolume      =  0.5;           /*MaxVolume*/        // Maximum position volume; it is checked if MultyOpen is used
input int      StopLoss       =  550;           /*StopLoss*/         // Stop Loss in points
input int      TakeProfit     =  550;           /*TakeProfit*/       // Take Profit in points
input int      Trailing       =  0;             /*Trailing*/         // Trailing stop level; when the value is 0, the trailing stop is disabled
input int      BreakEven      =  0;             /*BreakEven*/        // Profit level of a position expressed in points in order to move the Stop Loss to the breakeven level. If the value is 0, the function is disabled 
input int      Fast_Period    =  7;             /*Fast_Period*/      // Fast 明 period
input int      Fast_Price     =  PRICE_OPEN;    /*Fast_Price*/       // Fast 明 price
input int      Slow_Period    =  88;            /*Slow_Period*/      // Slow 明 period
input int      Slow_Price     =  PRICE_OPEN;    /*Slow_Price*/       // Slow 明 price
input double   DVBuySell      =  0.0011;        /*DVBuySell*/        // Buy and Sell minimum divergence level -DVBuySell
input double   DVStayOut      =  0.0079;        /*DVStayOut*/        // Buy and Sell maximum divergence level -DVStayOut
input bool     BasketProfitON =  false;         /*BasketProfitON*/   // Enables the function for closing all positions in the account when a certain profit level is reached
input int      BasketProfit   =  75;            /*BasketProfit*/     // Account profit at which all account positions will close (for all symbols)
input bool     BasketLossON   =  false;         /*BasketLossON*/     // Enables the function for closing all positions in the account when a certain loss level is reached
input int      BasketLoss     =  9999;          /*BasketLoss*/       // Account loss at which all account positions will close (for all symbols)

int MAFastHandle=INVALID_HANDLE;
int MASlowHandle=INVALID_HANDLE;

double maF1[1];
double maS1[1];
double maF2[1];
double maS2[1];


datetime ctm[1];
datetime LastTime;
double lot,slv,msl,tpv,mtp;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   // Loading indicators...
   
   MAFastHandle=iMA(_Symbol,PERIOD_CURRENT,Fast_Period,0,MODE_SMA,Fast_Price);
   MASlowHandle=iMA(_Symbol,PERIOD_CURRENT,Slow_Period,0,MODE_SMA,Slow_Price);

   if(MAFastHandle==INVALID_HANDLE || MASlowHandle==INVALID_HANDLE){
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
   if(MAFastHandle!=INVALID_HANDLE)IndicatorRelease(MAFastHandle);
   if(MASlowHandle!=INVALID_HANDLE)IndicatorRelease(MASlowHandle);   
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

   if(!BasketProfLossClose())return;

   if(CopyTime(_Symbol,PERIOD_CURRENT,0,1,ctm)==-1){
      return;
   }
   if(ctm[0]!=LastTime){
      
      // Indicators
      if(!Indicators()){
         return;
      }   
      
      double Diver=Divergence();
      
      // Signals
      bool CloseBuy=SignalCloseBuy();
      bool CloseSell=SignalCloseSell();
      bool OpenBuy=SignalOpenBuy(Diver);
      bool OpenSell=SignalOpenSell(Diver);

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
      bool PosExists=Pos.Select(_Symbol);
      if(!PosExists || MultyOpen){
         if(OpenBuy && !OpenSell && !CloseBuy){ 
            if(!Sym.RefreshRates())return;         
            if(!SolveLots(lot))return;
            slv=SolveBuySL(StopLoss);
            tpv=SolveBuyTP(TakeProfit);
            bool DoOpen=true;
               if(PosExists){
                  DoOpen=(NormalizeDouble(Pos.Volume()+lot,2)<=MaxVolume && Pos.PositionType()==POSITION_TYPE_BUY);
               }  
               if(DoOpen){
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
         }
         // Sell
         if(OpenSell && !OpenBuy && !CloseSell){
            if(!Sym.RefreshRates())return;         
            if(!SolveLots(lot))return;
            slv=SolveSellSL(StopLoss);
            tpv=SolveSellTP(TakeProfit);
            bool DoOpen=true;
               if(PosExists){
                  DoOpen=(NormalizeDouble(Pos.Volume()+lot,2)<=MaxVolume && Pos.PositionType()==POSITION_TYPE_SELL);
               }  
               if(DoOpen){               
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
      }            
      LastTime=ctm[0];
   }
   
   fSimpleTrailing();
   fSimpleBreakEven();

}

double Divergence(){
   double dv1=(maF1[0]-maS1[0]);
   double dv2=((maF1[0]-maS1[0])-(maF2[0]-maS2[0]));
   return(dv1-dv2);
}

bool BasketProfLossClose(){
   bool rv=true;
      if(BasketProfitON){
         if(Acc.Profit()>=BasketProfit){
            return(CloseAllPosOnAccount());
         }
      }
      if(BasketLossON){
         if(Acc.Profit()<=-BasketLoss){
            return(CloseAllPosOnAccount());         
         }
      }
   return(true);      
}

bool CloseAllPosOnAccount(){
   bool rv=true;
      for(int i=PositionsTotal()-1;i>=0;i--){
         if(Pos.SelectByIndex(i)){
            if(!Trade.PositionClose(Pos.Symbol())){
               rv=false;
            }
         }
         else{
            rv=false;
         }
      }
   return(rv);
}

//====================================================================

//+------------------------------------------------------------------+
//|   Function for copying indicator data and price                   |
//+------------------------------------------------------------------+
bool Indicators(){
   if(
      CopyBuffer(MAFastHandle,0,0,1,maF1)==-1 ||
      CopyBuffer(MASlowHandle,0,0,1,maS1)==-1 ||
      CopyBuffer(MAFastHandle,0,1,1,maF2)==-1 ||
      CopyBuffer(MASlowHandle,0,1,1,maS2)==-1
   ){
      return(false);
   }         
   return(true);
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool SignalOpenBuy(double Diver){
   return(Diver>=DVBuySell && Diver<=DVStayOut);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                           |
//+------------------------------------------------------------------+
bool SignalOpenSell(double Diver){
   return(Diver<= (DVBuySell*(-1)) && Diver>=(DVStayOut*(-1)));
}

//+------------------------------------------------------------------+
//|   Function for determining buy closing signals                  |
//+------------------------------------------------------------------+
bool SignalCloseBuy(){

   return (false);
}

//+------------------------------------------------------------------+
//|   Function for determining sell closing signals                  |
//+------------------------------------------------------------------+
bool SignalCloseSell(){

   return (false);
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
//|   Lot calculation function                                        |
//+------------------------------------------------------------------+
bool SolveLots(double & SolvedLots){
   SolvedLots=Lots;
   return(true);
}

//+------------------------------------------------------------------+
//| Simple Trailing function                                       |
//+------------------------------------------------------------------+
void fSimpleTrailing(){
   if(Trailing<=0){
      return;
   }         
   if(!Pos.Select(_Symbol)){
      return;
   }         
   if(!Sym.RefreshRates()){
      return;  
   }   
   double nsl,tmsl,psl;  
   switch(Pos.PositionType()){
      case POSITION_TYPE_BUY:
         nsl=Sym.NormalizePrice(Sym.Bid()-_Point*Trailing);
            if(nsl>=Sym.NormalizePrice(Pos.PriceOpen())){
               if(nsl>Sym.NormalizePrice(Pos.StopLoss())){
                  tmsl=Sym.NormalizePrice(Sym.Bid()-_Point*Sym.StopsLevel());
                     if(nsl<tmsl){
                        Trade.PositionModify(_Symbol,nsl,Pos.TakeProfit());
                     }
               }
            }
      break;
      case POSITION_TYPE_SELL:
         nsl=Sym.NormalizePrice(Sym.Ask()+_Point*Trailing);
            if(nsl<=Sym.NormalizePrice(Pos.PriceOpen())){
               psl=Sym.NormalizePrice(Pos.StopLoss());
                  if(nsl<psl || psl==0){
                     tmsl=Sym.NormalizePrice(Sym.Ask()+_Point*Sym.StopsLevel());
                        if(nsl>tmsl){
                           Trade.PositionModify(_Symbol,nsl,Pos.TakeProfit());
                        }
                  }
            }      
      break;
   }
}

//+------------------------------------------------------------------+
//| Simple breakeven function                                       |
//+------------------------------------------------------------------+
void fSimpleBreakEven(){
   if(BreakEven<=0){
      return;
   }         
   if(!Pos.Select(_Symbol)){
      return;
   }   
   if(!Sym.RefreshRates()){
      return;  
   }            
   double op=Sym.NormalizePrice(Pos.PriceOpen());
   double sl=Sym.NormalizePrice(Pos.StopLoss());
   double nsl;  
   switch(Pos.PositionType()){
      case POSITION_TYPE_BUY:
         if(sl<op){
            nsl=Sym.NormalizePrice(Sym.Bid()-_Point*BreakEven); 
               if(nsl>=op){
                  if(op<Sym.NormalizePrice(Sym.Bid()-_Point*Sym.StopsLevel())){
                     Trade.PositionModify(_Symbol,op,Sym.NormalizePrice(Pos.TakeProfit()));
                  }
               }   
         }
      break;
      case POSITION_TYPE_SELL:
         if(sl>op || sl==0){
            nsl=Sym.NormalizePrice(Sym.Ask()+_Point*BreakEven); 
               if(nsl<=op){
                  if(op>Sym.NormalizePrice(Sym.Ask()+_Point*Sym.StopsLevel())){
                     Trade.PositionModify(_Symbol,op,Sym.NormalizePrice(Pos.TakeProfit()));
                  }
               }   
         }    
      break;
   }
}
