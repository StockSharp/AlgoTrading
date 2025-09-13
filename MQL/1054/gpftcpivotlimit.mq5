//+------------------------------------------------------------------+
//|                                              gpfTCPivotLimit.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Rewritten from MQL4. Ссылка на оригинал - http://codebase.mql4.com/ru/320"
#property version   "1.00"

//--- input parameters
input double    Lots       =  0.1;     /*Lots*/       // order volume; when 0, the MaxR parameter is used
input bool      SndMl      =  true;    /*SndMl*/      // sends messages by e-mail when the Expert Advisor opens and closes positions
input double    DcF        =  3;       /*DcF*/        // lot reduction factor at losses. If the value is 0, the reduction is not carried out. The lower the value, the greater the lot reduction. If the lot cannot be reduced, the minimum lot is used.
input double    MaxR       =  0.02;    /*MaxR*/       // maximum risk from 0-1 (share of free funds). It is effective when the Lots value is 0
input int       TgtProfit  =  4;       /*TgtProfit*/  // variants of used the levels (1-5) as exemplified by a buy position: 1 - open based on Support1, Stop Loss at Support2, Take Profit at Resist1; 2 - open based on Support1, Stop Loss at Support2, Take Profit at Resist2; 3 - open based on Support2, Stop Loss at Support3, Take Profit at Resist1; 4 - open based on Support2, Stop Loss at Support3, Take Profit at Resist2; 5 - open based on Support2, Stop Loss at Support3, Take Profit at Resist3.
input bool      isTradeDay =  false;   /*isTradeDay*/ // intraday trade only (close the position at 23:00)
input bool      ModSL      =  false;   /*ModSL*/      // modify the Stop Loss when the first target is reached (the nearest level in the direction of the profit from the opening level)

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;

datetime ctm[1];
datetime LastTime;
double lot,slv,tpv,msl,mtp;
bool eres;     
string sHeaderLetter,sBodyLetter;
double Pivot,Resist1,Resist2,Resist3,Support1,Support2,Support3,cl1[1],cl2[1],hi2[1],lo2[1],op2[1],TargetBuy,TargetSell;
double sLevel,StopSell,ProfitSell,bLevel,StopBuy,ProfitBuy;
string gvp;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){
   
   gvp=MQL5InfoString(MQL5_PROGRAM_NAME)+"_"+_Symbol+"_"+IntegerToString(PeriodSeconds()/60)+"_"+IntegerToString(AccountInfoInteger(ACCOUNT_LOGIN));
   
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
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

   if(CopyTime(_Symbol,PERIOD_CURRENT,0,1,ctm)==-1){
      return;
   }
   
   bool ClPos=isCloseBuySell();
   
   if(ClPos){
      if(Pos.Select(_Symbol)){
         if(Pos.PositionType()==POSITION_TYPE_BUY){
            if(!Sym.RefreshRates())return;  
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3))return;
            MailCloseBuy();
          }
         if(Pos.PositionType()==POSITION_TYPE_SELL){
            if(!Sym.RefreshRates())return;  
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3))return;
            MailCloseSell();
         }         
      }
   }   
   
   if(ctm[0]!=LastTime){
      // Indicators
      if(!SolvePivot()){
         return;
      }     
      if(!ClPos){
         // Открытие
         if(!Pos.Select(_Symbol)){
            bool opb=isOpenBuy();
            bool ops=isOpenSell();
               // Покупка
               if(opb && !ops){ 
                  if(!Sym.RefreshRates())return;         
                  if(!LotsOptimized(lot))return;
                  SolveBuySLTP(slv,tpv);
                  SolveBuyMSLTP(msl,mtp);
                     if(ChekBuyMSL(slv,msl) && ChekBuyMTP(tpv,mtp)){
                        Trade.SetDeviationInPoints(Sym.Spread()*3);
                        if(!Trade.Buy(lot,_Symbol,0,slv,tpv,""))return;
                        GlobalVariableSet(gvp+"tg1",TargetBuy);
                        MailOpenBuy();
                     }
                     else{
                        SolveBuySLTP2(slv,tpv);
                           if(ChekBuyMSL(slv,msl) && ChekBuyMTP(tpv,mtp)){
                              Trade.SetDeviationInPoints(Sym.Spread()*3);
                              Print("Trying to open a buy position using the second variant sltp");
                              if(!Trade.Buy(lot,_Symbol,0,slv,tpv,""))return;
                              GlobalVariableSet(gvp+"tg1",TargetBuy);
                              MailOpenBuy();
                           }
                           else{
                              Print("Cannot open a Buy position, nearing the Stop Loss or Take Profit");
                           }                         
                     }         
               }
               // Buy
               if(ops && !opb){
                  if(!Sym.RefreshRates())return;         
                  if(!LotsOptimized(lot))return;
                  SolveSellSLTP(slv,tpv);
                  SolveSellMSLTP(msl,mtp);
                     if(ChekSellMSL(slv,msl) && ChekSellMTP(tpv,mtp)){
                        Trade.SetDeviationInPoints(Sym.Spread()*3);
                        if(!Trade.Sell(lot,_Symbol,0,slv,tpv,""))return;
                        GlobalVariableSet(gvp+"tg1",TargetSell);
                        MailOpenSell();
                                                 
                     }
                     else{
                        SolveSellSLTP2(slv,tpv);
                           if(ChekSellMSL(slv,msl) && ChekSellMTP(tpv,mtp)){
                              Trade.SetDeviationInPoints(Sym.Spread()*3);
                              Print("Trying to open a sell position using the second variant sltp");
                              if(!Trade.Sell(lot,_Symbol,0,slv,tpv,""))return;
                              GlobalVariableSet(gvp+"tg1",TargetSell);
                              MailOpenSell();
                                                       
                           }
                           else{
                              Print("Cannot open a Sell position, nearing the Stop Loss or Take Profit");
                           }                       
                     }          
               }
         }  
      }          
      LastTime=ctm[0];
   }
   
   CheckForSLMod();

}

//+------------------------------------------------------------------+
//|   Function for moving the Stop Loss to the breakeven when the first target    |
//|   is reached                                                           |
//+------------------------------------------------------------------+
void CheckForSLMod(){
   if(!ModSL)return;
   if(GlobalVariableCheck(gvp+"tg1")){
      if(Pos.Select(_Symbol)){
         if(!Sym.RefreshRates())return;  
         double tval=GlobalVariableGet(gvp+"tg1");
         double csl=NormalizeDouble(Pos.StopLoss(),_Digits);
         double ctp=NormalizeDouble(Pos.TakeProfit(),_Digits);
         double nsl;
            switch(Pos.PositionType()){
               case POSITION_TYPE_BUY:
                  if(Sym.Bid()>=tval){
                     nsl=NormalizeDouble(Pos.PriceOpen()+_Point*Sym.Spread(),_Digits);
                        if(nsl>csl){
                           SolveBuyMSLTP(msl,mtp);
                              if(ChekBuyMSL(nsl,msl)){
                                 Print("Modification of the Stop Loss for a buy position upon reaching the first target ("+DoubleToString(tval,_Digits)+")");
                                 if(Trade.PositionModify(_Symbol,nsl,ctp)){
                                    GlobalVariableDel(gvp+"tg1");
                                 }
                              }
                        }
                  }               
               break;
               case POSITION_TYPE_SELL:
                  if(Sym.Bid()<=tval){
                     nsl=NormalizeDouble(Pos.PriceOpen()-_Point*Sym.Spread(),_Digits);
                        if(nsl<csl || csl==0){
                           SolveSellMSLTP(msl,mtp);
                              if(ChekSellMSL(nsl,msl)){
                                 Print("Modification of the Stop Loss for a sell position upon reaching the first target ("+DoubleToString(tval,_Digits)+")");
                                 if(Trade.PositionModify(_Symbol,nsl,ctp)){
                                    GlobalVariableDel(gvp+"tg1");
                                 }                              
                              }
                        }
                  
                  }               
               break;
            }
      }
      else{
         GlobalVariableDel(gvp+"tg1");
      }
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

//+------------------------------------------------------------------+
//|   Pivot function                                                  |
//+------------------------------------------------------------------+
bool SolvePivot(){
   MqlRates r[1];
   if(CopyRates(_Symbol,PERIOD_D1,1,1,r)==-1 || 
      CopyClose(_Symbol,PERIOD_CURRENT,1,1,cl1)==-1 || 
      CopyClose(_Symbol,PERIOD_CURRENT,2,1,cl2)==-1 ||
      CopyHigh(_Symbol,PERIOD_CURRENT,2,1,hi2)==-1 ||
      CopyLow(_Symbol,PERIOD_CURRENT,2,1,lo2)==-1 ||
      CopyOpen(_Symbol,PERIOD_CURRENT,2,1,op2)==-1 
   ){
      return(false);
   }
   Pivot=(r[0].low+r[0].high+r[0].close)/3;
	Resist1=2.0*Pivot-r[0].low;
	Support1=2.0*Pivot-r[0].high;
	Resist2=Pivot+(Resist1-Support1);
	Support2=Pivot-(Resist1-Support1);
   Resist3=r[0].high+2.0*(Pivot-r[0].low);
   Support3=r[0].low-2.0*(r[0].high-Pivot);  
   Pivot    = NormalizeDouble(Pivot,_Digits);
   Resist1  = NormalizeDouble(Resist1,_Digits);
   Resist2  = NormalizeDouble(Resist2,_Digits);
   Resist3  = NormalizeDouble(Resist3,_Digits);
   Support1 = NormalizeDouble(Support1,_Digits);
   Support2 = NormalizeDouble(Support2,_Digits);
   Support3 = NormalizeDouble(Support3,_Digits); 
   
   switch (TgtProfit){
      case 1 : 
         sLevel      =  Resist1;
         StopSell    =  Resist2;
         ProfitSell  =  Support1;
         bLevel      =  Support1;
         StopBuy     =  Support2;
         ProfitBuy   =  Resist1;
         TargetSell  =  Pivot;
         TargetBuy   =  Pivot;  
      break;
      case 2 : 
         sLevel      =  Resist1;
         StopSell    =  Resist2;
         ProfitSell  =  Support2;
         bLevel      =  Support1;
         StopBuy     =  Support2;
         ProfitBuy   =  Resist2;
         TargetSell  =  Pivot;
         TargetBuy   =  Pivot;           
      break;
      case 3 : 
         sLevel      =  Resist2;
         StopSell    =  Resist3;
         ProfitSell  =  Support1;
         bLevel      =  Support2;
         StopBuy     =  Support3;
         ProfitBuy   =  Resist1;
         TargetSell  =  Resist1;
         TargetBuy   =  Support1;           
      break;
      case 4 : 
         sLevel      =  Resist2;
         StopSell    =  Resist3;
         ProfitSell  =  Support2;
         bLevel      =  Support2;
         StopBuy     =  Support3;
         ProfitBuy   =  Resist2;
         TargetSell  =  Resist1;
         TargetBuy   =  Support1;             
      break;
      case 5 : 
         sLevel      =  Resist2;
         StopSell    =  Resist3;
         ProfitSell  =  Support3;
         bLevel      =  Support2;
         StopBuy     =  Support3;
         ProfitBuy   =  Resist3;
         TargetSell  =  Resist1;
         TargetBuy   =  Support1;             
      break;
   }    
      
   return(true);   
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool isOpenBuy(){
   return(((lo2[0]<bLevel || cl2[0]<=bLevel) && op2[0]>bLevel) && cl1[0]>=bLevel);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                           |
//+------------------------------------------------------------------+
bool isOpenSell(){
   return(((hi2[0]>sLevel || cl2[0]>=sLevel) && op2[0]< sLevel) && cl1[0]<=sLevel);
}

//+------------------------------------------------------------------+
//|   Function for determining signals for closing                          |
//+------------------------------------------------------------------+
bool isCloseBuySell(){
      if(isTradeDay){
         MqlDateTime t;
         TimeToStruct(TimeCurrent(),t);
         return(t.hour==23);
      }      
   return(false);
}

//+------------------------------------------------------------------+
//|   Function for determining the Stop Loss and Take Profit for a buy position                  |
//+------------------------------------------------------------------+
void SolveBuySLTP(double & aSL,double & aTP){
   aSL=StopBuy;
   aTP=ProfitBuy;
} 

//+------------------------------------------------------------------+
//|   Function for determining the Stop Loss and Take Profit for a sell position                 |
//+------------------------------------------------------------------+
void SolveSellSLTP(double & aSL,double & aTP){
   aSL=StopSell;
   aTP=ProfitSell;
}   

//+------------------------------------------------------------------+
//|   Function for determining the Stop Loss and Take Profit for a buy position for the second       |
//|   attempt                                                        |
//+------------------------------------------------------------------+
void SolveBuySLTP2(double & aSL,double & aTP){
   aSL=Support2;
   aTP=Resist3;
} 

//+------------------------------------------------------------------+
//|   Function for determining the Stop Loss and Take Profit for a sell position for the second      |
//|   attempt                                                        |
//+------------------------------------------------------------------+
void SolveSellSLTP2(double & aSL,double & aTP){
   aSL=0;
   aTP=0;
   aSL=NormalizeDouble(Resist2+_Point*Sym.Spread(),_Digits);
   aTP=NormalizeDouble(Support3+_Point*Sym.Spread(),_Digits);
}   

//+------------------------------------------------------------------+
//|   Function for determining the minimum Stop Loss and Take Profit for a buy position      |
//+------------------------------------------------------------------+
void SolveBuyMSLTP(double & aMSL,double & aMTP){
   aMSL=NormalizeDouble(Sym.Bid()-_Point*(Sym.StopsLevel()+1),_Digits);
   aMTP=NormalizeDouble(Sym.Ask()+_Point*(Sym.StopsLevel()+1),_Digits);
}

//+------------------------------------------------------------------+
//|   Function for determining the minimum Stop Loss and Take Profit for a sell position     |
//+------------------------------------------------------------------+
void SolveSellMSLTP(double & aMSL,double & aMTP){
   aMSL=NormalizeDouble(Sym.Ask()+_Point*(Sym.StopsLevel()+1),_Digits);
   aMTP=NormalizeDouble(Sym.Bid()-_Point*(Sym.StopsLevel()+1),_Digits);
}

//+------------------------------------------------------------------+
//|   Function for checking the Stop Loss for a buy position                                  |
//+------------------------------------------------------------------+
bool ChekBuyMSL(double aSL,double aMSL){
   return(aSL==0 || aSL<aMSL);
}

//+------------------------------------------------------------------+
//|   Function for checking the Take Profit for a buy position                                  |
//+------------------------------------------------------------------+
bool ChekBuyMTP(double aTP,double aMTP){
   return(aTP==0 || aTP>aMTP);
}

//+------------------------------------------------------------------+
//|   Function for checking the Stop Loss for a sell position                                 |
//+------------------------------------------------------------------+
bool ChekSellMSL(double aSL,double aMSL){
   return(aSL==0 || aSL>aMSL);
}

//+------------------------------------------------------------------+
//|   Function for checking the Take Profit for a sell position                               |
//+------------------------------------------------------------------+
bool ChekSellMTP(double aTP,double aMTP){
   return(aTP==0 || aTP<aMTP);
}

//+------------------------------------------------------------------+
//|   Function for sending an e-mail upon closing of a buy position                   |
//+------------------------------------------------------------------+
void MailCloseBuy(){
   if(SndMl){
      sHeaderLetter = "Operation CLOSE BUY at" + _Symbol+"";
      sBodyLetter = "Close order Buy at "+ _Symbol + " for " + DoubleToString(Sym.Bid(),_Digits)+ ", and finish this Trade";
      sndMessage(sHeaderLetter, sBodyLetter);
   }
}

//+------------------------------------------------------------------+
//|   Function for sending an e-mail upon closing a sell position                   |
//+------------------------------------------------------------------+
void MailCloseSell(){
   if(SndMl){
      sHeaderLetter = "Operation CLOSE SELL at" + _Symbol+"";
      sBodyLetter = "Close order Sell at "+ _Symbol + " for " + DoubleToString(Sym.Ask(),_Digits)+ ", and finish this Trade";
      sndMessage(sHeaderLetter, sBodyLetter);
   }
}    

//+------------------------------------------------------------------+
//|   Function for sending an e-mail upon opening a buy position                   |
//+------------------------------------------------------------------+
void MailOpenBuy(){
   if(SndMl){
      sHeaderLetter = "Operation BUY by " + _Symbol+"";
      sBodyLetter = "Order Buy by "+ _Symbol + " at " + DoubleToString(Sym.Ask(),_Digits)+ ", and set stop/loss at " + DoubleToString(slv,_Digits)+"";
      sndMessage(sHeaderLetter, sBodyLetter);
   }
} 
   
//+------------------------------------------------------------------+
//|   Function for sending an email upon opening a sell position                   |
//+------------------------------------------------------------------+
void MailOpenSell(){
   if(SndMl){
      sHeaderLetter = "Operation SELL by " + _Symbol+"";
      sBodyLetter = "Order Sell by "+ _Symbol + " at " + DoubleToString(Sym.Bid(),_Digits)+ ", and set stop/loss at " + DoubleToString(slv,_Digits)+"";
      sndMessage(sHeaderLetter, sBodyLetter);
   }
}  