//+------------------------------------------------------------------+
//|                                                 TradeChannel.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description ""
#property version   "1.00"
#property description "Expert rewritten from MQ4, the author is ExpertTrader (http://www.mql4.com/ru/users/ExpertTrader), link to original - http://codebase.mql4.com/ru/558"

/*
   The author: ExpertTrader, http://www.mql4.com/ru/users/ExpertTrader
   
   The original: http://codebase.mql4.com/ru/558
   
   How it works:
   
   GO parameter calculation using the following formula:
   
   GO=((C-O)+(H-O)+(L-O)+(C-L)+(C-H))*V;
   
   Where C, O, H, L - values ??of moving averages by prices Close, Open, High, Low. V - volume of the signal bar.
   
   If the GO value is greater than 0, the buying is open, if less, the buying is closed and the saling is opened. 

*/

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;


   
//--- input parameters
input double                           Lots              =  0.1;              /*Lots*/             // Lot, MaximumRisk parameter works with zero value.
input double                           MaximumRisk       =  0.05;             /*MaximumRisk*/      // Risk (valid for Lots=0).
input int                              Shift             =  1;                /*Shift*/            // Bar on which indicators are checked: 0 - shaped bar, 1 - the first shaped bar
input int                              MAPeriod          =  174;	            /*MAPeriod*/         // MA period
input int                              MAShift           =  0;	               /*MAShift*/          // MA shift
input ENUM_MA_METHOD                   MAMethod          =  MODE_EMA;	      /*MAMethod*/         // MA method
input ENUM_APPLIED_VOLUME              VolVolume         =  VOLUME_TICK;	   /*VolVolume*/        // Volume
input double                           OpenLevel         =  0;                /*OpenLevel*/        // If the GO value is greater than level, the buying is open, if less -OpenLevel - selling
input double                           CloseLevelDif     =  0;                /*CloseLevelDif*/    // The difference between the value of the level of opening and closing (a positive value, the closing level must be equal to or lower than the opening level)
input bool                             ShowGO            =  true;             /*ShowGO*/           // display the GO value in the chart comment



int HandleHigh=INVALID_HANDLE;
int HandleLow=INVALID_HANDLE;
int HandleClose=INVALID_HANDLE;
int HandleOpen=INVALID_HANDLE;
int HandleVolume=INVALID_HANDLE;

double o[1],h[1],l[1],c[1],v[1],GO;


datetime ctm[1];
datetime LastTime;
double lot,slv,msl,tpv,mtp;

int StopLoss    =  0;
int TakeProfit  =  0;


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   Comment("");

   // Loading indicators...
   
   HandleHigh     =  iMA(NULL,PERIOD_CURRENT,MAPeriod,MAShift,MAMethod,PRICE_HIGH);
   HandleLow      =  iMA(NULL,PERIOD_CURRENT,MAPeriod,MAShift,MAMethod,PRICE_LOW);
   HandleClose    =  iMA(NULL,PERIOD_CURRENT,MAPeriod,MAShift,MAMethod,PRICE_CLOSE);
   HandleOpen     =  iMA(NULL,PERIOD_CURRENT,MAPeriod,MAShift,MAMethod,PRICE_OPEN);
   HandleVolume   =  iVolumes(NULL,PERIOD_CURRENT,VolVolume);

   if(HandleHigh==INVALID_HANDLE || HandleLow==INVALID_HANDLE || HandleClose==INVALID_HANDLE || HandleOpen==INVALID_HANDLE || HandleVolume==INVALID_HANDLE){
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
   if(HandleHigh!=INVALID_HANDLE)IndicatorRelease(HandleHigh);
   if(HandleLow!=INVALID_HANDLE)IndicatorRelease(HandleLow);
   if(HandleClose!=INVALID_HANDLE)IndicatorRelease(HandleClose);
   if(HandleOpen!=INVALID_HANDLE)IndicatorRelease(HandleOpen); 
   if(HandleVolume!=INVALID_HANDLE)IndicatorRelease(HandleVolume);
   Comment("");
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
      
      if(ShowGO){
         Comment(DoubleToString(GO,4));
      }
      
      // Signals
      bool CloseBuy=SignalCloseBuy();
      bool CloseSell=SignalCloseSell();
      bool OpenBuy=SignalOpenBuy();
      bool OpenSell=SignalOpenSell();

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
               slv=SolveBuySL(StopLoss);
               tpv=SolveBuyTP(TakeProfit);
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
   if(
      CopyBuffer(HandleHigh,0,Shift,1,h)==-1 ||
      CopyBuffer(HandleLow,0,Shift,1,l)==-1 ||
      CopyBuffer(HandleClose,0,Shift,1,c)==-1 ||
      CopyBuffer(HandleOpen,0,Shift,1,o)==-1 ||
      CopyBuffer(HandleVolume,0,Shift,1,v)==-1
   ){
      return(false);
   } 
   GO=((c[0]-o[0])+(h[0]-o[0])+(l[0]-o[0])+(c[0]-l[0])+(c[0]-h[0]))*v[0];      
   return(true);
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool SignalOpenBuy(){
   return(GO>OpenLevel);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                          |
//+------------------------------------------------------------------+
bool SignalOpenSell(){
   return(GO<-OpenLevel);
}

//+------------------------------------------------------------------+
//|   Function for determining buy close signals                     |
//+------------------------------------------------------------------+
bool SignalCloseBuy(){
   return (GO<(OpenLevel-CloseLevelDif));
}

//+------------------------------------------------------------------+
//|   Function for determining sell close signals                    |
//+------------------------------------------------------------------+
bool SignalCloseSell(){
   return (GO>-(OpenLevel-CloseLevelDif));
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
      if(Lots==0){
         aLots=fLotsNormalize(AccountInfoDouble(ACCOUNT_FREEMARGIN)*MaximumRisk/1000.0);        
      }
      else{
         aLots=Lots;         
      }

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
