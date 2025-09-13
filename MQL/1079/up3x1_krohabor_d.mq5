//+------------------------------------------------------------------+
//|                                             up3x1_Krohabor_D.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Rewritten from MQL4. Link to the original publication - http://codebase.mql4.com/ru/337, author: izhutov (http://www.mql4.com/ru/users/izhutov)"
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

input double MaximumRisk        = 0.05;  /*MaximumRisk*/       // Risk (used if Lots=0)
input double Lots               = 0.1;   /*Lots*/              // Lot
input int    DecreaseFactor     = 0;     /*DecreaseFactor*/    // Lot reduction factor after losing trades. 0 - reduction disabled. The smaller the value, the greater the reduction. Where it is impossible to reduce the lot size, the minimum lot position is opened.
input int    TakeProfit         = 50;    /*TakeProfit*/        // Take Profit in points
input int    StopLoss           = 1100;  /*StopLoss*/          // Stop Loss in points
input int    TrailingStop       = 100;   /*TrailingStop*/      // Trailing Stop in points. If the value is 0, the Trailing stop function is disabled
input int    FastPeriod         = 24;    /*FastPeriod*/        // Fast 明 period
input int    FastShift          = 6;     /*FastShift*/         // Fast 明 shift
input int    MiddlePeriod       = 60;    /*MiddlePeriod*/      // Middle MA period
input int    MiddleShift        = 6;     /*MiddleShift*/       // Middle MA shift
input int    SlowPeriod         = 120;   /*SlowPeriod*/        // Slow 明 period
input int    SlowShift          = 6;     /*SlowShift*/         // Slow 明 shift


int ma14h,ma25h,ma36h;

double f1[1],f0[1],m1[1],m0[1],s1[1],s0[1];

datetime ctm[1];
datetime LastTime;
double lot,slv,tpv;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   ma14h=iMA(_Symbol,PERIOD_CURRENT,FastPeriod,FastShift,MODE_SMA,PRICE_CLOSE);
   ma25h=iMA(_Symbol,PERIOD_CURRENT,MiddlePeriod,MiddleShift,MODE_SMA,PRICE_CLOSE);
   ma36h=iMA(_Symbol,PERIOD_CURRENT,SlowPeriod,SlowShift,MODE_SMA,PRICE_CLOSE);

   if(ma14h==INVALID_HANDLE || ma25h==INVALID_HANDLE || ma36h==INVALID_HANDLE){
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
   if(ma14h!=INVALID_HANDLE)IndicatorRelease(ma14h);
   if(ma25h!=INVALID_HANDLE)IndicatorRelease(ma25h);
   if(ma36h!=INVALID_HANDLE)IndicatorRelease(ma36h);
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
      
      // Opening
      if(!Pos.Select(_Symbol)){
            // Buy
            if(OpBuy && !OpSell){ 
               if(!Sym.RefreshRates())return;         
               if(!LotsOptimized(lot))return;
               slv=NormalizeDouble(Sym.Ask()-_Point*StopLoss,_Digits);
               tpv=NormalizeDouble(Sym.Ask()+_Point*TakeProfit,_Digits);
               Trade.SetDeviationInPoints(Sym.Spread()*3);
               if(!Trade.Buy(lot,_Symbol,0,slv,tpv,""))return;
            }
            // Sell
            if(OpSell && !OpBuy){
               if(!Sym.RefreshRates())return;         
               if(!LotsOptimized(lot))return;
               slv=NormalizeDouble(Sym.Bid()+_Point*StopLoss,_Digits);
               tpv=NormalizeDouble(Sym.Bid()-_Point*TakeProfit,_Digits);
               Trade.SetDeviationInPoints(Sym.Spread()*3);
               if(!Trade.Sell(lot,_Symbol,0,slv,tpv,""))return;
            }
      }            
      LastTime=ctm[0];
   }
   fSimpleTrailing();
}

//+------------------------------------------------------------------+
//| Simple Trailing function                                       |
//+------------------------------------------------------------------+
void fSimpleTrailing(){
   if(TrailingStop<=0){
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
         nsl=NormalizeDouble(Sym.Bid()-_Point*TrailingStop,_Digits);
            if(nsl>=NormalizeDouble(Pos.PriceOpen(),_Digits)){
               if(nsl>NormalizeDouble(Pos.StopLoss(),_Digits)){
                  tmsl=NormalizeDouble(Sym.Bid()-_Point*Sym.StopsLevel(),_Digits);
                     if(nsl<tmsl){
                        Trade.PositionModify(_Symbol,nsl,Pos.TakeProfit());
                     }
               }
            }
      break;
      case POSITION_TYPE_SELL:
         nsl=NormalizeDouble(Sym.Ask()+_Point*TrailingStop,_Digits);
            if(nsl<=NormalizeDouble(Pos.PriceOpen(),_Digits)){
               psl=NormalizeDouble(Pos.StopLoss(),_Digits);
                  if(nsl<psl || psl==0){
                     tmsl=NormalizeDouble(Sym.Ask()+_Point*Sym.StopsLevel(),_Digits);
                        if(nsl>tmsl){
                           Trade.PositionModify(_Symbol,nsl,Pos.TakeProfit());
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
      CopyBuffer(ma14h,0,1,1,f1)==-1 ||
      CopyBuffer(ma14h,0,0,1,f0)==-1 ||

      CopyBuffer(ma25h,0,1,1,m1)==-1 ||
      CopyBuffer(ma25h,0,0,1,m0)==-1 ||
      
      CopyBuffer(ma36h,0,1,1,s1)==-1 || 
      CopyBuffer(ma36h,0,0,1,s0)==-1
   ){
      return(false);
   }   
   return(true);   
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool OpenBuy(){
   
   bool FastCrossMidUp=(f0[0]>m0[0] && f1[0]<m1[0]);
   bool FastMoreSlow=(f0[0]>s0[0] && f0[0]>s1[0] && f1[0]>s0[0] && f1[0]>s1[0]);
   bool MiddMoreSlow=(m0[0]>s0[0] && m0[0]>s1[0] && m1[0]>s0[0] && m1[0]>s1[0]);

   return(FastCrossMidUp && FastMoreSlow && MiddMoreSlow);

   /*

   The fast MA crosses the middle MA upwards.  
   
   The fast MA is above the slow MA, 
   the fast MA is above the slow MA from the previous bar, 
   the fast MA on the previous bar is above the slow MA, 
   the fast MA on the previous bar is above the slow MA from the previous bar.
   
   The middle MA is above the slow MA, 
   the middle MA is above the slow MA from the previous bar, 
   the middle MA on the previous bar is above the slow MA, 
   the middle MA on the previous bar is above the slow MA from the previous bar

   */
}   

//+------------------------------------------------------------------+
//|   Function for determining sell signals                           |
//+------------------------------------------------------------------+
bool OpenSell(){

   bool FastCrossMidDn=(f0[0]<m0[0] && f1[0]>m1[0]);
   bool FastLessSlow=(f0[0]<s0[0] && f0[0]<s1[0] && f1[0]<s0[0] && f1[0]<s1[0]);
   bool MiddLessSlow=(m0[0]<s0[0] && m0[0]<s1[0] && m1[0]<s0[0] && m1[0]<s1[0]);

   return(FastCrossMidDn && FastLessSlow && MiddLessSlow);

   /*

   The fast MA crosses the middle MA downwards.  
   
   The fast MA is below the slow MA, 
   the fast MA is below the slow MA from the previous bar, 
   the fast MA on the previous bar is below the slow MA, 
   the fast MA on the previous bar is below the slow MA from the previous bar.
   
   The middle MA is below the slow MA, 
   the middle MA is below the slow MA from the previous bar, 
   the middle MA on the previous bar is below the slow MA, 
   the middle MA on the previous bar is below the slow MA from the previous bar

   */
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
