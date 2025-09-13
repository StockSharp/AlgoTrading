//+------------------------------------------------------------------+
//|                                                        Kloss.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+

#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Expert rewritten from MQL4, the author is - http://www.mql4.com/ru/users/klopka, link to original - http://codebase.mql4.com/ru/3692"
#property version   "1.00"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;

input double                           Lots              =  0.1;           /*Lots*/          // Lot, MaximumRisk parameter works with zero value.
input double                           MaximumRisk       =  0.05;          /*MaximumRisk*/   // Risk (valid for Lots=0).
input int                              StopLoss          =  550;           /*StopLoss*/      // Stoploss in points, 0 - without stoploss.
input int                              TakeProfit        =  550;           /*TakeProfit*/    // Takeprofit in points, 0 - without takeprofit.
input bool                             RevClose          =  true;          /*RevClose*/      // Close the position by the opposite trade signal

input int                              MAPeriod          =  1;	            /*MAPeriod*/      // MA period
input ENUM_MA_METHOD                   MAMethod          =  MODE_LWMA;	   /*MAMethod*/      // MA method
input ENUM_APPLIED_PRICE               MAPrice           =  PRICE_TYPICAL;	/*MAPrice*/       // MA price
input int                              MAShift           =  5;	            /*MAShift*/       // Bar from which the MA value is taken for comparing with price
input int                              PShift            =  1;	            /*PShift*/        // Bar from which the price value is taken for comparing with MA

input int                              CCIPeriod         =  10;	         /*CCIPeriod*/     // CCI period
input ENUM_APPLIED_PRICE               CCIPrice          =  PRICE_WEIGHTED;/*CCIPrice*/      // CCI price
input int                              CCIDiffer         =  120;           /*CCIDiffer*/     // CCI level (in both directions from 0 to buy and sell)
input int                              CCIShift          =  0;	            /*CCIShift*/      // Bar on which the CCI is checked

input int                              StKPeriod         =  5;	            /*StKPeriod*/     // K stochastic period
input int                              StDPeriod         =  3;	            /*StDPeriod*/     // D stochastic period
input int                              StSPeriod         =  3;	            /*StSPeriod*/     // S stochastic period
input ENUM_MA_METHOD                   StMethod          =  MODE_SMA;	   /*StMethod*/      // Stochastic method
input ENUM_STO_PRICE                   StPrice           =  STO_LOWHIGH;	/*StPrice*/       // Stochastic price
input int                              StShift           =  0;	            /*StShift*/       // Bar on which the Stochastic is checked
input int                              StDiffer          =  20;            /*StDiffer*/      // Stochastic level (in both directions from 50 to buy and sell)
input int                              CommonShift       =  1;             /*CommonShift*/   // General shift (is added to the Shift variables of the all indicators)

input bool                             MWMode            =  true;          /*MWMode*/        // To put stoploss and takeprofit after opening the position



int ma_shift,p_shift,cci_shif,st_shift;
bool ZeroBar;

int MAHand=INVALID_HANDLE;
int CCIHand=INVALID_HANDLE;
int StHand=INVALID_HANDLE;

double MABuf[1];
double CCIBuf[1];
double StMBuf[1];
double StSBuf[1];
double cl[1];
   
//--- input parameters


int Handle=INVALID_HANDLE;

datetime ctm[1];
datetime LastTime;
double lot,slv,msl,tpv,mtp;
string gvp;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){
   
   ma_shift=MAShift+CommonShift;
   p_shift=PShift+CommonShift;
   cci_shif=CCIShift+CommonShift;
   st_shift=StShift+CommonShift;
   ZeroBar=(ma_shift==0 || p_shift==0 || cci_shif==0 || st_shift==0);

   // Preparation of global variables names
   gvp=MQL5InfoString(MQL5_PROGRAM_NAME)+"_"+_Symbol+"_"+IntegerToString(PeriodSeconds()/60)+"_"+IntegerToString(AccountInfoInteger(ACCOUNT_LOGIN));
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_DEMO)gvp=gvp+"_d";
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_REAL)gvp=gvp+"_r";
   if(MQL5InfoInteger(MQL5_TESTING))gvp=gvp+"_t";
   DeleteGV();

   // Loading indicators...
   
   MAHand=iMA(NULL,PERIOD_CURRENT,MAPeriod,0,MAMethod,MAPrice);
   CCIHand=iCCI(NULL,PERIOD_CURRENT,CCIPeriod,CCIPrice);
   StHand=iStochastic(NULL,PERIOD_CURRENT,StKPeriod,StDPeriod,StSPeriod,StMethod,StPrice);

   if(MAHand==INVALID_HANDLE || CCIHand==INVALID_HANDLE || StHand==INVALID_HANDLE){
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
   if(MAHand!=INVALID_HANDLE)IndicatorRelease(MAHand);
   if(CCIHand!=INVALID_HANDLE)IndicatorRelease(CCIHand);
   if(StHand!=INVALID_HANDLE)IndicatorRelease(StHand);
   DeleteGV();   
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

   if(!MWMode_F2())return;

   if(CopyTime(_Symbol,PERIOD_CURRENT,0,1,ctm)==-1){
      return;
   }
   if(ZeroBar || ctm[0]!=LastTime){
      
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
                     MWMode_F1(slv,tpv);
                        if(Trade.Buy(lot,_Symbol,0,slv,tpv,"")){
                           MWMode_F2();
                        }
                        else{
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
                     MWMode_F1(slv,tpv);
                        if(Trade.Sell(lot,_Symbol,0,slv,tpv,"")){
                           MWMode_F2();
                        }
                        else{
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
      CopyBuffer(MAHand,0,ma_shift,1,MABuf)==-1 ||
      CopyBuffer(CCIHand,0,cci_shif,1,CCIBuf)==-1 ||
      CopyBuffer(StHand,0,st_shift,1,StMBuf)==-1 ||
      CopyBuffer(StHand,1,st_shift,1,StSBuf)==-1 ||
      CopyClose(_Symbol,PERIOD_CURRENT,p_shift,1,cl)==-1
   )return(false);      

   return(true);
}
 
//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool SignalOpenBuy(){
   return(CCIBuf[0]<-CCIDiffer && StMBuf[0]<50-StDiffer && cl[0]>MABuf[0]);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                          |
//+------------------------------------------------------------------+
bool SignalOpenSell(){
   return(CCIBuf[0]>CCIDiffer && StMBuf[0]>50+StDiffer && cl[0]<MABuf[0]);
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

//+------------------------------------------------------------------+
//|   Pre-function for the MWMode mode                               |
//+------------------------------------------------------------------+
void MWMode_F1(double & aStopLossValue,double & aTakeProfitValue){
   if(!MWMode)return;
   GlobalVariableSet(gvp+"_mwm_sl",aStopLossValue);
   GlobalVariableSet(gvp+"_mwm_tp",aTakeProfitValue);
   aStopLossValue=0;
   aTakeProfitValue=0;
}

//+------------------------------------------------------------------+
//|   Main function for the MWMode mode                              |
//+------------------------------------------------------------------+
bool MWMode_F2(){
      if(!MWMode){
         return(true);
      }         
      if(GlobalVariableCheck(gvp+"_mwm_sl") || GlobalVariableCheck(gvp+"_mwm_tp")){
         if(Pos.Select(_Symbol)){
            if(Pos.StopLoss()==0 && Pos.TakeProfit()==0){
               slv=GlobalVariableGet(gvp+"_mwm_sl");
               tpv=GlobalVariableGet(gvp+"_mwm_tp");
               if(slv!=0 || tpv!=0){
                  if(!Sym.RefreshRates()){
                     return(false);
                  }              
                  switch(Pos.PositionType()){
                     case POSITION_TYPE_BUY:
                        if(slv!=0){
                           slv=MathMin(slv,Sym.NormalizePrice(Sym.Bid()-Sym.Point()*(Sym.Spread()+Sym.StopsLevel()+1)));
                        }
                        if(tpv!=0){
                           tpv=MathMax(tpv,Sym.NormalizePrice(Sym.Ask()+Sym.Point()*(Sym.StopsLevel()+1)));
                        }
                        if(Trade.PositionModify(_Symbol,slv,tpv)){
                           GlobalVariableDel(gvp+"_mwm_sl");
                           GlobalVariableDel(gvp+"_mwm_tp");
                        }
                        else{
                           return(false);
                        }
                     break;
                     case POSITION_TYPE_SELL:
                        if(slv!=0){
                           slv=MathMax(slv,Sym.NormalizePrice(Sym.Ask()+Sym.Point()*(Sym.Spread()+Sym.StopsLevel()+1)));
                        }
                        if(tpv!=0){
                           tpv=MathMin(tpv,Sym.NormalizePrice(Sym.Bid()-Sym.Point()*(Sym.StopsLevel()+1)));
                        }
                        if(Trade.PositionModify(_Symbol,slv,tpv)){
                           GlobalVariableDel(gvp+"_mwm_sl");
                           GlobalVariableDel(gvp+"_mwm_tp");
                        }
                        else{
                           return(false);
                        }
                     break;
                  }
               }
            }
         }
      }
   return(true);
}