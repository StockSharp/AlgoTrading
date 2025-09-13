//+------------------------------------------------------------------+
//|                                                       Eugene.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description ""
#property version   "1.00"

#property description "Rewritten from MQL4. The author is Builder (http://www.mql4.com/ru/users/Builder), Link to original - http://codebase.mql4.com/ru/2583"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;
   
//--- input parameters
input double   Lots              =  0.1;     /*Lots*/             // Lot, MaximumRisk parameter works with zero value.
input int      StopLoss          =  0;       /*StopLoss*/         // Stoploss in points, 0 - without stoploss.
input int      TakeProfit        =  0;       /*TakeProfit*/       // Takeprofit in points, 0 - without takeprofit.
input bool     InvertSignals     =  false;   /*InvertSignals*/    // Swap the trading signals

datetime ctm[1];
double lot,slv,msl,tpv,mtp;
string gvp;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   gvp=MQL5InfoString(MQL5_PROGRAM_NAME)+"_"+_Symbol+"_"+IntegerToString(PeriodSeconds()/60)+"_"+IntegerToString(AccountInfoInteger(ACCOUNT_LOGIN));
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_DEMO)gvp=gvp+"_d";
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_REAL)gvp=gvp+"_r";
   if(MQL5InfoInteger(MQL5_TESTING))gvp=gvp+"_t";
   DeleteGV();

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
   DeleteGV();
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

      if(CopyTime(_Symbol,PERIOD_CURRENT,0,1,ctm)==-1){
         return;
      }

      bool OpenBuy,OpenSell,CloseBuy,CloseSell;
  
      // Indicators
      if(InvertSignals){
         if(!Indicators(OpenSell,OpenBuy,CloseSell,CloseBuy)){
            return;
         }   
      }
      else{
         if(!Indicators(OpenBuy,OpenSell,CloseBuy,CloseSell)){
            return;
         }   
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
            if(OpenBuy && !OpenSell && !CloseBuy && GlobalVariableGet(gvp+"_LBT")!=ctm[0]){ 
               if(!Sym.RefreshRates())return;         
               if(!SolveLots(lot))return;
               slv=SolveBuySL(StopLoss);
               tpv=SolveBuyTP(TakeProfit);
                  if(CheckBuySL(slv) && CheckBuyTP(tpv)){
                     Trade.SetDeviationInPoints(Sym.Spread()*3);
                     if(!Trade.Buy(lot,_Symbol,0,slv,tpv,"")){
                        return;
                     }
                     GlobalVariableSet(gvp+"_LBT",ctm[0]);
                  }
                  else{
                     Print("Buy position does not open, stoploss or takeprofit is near");
                  }         
            }
            // Sell
            if(OpenSell && !OpenBuy && !CloseSell && GlobalVariableGet(gvp+"_LST")!=ctm[0]){
               if(!Sym.RefreshRates())return;         
               if(!SolveLots(lot))return;
               slv=SolveSellSL(StopLoss);
               tpv=SolveSellTP(TakeProfit);
                  if(CheckSellSL(slv) && CheckSellTP(tpv)){
                     Trade.SetDeviationInPoints(Sym.Spread()*3);
                     if(!Trade.Sell(lot,_Symbol,0,slv,tpv,"")){
                        return;
                     }
                     GlobalVariableSet(gvp+"_LST",ctm[0]);
                  }
                  else{
                     Print("Sell position does not open, stoploss or takeprofit is near");
                  }          
            }
      }            

}

//+------------------------------------------------------------------+
//|   Function of data copy for indicators and price                 |
//+------------------------------------------------------------------+
bool Indicators(bool & aBuy, bool & aSell,bool & aCloseBuy,bool & aCloseSell){

   aBuy=false;
   aSell=false;
   aCloseBuy=false;
   aCloseSell=false;
   
   MqlRates r[];
   ArraySetAsSeries(r,true);
   if(CopyRates(_Symbol,PERIOD_CURRENT,0,4,r)==-1)return(false);

   bool Insider=(r[1].high<=r[2].high && r[1].low>=r[2].low);
   bool Insider2=(r[2].high<=r[3].high && r[2].low>=r[3].low);
   bool Black_insider=(r[1].high<=r[2].high && r[1].low>=r[2].low && r[1].close<=r[1].open);
   bool White_insider=(r[1].high<=r[2].high && r[1].low>=r[2].low && r[1].close>r[1].open);
   bool White_bird=(White_insider && r[2].close>r[2].open);
   bool Black_bird=(Black_insider && r[2].close<r[2].open);
   MqlDateTime dt;
   
   double Zig_level_buy,Zig_level_sell;
  
         if(r[1].open<r[1].close){
            Zig_level_buy=(r[1].close-(r[1].close-r[1].open)/3);
         }
         else{
            Zig_level_buy=(r[1].close-(r[1].close-r[1].low)/3);
         }
      
         if(r[1].open>r[1].close){
            Zig_level_sell=(r[1].close+(r[1].open-r[1].close)/3);
         }
         else{
            Zig_level_sell=(r[1].close+(r[1].high-r[1].close)/3);
         }

         TimeToStruct(TimeCurrent(),dt);
  
         bool Confirm_buy=((r[0].low<=Zig_level_buy || dt.hour>=8) && !Black_bird && !White_insider);
         bool Confirm_sell=((r[0].high>=Zig_level_sell || dt.hour>=8) && !White_bird && !Black_insider);
         bool Buy_signal=(r[0].high>r[1].high);
         bool Sell_signal=(r[0].low<r[1].low);
         
         if(Buy_signal && Confirm_buy && r[0].low>r[1].low && r[1].low<r[2].high){
            aBuy=true;
         }

         if(Sell_signal && Confirm_sell && r[0].high<r[1].high && r[1].high>r[2].low){
            aSell=true;
         }
         
         if(Sell_signal && Confirm_sell && r[0].high<r[1].high){
            aCloseBuy=true;
         }
         if(Buy_signal && Confirm_buy && r[0].low>r[1].low){
            aCloseSell=true;
         }         
    
   return(true);
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
   return(true);
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