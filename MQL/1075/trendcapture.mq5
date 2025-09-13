//+------------------------------------------------------------------+
//|                                                 TrendCapture.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Rewritten from MQL4. Link to the original publication - http://codebase.mql4.com/ru/353, author: Reshetov (http://www.mql4.com/ru/users/Reshetov)"
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

input double   Lots              =  0.1;     /*Lots*/          // Lot
input double   MaximumRisk       =  0.05;    /*MaximumRisk*/   // Risk (it is used if Lots=0)
input int      StopLoss          =  1800;    /*StopLoss*/      // Stop Loss in points
input int      TakeProfit        =  500;     /*TakeProfit*/    // Take Profit in points
input double   SARStep           =  0.02;    /*SARStep*/       // SAR step
input double   SARMax            =  0.2;     /*SARMax*/        // Maximum SAR step
input int      ADXPeriod         =  14;      /*ADXPeriod*/     // ADX period
input int      ADXLevel          =  20;      /*ADXLevel*/      // ADX level
input int      Shift             =  1;       /*Shift*/         // The bar on which the indicator values are checked
input int      BreakEven         =  50;      /*BreakEven*/     // Profit level of a position expressed in points in order to move the Stop Loss to the breakeven level. If the value is 0, the function is disabled 

int sh=INVALID_HANDLE,ah=INVALID_HANDLE;
double s0[1],a0[1],cl0[1];

datetime ctm[1];
datetime LastTime;
double lot,slv,tpv;
int ADir;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   sh=iSAR(_Symbol,PERIOD_CURRENT,SARStep,SARMax);
   ah=iADX(_Symbol,PERIOD_CURRENT,ADXPeriod);

   if(sh==INVALID_HANDLE || ah==INVALID_HANDLE){
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
   if(sh!=INVALID_HANDLE)IndicatorRelease(sh);
   if(ah!=INVALID_HANDLE)IndicatorRelease(ah);
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

      bool OpBuy=OpenBuy();
      bool OpSell=OpenSell();
      
      // Opening
      if(!Pos.Select(_Symbol)){
            // Buy
            if(OpBuy && !OpSell){ 
               if(!CheckAllowedDir(ADir))return;
                  if(ADir!=-1){
                     if(!Sym.RefreshRates())return;         
                     if(!LotsOptimized(lot))return;
                     slv=NormalizeDouble(Sym.Ask()-_Point*StopLoss,_Digits);
                     tpv=NormalizeDouble(Sym.Ask()+_Point*TakeProfit,_Digits);
                     Trade.SetDeviationInPoints(Sym.Spread()*3);
                     if(!Trade.Buy(Lots,_Symbol,0,slv,tpv,""))return;
                  }
            }
            // Sell
            if(OpSell && !OpBuy){
               if(!CheckAllowedDir(ADir))return;
                  if(ADir!=1){
                     if(!Sym.RefreshRates())return;         
                     if(!LotsOptimized(lot))return;
                     slv=NormalizeDouble(Sym.Bid()+_Point*StopLoss,_Digits);
                     tpv=NormalizeDouble(Sym.Bid()-_Point*TakeProfit,_Digits);
                     Trade.SetDeviationInPoints(Sym.Spread()*3);
                     if(!Trade.Sell(Lots,_Symbol,0,slv,tpv,""))return;
                  }                     
            }
      }            
      LastTime=ctm[0];
   }
   fSimpleBreakEven();
}

//+------------------------------------------------------------------+
//| Simple Trailing function                                       |
//+------------------------------------------------------------------+
void fSimpleBreakEven(){
   if(BreakEven<=0){
      return;
   }         
   if(!Pos.Select(_Symbol)){
      return;
   }         
   double op=ND(Pos.PriceOpen());
   double sl=ND(Pos.StopLoss());
   if(!Sym.RefreshRates()){
      return;  
   }   
   double nsl,msl;  
   switch(Pos.PositionType()){
      case POSITION_TYPE_BUY:
         if(sl<op){
            nsl=ND(Sym.Bid()-_Point*BreakEven); 
               if(nsl>=op){
                  msl=ND(Sym.Bid()-_Point*Sym.StopsLevel()); 
                     if(op<msl){
                        Trade.PositionModify(_Symbol,op,ND(Pos.TakeProfit()));
                     }
               }   
         }
      break;
      case POSITION_TYPE_SELL:
         if(sl>op || sl==0){
            nsl=ND(Sym.Ask()+_Point*BreakEven); 
               if(nsl<=op){
                  msl=ND(Sym.Ask()+_Point*Sym.StopsLevel()); 
                     if(op>msl){
                        Trade.PositionModify(_Symbol,op,ND(Pos.TakeProfit()));
                     }
               }   
         }    
      break;
   }
}

//+------------------------------------------------------------------+
//| Function for getting indicator values                           |
//+------------------------------------------------------------------+
bool Indicators(){
   if(
      CopyBuffer(sh,0,Shift,1,s0)==-1 ||
      CopyBuffer(ah,0,Shift,1,a0)==-1 ||
      CopyClose(_Symbol,PERIOD_CURRENT,Shift,1,cl0)==-1
   ){
      return(false);
   }   
   return(true);   
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool OpenBuy(){
   return(cl0[0]>s0[0] && a0[0]<ADXLevel);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                           |
//+------------------------------------------------------------------+
bool OpenSell(){
   return(cl0[0]<s0[0] && a0[0]<ADXLevel);
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
   return(true);      
}

//+------------------------------------------------------------------+
//|   Function for determining the permitted trade direction based on       |
//|   historical data                                                        |
//+------------------------------------------------------------------+
bool CheckAllowedDir(int & aDir){
   aDir=0;
      if(!HistorySelect(0,TimeCurrent())){
         return(false);
      }
      for(int i=HistoryDealsTotal()-1;i>=0;i--){
         if(!Deal.SelectByIndex(i))return(false);
         if(Deal.DealType()!=DEAL_TYPE_BUY && Deal.DealType()!=DEAL_TYPE_SELL)continue;
         if(Deal.Entry()!=DEAL_ENTRY_OUT)continue;
         if(Deal.Symbol()!=_Symbol)continue;
            if(Deal.Profit()>0){
               if(Deal.DealType()==DEAL_TYPE_SELL){
                  aDir=1;
               }
               else if(Deal.DealType()==DEAL_TYPE_BUY){
                  aDir=-1;
               }
            }
            else{
               if(Deal.DealType()==DEAL_TYPE_SELL){
                  aDir=-1;
               }
               else if(Deal.DealType()==DEAL_TYPE_BUY){
                  aDir=1;
               }            
            }
         break;
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

//+------------------------------------------------------------------+
//| Function for normalization of 'double' by Digits                            |
//+------------------------------------------------------------------+
double ND(double aValue){
   return(NormalizeDouble(aValue,_Digits));
}
