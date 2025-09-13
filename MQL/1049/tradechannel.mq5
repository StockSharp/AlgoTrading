//+------------------------------------------------------------------+
//|                                                 TradeChannel.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Rewritten from MQL4. Link to the original - http://codebase.mql4.com/ru/226"
#property version   "1.00"

//--- input parameters

input double    Lots       =  0.1;  /*Lots*/       // the order volume, MaxrR parameter is used when the value is 0
input bool      SndMl      =  true; /*SndMl*/      // sends messages by e-mail when the Expert Advisor opens and closes positions
input double    DcF        =  3;    /*DcF*/        // lot reduction factor at losses. If the value is 0, the reduction is not carried out. The lower the value, the greater the lot reduction. If the lot cannot be reduces, the minimum lot is used.
input double    MaxR       =  0.02; /*MaxR*/       // maximum risk from 0-1 (share of free funds). It is used when Lots value is 0
input int       pATR       =  4;    /*pATR*/       // ATR period for Stop Loss
input int       rChannel   =  20;   /*rChannel*/   // price channel period
input int       Trailing   =  300;  /*Trailing*/   // trailing level; if the value is 0, the trailing is off

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;

int ah;
double h1[];
double h2[];
double l1[];
double l2[];
double cl1[1];
double hp1[1];
double lp1[1];
double atr0[1];
double Resist,ResistPrev,Support,SupportPrev,Pivot;
datetime ctm[1];
datetime LastTime;
double lot,slv,msl;
bool eres;     
string sHeaderLetter,sBodyLetter;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   ArrayResize(h1,rChannel);
   ArrayResize(h2,rChannel);
   ArrayResize(l1,rChannel);
   ArrayResize(l2,rChannel);
   
   ah=iATR(_Symbol,PERIOD_CURRENT,pATR);
   
   if(ah==INVALID_HANDLE){
      Alert("Failed to load the indicator, try again");
      return(-1);
   }   
   
   if(!Sym.Name(_Symbol)){
      Alert("Failed to initialize CSymbolInfo, try again");    
      return(-1);
   }

   Print("Expert initialization complete");
   
   return(0);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason){
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
      if(!defPcChannel()){
         return;
      }     
      
      // Closing
      if(Pos.Select(_Symbol)){
         if(isCloseBuy() && Pos.PositionType()==POSITION_TYPE_BUY){
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
         if(isCloseSell() && Pos.PositionType()==POSITION_TYPE_SELL){
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
      
      // Opening
      if(!Pos.Select(_Symbol)){
         bool opb=isOpenBuy();
         bool ops=isOpenSell();
            // Buy
            if(opb && !ops){ 
               if(!Sym.RefreshRates())return;         
               if(!LotsOptimized(lot))return;
               slv=NormalizeDouble(Support-atr0[0],_Digits);
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
                     Print("Cannot open a Buy position, the Stop Loss is near");
                  }         
            }
            // Sell
            if(ops && !opb){
               if(!Sym.RefreshRates())return;         
               if(!LotsOptimized(lot))return;
               slv=NormalizeDouble(Resist+atr0[0]+(Sym.Ask()-Sym.Bid()),_Digits);
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
                     Print("Cannot open a Sell position, the Stop Loss is near");
                  }          
            }
      }            
      LastTime=ctm[0];
   }
   
   fSimpleTrailing();
   
}

//+------------------------------------------------------------------+
//| Simple Trailing function                                         |
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
         nsl=NormalizeDouble(Sym.Bid()-_Point*Trailing,_Digits);
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
         nsl=NormalizeDouble(Sym.Ask()+_Point*Trailing,_Digits);
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
//|   The function for determining the lot based on the trade results       |
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
//|   Function for sending messages by e-mail                            |
//+------------------------------------------------------------------+
void sndMessage(string HeaderLetter, string BodyLetter){
   ResetLastError();
   int RetVal;
   SendMail(HeaderLetter,BodyLetter);
   RetVal = GetLastError();
   if(RetVal!=0)Print("Error "+IntegerToString(RetVal)+", the message is not sent");
}

//+------------------------------------------------------------------+
//|   Price channel function                                         |
//+------------------------------------------------------------------+
bool defPcChannel(){
   if(CopyHigh(_Symbol,PERIOD_CURRENT,1,rChannel,h1)==-1 ||
      CopyHigh(_Symbol,PERIOD_CURRENT,2,rChannel,h2)==-1 ||
      CopyLow(_Symbol,PERIOD_CURRENT,1,rChannel,l1)==1 ||
      CopyLow(_Symbol,PERIOD_CURRENT,2,rChannel,l2)==-1 || 
      CopyClose(_Symbol,PERIOD_CURRENT,1,1,cl1)==-1 || 
      CopyHigh(_Symbol,PERIOD_CURRENT,1,1,hp1)==-1 ||
      CopyLow(_Symbol,PERIOD_CURRENT,1,1,lp1)==-1)
   {
      return(false);
   }      
   
   Resist=h1[ArrayMaximum(h1)];
   ResistPrev=h2[ArrayMaximum(h2)];
   Support=l1[ArrayMinimum(l1)];
   SupportPrev=l2[ArrayMinimum(l2)];
   Pivot=(Resist+Support+cl1[0])/3;
   
   Resist=NormalizeDouble(Resist,_Digits);
   ResistPrev=NormalizeDouble(ResistPrev,_Digits);
   Support=NormalizeDouble(Support,_Digits);
   SupportPrev=NormalizeDouble(SupportPrev,_Digits);
   
   cl1[0]=NormalizeDouble(cl1[0],_Digits);
   hp1[0]=NormalizeDouble(hp1[0],_Digits);
   lp1[0]=NormalizeDouble(lp1[0],_Digits);

   return(true);
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool isOpenBuy(){
   if(hp1[0]>=Resist && Resist==ResistPrev)return(true);
   if(cl1[0]<Resist && Resist==ResistPrev && cl1[0]>Pivot)return(true); 
   return(false);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                          |
//+------------------------------------------------------------------+
bool isOpenSell(){
   if(lp1[0]<=Support && Support==SupportPrev)return(true);
   if(cl1[0]>Support && Support==SupportPrev && cl1[0]<Pivot)return(true);
   return(false);
}

//+------------------------------------------------------------------+
//|   Function for determining the closing of buy signals                     |
//+------------------------------------------------------------------+
bool isCloseBuy(){
   if(hp1[0]>=Resist && Resist==ResistPrev)return(true); 
   return (false);
}

//+------------------------------------------------------------------+
//|   Function for determining the closing of sell signals                    |
//+------------------------------------------------------------------+
bool isCloseSell(){
   if(lp1[0]<=Support && Support==SupportPrev)return (true);
   return (false);
}

