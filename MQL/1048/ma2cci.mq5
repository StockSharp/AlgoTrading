//+------------------------------------------------------------------+
//|                                                       MA2CCI.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Rewritten from MQL4. Link to the original publication - http://codebase.mql4.com/ru/217"
#property version   "1.00"

//--- input parameters
input int       FMa     =  4;    /*FMa*/     // fast лю period
input int       SMa     =  8;    /*SMa*/     // slow MA period
input int       PCCi    =  4;    /*PCCi*/    // CCI period
input int       pATR    =  4;    /*pATR*/    // ATR period for Stop Loss
input double    Lots    =  0.1;  /*Lots*/    // volume order; when 0, the MaxR parameter is used
input bool      SndMl   =  true; /*SndMl*/   // sends messages by e-mal when the Expert Advisor opens and closes positions
input double    DcF     =  3;    /*DcF*/     // lot reduction factor at losses. If the value is 0, the reduction is not carried out. The lower the value, the greater the lot reduction. If the lot cannot be reduced, the minimum lot is used.
input double    MaxR    =  0.02; /*MaxR*/    // Maximum risk from 0-1 (share of free funds). It is effective when the Lots value is 0

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;

int fh,sh,ch,ah;
datetime ctm[1];
datetime LastTime;
double   fma1[1],
         fma2[1],
         sma1[1],
         sma2[1],
         cci1[1],
         cci2[1],
         atr0[1];
double lot,slv,msl;
bool eres;     
string sHeaderLetter,sBodyLetter;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   fh=iMA(_Symbol,PERIOD_CURRENT,FMa,0,MODE_SMA,PRICE_CLOSE);
   sh=iMA(_Symbol,PERIOD_CURRENT,SMa,0,MODE_SMA,PRICE_CLOSE);
   ch=iCCI(_Symbol,PERIOD_CURRENT,PCCi,PRICE_CLOSE);
   ah=iATR(_Symbol,PERIOD_CURRENT,pATR);
   
   if(fh==INVALID_HANDLE||sh==INVALID_HANDLE||ch==INVALID_HANDLE||ah==INVALID_HANDLE){
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
   if(fh!=INVALID_HANDLE)IndicatorRelease(fh);
   if(sh!=INVALID_HANDLE)IndicatorRelease(sh);
   if(ch!=INVALID_HANDLE)IndicatorRelease(ch);
   if(ah!=INVALID_HANDLE)IndicatorRelease(ah);
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
      if(CopyBuffer(fh,0,1,1,fma1)==-1 ||
         CopyBuffer(fh,0,2,1,fma2)==-1 ||
         CopyBuffer(sh,0,1,1,sma1)==-1 ||
         CopyBuffer(sh,0,2,1,sma2)==-1 ||
         CopyBuffer(ch,0,1,1,cci1)==-1 ||
         CopyBuffer(ch,0,2,1,cci2)==-1 ||
         CopyBuffer(ah,0,1,1,atr0)==-1)
      {
         return;
      }
      // Closing
      if(Pos.Select(_Symbol)){
         if(fma1[0]<sma1[0] && fma2[0]>=sma2[0] && Pos.PositionType()==POSITION_TYPE_BUY){
            if(!Sym.RefreshRates()){
               return;  
            }
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3)){
               return;
            }
            if(SndMl){
               sHeaderLetter = "Operation CLOSE BUY at" + _Symbol+"";
               sBodyLetter = "Close order Buy at "+ _Symbol + " for " + DoubleToString(Sym.Bid(),_Digits)+ ", and finish this Trade";
               sndMessage(sHeaderLetter, sBodyLetter);
            }
         }
         if(fma1[0]>sma1[0] && fma2[0]<=sma2[0] && Pos.PositionType()==POSITION_TYPE_SELL){
            if(!Sym.RefreshRates()){
               return;  
            }         
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3)){
               return;
            }
            if(SndMl){
               sHeaderLetter = "Operation CLOSE SELL at" + _Symbol+"";
               sBodyLetter = "Close order Sell at "+ _Symbol + " for " + DoubleToString(Sym.Ask(),_Digits)+ ", and finish this Trade";
               sndMessage(sHeaderLetter, sBodyLetter);
            }
         }         
      }
      
      // Opening a Buy position
      if((fma1[0]>sma1[0] && fma2[0]<=sma2[0]) && (cci1[0]>0 && cci2[0]<=0) && !Pos.Select(_Symbol)){
         if(!Sym.RefreshRates())return;         
         if(!LotsOptimized(lot))return;
         slv=NormalizeDouble(Sym.Bid()-atr0[0],_Digits);
         msl=NormalizeDouble(Sym.Bid()-_Point*(Sym.StopsLevel()+1),_Digits);
            if(slv<msl){
               Trade.SetDeviationInPoints(Sym.Spread()*3);
               eres=Trade.Buy(lot,_Symbol,0,slv,0,"");
                  if(!eres){
                     return;
                  }
                  if(SndMl){
                     sHeaderLetter = "Operation BUY by " + _Symbol+"";
                     sBodyLetter = "Order Buy by "+ _Symbol + " at " + DoubleToString(Sym.Ask(),_Digits)+ ", and set stop/loss at " + DoubleToString(slv,_Digits)+"";
                     sndMessage(sHeaderLetter, sBodyLetter);
                  }
            }
            else{
               Print("Cannot open a Buy position, nearing the Stop Loss");
            }
      }            
      // Opening a Sell position
      if((fma1[0]<sma1[0] && fma2[0]>=sma2[0]) && (cci1[0]<0 && cci2[0]>=0) && !Pos.Select(_Symbol)){
         if(!Sym.RefreshRates())return;         
         if(!LotsOptimized(lot))return;
         slv=NormalizeDouble(Sym.Ask()+atr0[0],_Digits);
         msl=NormalizeDouble(Sym.Ask()+_Point*(Sym.StopsLevel()+1),_Digits);
            if(slv>msl){
               Trade.SetDeviationInPoints(Sym.Spread()*3);
               eres=Trade.Sell(lot,_Symbol,0,slv,0,"");
                  if(!eres){
                     return;
                  }
                  if(SndMl){
                     sHeaderLetter = "Operation SELL by " + _Symbol+"";
                     sBodyLetter = "Order Sell by "+ _Symbol + " at " + DoubleToString(Sym.Bid(),_Digits)+ ", and set stop/loss at " + DoubleToString(slv,_Digits)+"";
                     sndMessage(sHeaderLetter, sBodyLetter);
                  }
            }
            else{
               Print("Cannot open a Sell position, nearing the Stop Loss");
            }            
      }
      LastTime=ctm[0];
   }
}

//+------------------------------------------------------------------+
//|   Function for determining the lot based on the trade results               |
//+------------------------------------------------------------------+
bool LotsOptimized(double & aLots){
      if(Lots==0){
         aLots=fLotsNormalize(AccountInfoDouble(ACCOUNT_FREEMARGIN)*MaxR/1000.0);        
      }
      else{
         aLots=Lots;         
      }
      if(DcF<=0){
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
         aLots=fLotsNormalize(aLots-aLots*losses/DcF);      
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
//|   Function for sending messages by e-mail                            |
//+------------------------------------------------------------------+
void sndMessage(string HeaderLetter, string BodyLetter){
   ResetLastError();
   int RetVal;
   SendMail(HeaderLetter,BodyLetter);
   RetVal = GetLastError();
   if(RetVal!=0)Print("Error "+IntegerToString(RetVal)+", failed to send the message");
}