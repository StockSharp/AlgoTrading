//+------------------------------------------------------------------+
//|                                                    T3MA(MTC).mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Rewritten from MQL4. Link to original - http://codebase.mql4.com/ru/2661, author is SAW (http://www.mql4.com/ru/users/SAW)"
#property version   "1.00"

/*
   The author: http://www.mql4.com/ru/users/SAW
   
   The original: http://codebase.mql4.com/ru/2661
   
   How it works: Expert works by the signals of the T3MA-ALARM indicator. 
   
*/

#define IND "T3MA-ALARM"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CSymbolInfo Sym;
CPositionInfo Pos;
   
//--- input parameters
input double                           Lots              =  0.1;           /*Lots*/             // Lot
input int                              StopLoss          =  0;             /*StopLoss*/         // Stoploss in points, 0 - without stoploss.
input int                              TakeProfit        =  300;           /*TakeProfit*/       // Takeprofit in points, 0 - without takeprofit.
input int                              Shift             =  1;             /*Shift*/            // Bar on which indicators are checked: 0 - shaped bar, 1 - the first shaped bar
input bool                             RevClose          =  true;          /*RevClose*/         // Close the position by opposite signal
input int                              MAPeriod          =  19;            /*MAPeriod*/         // MA period
input int                              MAShift           =  0;	            /*MAShift*/          // MA shift
input ENUM_MA_METHOD                   MAMethod          =  MODE_EMA;	   /*MAMethod*/         // MA method
input ENUM_APPLIED_PRICE               MAPrice           =  PRICE_CLOSE;	/*MAPrice*/          // MA price

int Handle=INVALID_HANDLE;

datetime ctm[1];
datetime LastTime;
double lot,slv,msl,tpv,mtp;
string gvp;

double ba[1],sa[1];

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   // Loading indicators...
   Handle=iCustom(NULL,PERIOD_CURRENT,IND,MAPeriod,MAShift,MAMethod,MAPrice);

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
      bool CloseBuy=false;
      bool CloseSell=false;
      bool OpenBuy=SignalOpenBuy();
      bool OpenSell=SignalOpenSell();
      
      if(RevClose){
         CloseBuy=OpenSell;
         CloseSell=OpenBuy;
      }
      

      // Close
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
      
      // Open
      if(!Pos.Select(_Symbol)){
            if(OpenBuy && !OpenSell && !CloseBuy){ 
               if(!Sym.RefreshRates())return;         
               if(!SolveLots(lot))return;
               slv=SolveBuySL(StopLoss);
               tpv=SolveBuyTP(TakeProfit);
                  if(CheckBuySL(slv) && CheckBuyTP(tpv)){
                     Trade.SetDeviationInPoints(Sym.Spread()*3);
                     if(!Trade.Buy(lot,_Symbol,0,slv,tpv,"")){
                        return;
                     }
                  }
                  else{
                     Print("Buy position does not open, stoploss or takeprofit is near");
                  }         
            }
            // Sell
            if(OpenSell && !OpenBuy && !CloseSell){
               if(!Sym.RefreshRates())return;         
               if(!SolveLots(lot))return;
               slv=SolveSellSL(StopLoss);
               tpv=SolveSellTP(TakeProfit);
                  if(CheckSellSL(slv) && CheckSellTP(tpv)){
                     Trade.SetDeviationInPoints(Sym.Spread()*3);
                     if(!Trade.Sell(lot,_Symbol,0,slv,tpv,"")){
                        return;
                     }
                  }
                  else{
                     Print("Sell position does not open, stoploss or takeprofit is near");
                  }          
            }
      }            
      LastTime=ctm[0];
   }
}

//+------------------------------------------------------------------+
//|   Function of data copy for indicators and price                 |
//+------------------------------------------------------------------+
bool Indicators(){
   if(CopyBuffer(Handle,1,Shift,1,ba)==-1 ||
      CopyBuffer(Handle,2,Shift,1,sa)==-1
   )return(false);
   
   
         
   return(true);
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool SignalOpenBuy(){
   return(ba[0]!=0);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                          |
//+------------------------------------------------------------------+
bool SignalOpenSell(){
   return(sa[0]!=0);
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
