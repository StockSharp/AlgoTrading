//+------------------------------------------------------------------+
//|                                                SmartAssTrade.mq4 |
//|                 Copyright 2014,  Roy Philips Jacobs ~ 25/01/2014 |
//|                            http://www.gol2you.com ~ Forex Videos |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014,  Roy Philips Jacobs ~  ~ Last Update 03/03/2014"
#property link      "http://www.gol2you.com ~ Forex Videos"
//---
#include <stderror.mqh>
#include <stdlib.mqh>
//--- User Input
extern string      SmartAssTrade = "Copyright © 2014 3RJ ~ Roy Philips-Jacobs";
extern bool              Hedging = False; // If True, EA will open order BUY and SELL according to the trend change.
extern string   OptimizationLots = "Set LotsOptimization=True, Lots=0.0"; 
extern bool     LotsOptimization = False; // If True, Lots wil calculation by EA
extern double               Lots = 1.0; // If LotsOptimization=False, Lots adjusted by user
extern string  AutomaticSystemTP = "Set AutomaticTakeProfit=True or False";
extern bool  AutomaticTakeProfit = True; // TP will calculation by EA and Automatic TP by EA
extern bool  NoMinimumTakeProfit = False; // True or False -> If Set True, 100% TP by EA not use minimum TP.
extern string    MinimumSystemTP = "If Set NoMinimumTakeProfit=False"; // TP by EA on minimum TP values
extern double          MinimumTP = 15; // Minimum TP by EA on the AutomaticTakeProfit=True function.
extern string     ManualSystemTP = "If Set AutomaticTakeProfit=False"; // TP by System MT4 (same as manual trading)
extern double         TakeProfit = 25; // TP by System, values can adjust by user
extern string  AutomaticSystemSL = "Set AutomaticStopLoss=True";
extern bool    AutomaticStopLoss = True; // SL will calculation by EA
extern string     ManualSystemSL = "If Set AutomaticStopLoss=False"; // SL values can adjusted by user
extern double           StopLoss = 350; // SL adjusted by user
//----
//---- Global scope
double lot,pp,digit;
double sbb,BBlt0,BBlb0;
double lastAsk,lastBid;
double mrg,ask,bid,sl,tp;
double slA,slB,tpA,tpB,tpAS,tpBS;
double difB,difS,pBo,pSo,pdifB,pdifS;
//---
bool mt4ecn,ohdg;
bool NoOrder,nomintp;
bool lotopt,opttp,autsl;
bool mdHC,mdLC,mdUp,mdDn;
bool SignalBuy,SignalSell;
//---
int TFX[]={5,15,30}; // Timeframes OsMA && MA
//---
int spg=0;
int tfm=60;
int totfr,Mnt;
int total,totalord,totalpft,totalhst;
int dev,hb,hs,ob,os,i,ip,is,it,nx,ox;
//---
string symbol;
string CopyR;
//----    
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {      
//----  
   symbol = Symbol(); 
   CopyR = "Copyright © 2014 3RJ ~ Roy Philips-Jacobs";
//---- 
   // Checking the Digits Point
   digit=Digits;
   if (digit==3 || digit==5)
      {pp=Point*10; dev=pp*spg*10; mt4ecn=true;}
   else {pp=Point; dev=pp*spg; mt4ecn=false;}
//----
   difB=0.8;
   difS=0.8;
   nx=0;
   ox=1;
   totfr=1;
   hb=0;
   hs=0;
//---- 
   lotopt=LotsOptimization;
   opttp=AutomaticTakeProfit;
   autsl=AutomaticStopLoss;
   nomintp=NoMinimumTakeProfit;
   ohdg=Hedging;
//---- initialization done
   return(0);
  }
//+------------------------------------------------------------------+
//| expert proccess working function                                 |
//+------------------------------------------------------------------+
//----
int LotOpt() //--function: calculation Optimization Lots
   {
      if (lotopt) 
         {
           //----
           mrg = AccountBalance();
           if (mrg < 1000) {lot = 0.1;} 
           if ((mrg > 1000) && (mrg < 5000)) {lot = 0.1;}
           if ((mrg > 5000) && (mrg < 9000)) {lot = 0.2;}
           if ((mrg > 9000) && (mrg < 13000)) {lot = 0.3;}
           if ((mrg > 13000) && (mrg < 17000)) {lot = 0.4;}
           if ((mrg > 17000) && (mrg < 21000)) {lot = 0.5;}
           if ((mrg > 21000) && (mrg < 25000)) {lot = 0.6;}
           if ((mrg > 25000) && (mrg < 29000)) {lot = 0.7;}
           if ((mrg > 29000) && (mrg < 33000)) {lot = 0.8;}
           if ((mrg > 33000) && (mrg < 37000)) {lot = 0.9;}
           if ((mrg > 41000) && (mrg < 45000)) {lot = 1.0;}
           if (mrg > 45000) {lot = 1.5;}
         }
      else lot = Lots;
      return(lot);
      //----
   } //-end LotOpt()
//---------//

int OptSL() //--function: calculation Stop Loss 
   {   
      if (!autsl) {sl=StopLoss*pp;}
      else {sl=180*pp;}
      return(sl);
      //----
   } //-end OptSL()
//---------//

int CkOpen() //--function: CheckOpenTrade.
   {
      //----
      totalord = OrdersTotal();
      ob=0; os=0;
      //----
      for (i=0; i<totalord; i++)   
          {
             if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == True)
                {
                   if (OrderSymbol() == symbol)
                      {
                         if (OrderType() == OP_BUY) {ob++; hb++;}
                         if (OrderType() == OP_SELL) {os++; hs++;}
                      }
                }
          }
     //----     
     return(ob);
     return(os);
     return(hb);
     return(hs);
     //----
   } //-end CkOpen()
//---------//

int CkClose() //--function: CheckOrderClose.
   {
      //----
      CkOpen();
      totalhst = OrdersHistoryTotal();
      //----
      for (i=0; i<totalhst; i++)   
          {
             if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY) == True)
                {
                   if (OrderSymbol() == symbol)
                      {
                        if (OrderType() == OP_BUY)
                           {
                             RefreshRates();
                             datetime oct=OrderCloseTime();
                             if (hb>0 && ob==0 && oct>0)
                                {
                                  Print("-----CLOSED BUY ORDER ",symbol," - Instant Close By System..OK!");
                                  PlaySound("ping.wav");
                                  hb=0;
                                  ox=1;
                                  totfr=1;
                                  break;
                                }                             
                           }
                         //--
                         if (OrderType() == OP_SELL)
                            {
                              RefreshRates();
                              oct=OrderCloseTime();
                              if (hs>0 && os==0 && oct>0)
                                {
                                  Print("-----CLOSED SELL ORDER ",symbol," - Instant Close By System..OK!");
                                  PlaySound("ping.wav");
                                  hs=0;
                                  ox=1;
                                  totfr=1;
                                  break;
                                }                                
                           }                         
                      }
                }
          }
     //----
     return(hb);
     return(hs);
     return(ox);
     return(totfr);
     //----
   } //-end CkClose()
//---------//

int CalcProvit() //--function: for calculation Automatic Take Profit.
   { 
      //----
      RefreshRates();
      BBlt0=iBands(symbol,tfm,20,2,0,4,1,0);
      BBlb0=iBands(symbol,tfm,20,2,0,4,2,0);
      sbb=BBlt0-BBlb0;
      //----
      if (opttp && nomintp)
         {      
           if (sbb<=pp*40) {tpAS=sbb/2; tpBS=sbb/2;}
           else if (sbb>pp*40 && sbb<=pp*70) {tpAS=sbb/3; tpBS=sbb/3;}
           else {tpAS=sbb/4; tpBS=sbb/4;}
         }
      else if (opttp && !nomintp) {tpAS=pp*MinimumTP; tpBS=pp*MinimumTP;}
      else {tp=TakeProfit*pp;}
      //--
      RefreshRates();
      pBo = High[0]-Close[0];
      pSo = Close[0]-Low[0];
      //--
      pdifB = pp*difB;
      pdifS = pp*difS;
      mdHC = (pBo>pdifB);
      mdLC = (pSo>pdifS);
      mdUp = (Close[0]<(lastBid-pdifB));
      mdDn = (Close[0]>(lastAsk+pdifS));
      //--
      return(tp);
      return(tpAS);
      return(tpBS);
      return(mdHC);
      return(mdLC);
      return(mdUp);
      return(mdDn);
      //----
   } //-end CalcProvit()
//---------/

int SmartAss() //--function: Check trend and calculation order.
   {
     //----
     SignalBuy=false;
     SignalSell=false;
     bool Upward;
     bool OrHdg;
     int upm,dnm;
     int osb,oss,x;
     ResetLastError();
     RefreshRates();
     //--
     for(x=0;x<3;x++)
       {
          if (iOsMA(symbol,TFX[x],12,26,9,6,0)>iOsMA(symbol,TFX[x],12,26,9,6,1))
             {osb++;}
          if (iOsMA(symbol,TFX[x],12,26,9,6,0)<iOsMA(symbol,TFX[x],12,26,9,6,1))
             {oss++;}
          //--
          if (iMA(symbol,TFX[x],20,0,0,0,0)>iMA(symbol,TFX[x],20,0,0,1,0))
             {upm++;}
          else {dnm++;}
       }
     //--
     if (osb==3 && upm>=2) {Upward=true;}
        
     if (oss==3 && dnm>=2) {Upward=false;}
     //--
     RefreshRates();
     double wprr=100-MathAbs(iWPR(symbol,1440,26,0));
     bool wprmb=wprr<98;
     bool wprms=wprr>2;
     //----
     CkOpen();
     Mnt=Minute();
     //--
     if (ohdg) {OrHdg=(ob+os<2) && (ox+totfr<2) && (MathMod(Mnt,15)>2);}
     else {OrHdg=(ob+os==0) && (ox+totfr==0) && (MathMod(Mnt,15)>2);}
     //--
     //----
     if (OrHdg && NoOrder==false)
        {
           if (Upward==true && wprmb) {SignalBuy=true;}
           //---
           if (Upward==false && wprms) {SignalSell=true;}
        }
     //----
     if (SignalBuy) {OrdBuy();} // New Order BUY
     //--
     if (SignalSell) {OrdSell();} // New Order SELL
     //--
     return(0);
     //----
   } //--end SmartAss()
//---------//

int OrdBuy() //--function: Open order if price was up
   {
      //---- 
      CkOpen();
      LotOpt();
      OptSL();
      CalcProvit();
      //----
      if (ob<1)
         {
            // Error checking
            if ((AccountFreeMarginCheck(symbol,OP_BUY,lot) <= 0) || (GetLastError()==134))
               {Print("-----OPEN NEW BUY ORDER ",symbol," - NOT ENOUGH MONEY..!"); ResetLastError(); return(0);}
            else  
               {      
                  ResetLastError();
                  int error;
                  bool result;
                  RefreshRates();
                  ask = MarketInfo(symbol,MODE_ASK);
                  lastBid = ask;
                  if (!autsl) {slA=ask-sl;}
                  else {slA=iLow(symbol,1440,0)-sl;}
                  if (opttp) {tpA=0;}
                  if (!opttp) {tpA=ask+tp;}
                  //--
                  if (mt4ecn==true) // If MT4 ECN
                     {
                       result=OrderSend(symbol,OP_BUY,lot,ask,dev,0,0);
                       if(result!=TRUE) {error=GetLastError();}
                       if (error>0) 
                          {
                            ResetLastError();
                            Sleep(2000);
                            RefreshRates(); 
                            ask = MarketInfo(symbol,MODE_ASK);
                            lastBid = ask;
                            result=OrderSend(symbol,OP_BUY,lot,ask,dev,0,0);
                            error=GetLastError();
                            if (result==true && error==0) {Sleep(2000); RefreshRates(); modif();}
                            else 
                              {
                                Print("Failed to Open New BUY ORDER ",symbol,"! Error code = ",
                                GetLastError(), ", ",ErrorDescription(error));
                                ResetLastError();
                                return(0);
                              }                   
                          }
                     }
                  //--
                  if (mt4ecn==false)
                     {
                       result=OrderSend(symbol,OP_BUY,lot,ask,dev,slA,tpA);
                       if(result!=TRUE) {error=GetLastError();}
                       if (error>0) 
                          {
                            ResetLastError();
                            Sleep(2000);
                            RefreshRates(); 
                            ask = MarketInfo(symbol,MODE_ASK);
                            lastBid = ask;
                            if (!autsl) {slA=ask-sl;}
                            else {slA=iLow(symbol,1440,0)-sl;}
                            if (opttp) {tpA=0;}
                            if (!opttp) {tpA=ask+tp;}
                            result=OrderSend(symbol,OP_BUY,lot,ask,dev,slA,tpA);
                            error=GetLastError();
                            if (error>0)
                              {
                                Print("Failed to Open New BUY ORDER ",symbol,"! Error code = ",
                                GetLastError(), ", ",ErrorDescription(error));
                                ResetLastError();
                                return(0);
                              }                            
                          }
                     }
                  //--
                  if (error==0 && result==true)
                     {    
                       Print("-----SmartAssTrade has Opened New BUY ORDER ",symbol," - OK!");
                       PlaySound("gun.wav");
                       hb=1;
                       ox++;
                       totfr++;
                     }
               }
         }
      //----
      return(hb);
      return(ox);
      return(totfr);
      //----
   } //-end OrdBuy() 
//---------//

int OrdSell() //--function: Open order if price was down
   {
      //----   
      CkOpen();
      LotOpt();
      OptSL();
      CalcProvit();
      //----
      if (os<1)
         {
            // Error checking
            if ((AccountFreeMarginCheck(symbol,OP_SELL,lot) <= 0) || (GetLastError()==134))
               {Print("-----OPEN NEW SELL ORDER ",symbol," - NOT ENOUGH MONEY..!"); ResetLastError(); return(0);}
            else
               {      
                  ResetLastError();
                  int error;
                  bool result;
                  RefreshRates();
                  bid = MarketInfo(symbol,MODE_BID);
                  lastAsk = bid;
                  if (!autsl) {slB=bid+sl;}
                  else {slB=iHigh(symbol,1440,0)+sl;}
                  if (opttp) {tpB=0;}
                  if (!opttp) {tpB=bid-tp;}
                  //--  
                  if(mt4ecn==true) // If MT4 ECN 
                     {
                       result=OrderSend(symbol,OP_SELL,lot,bid,dev,0,0);
                       if(result!=true) {error=GetLastError();}
                       if (error>0) 
                          {
                            ResetLastError();
                            Sleep(2000);
                            RefreshRates();
                            bid = MarketInfo(symbol,MODE_BID);
                            lastAsk = bid;
                            result=OrderSend(symbol,OP_SELL,lot,bid,dev,0,0);
                            error=GetLastError();
                            if (result==true && error==0) {Sleep(2000); RefreshRates(); modif();}
                            else 
                              {
                                Print("Failed to Open New SELL ORDER ",symbol,"! Error code = ",
                                GetLastError(), ", ",ErrorDescription(error));
                                ResetLastError();
                                return(0);
                              }                            
                          }
                     }
                  //--                       
                  if (mt4ecn==false) 
                     {
                       result=OrderSend(symbol,OP_SELL,lot,bid,dev,slB,tpB);
                       if(result!=true) {error=GetLastError();}
                       if (error>0 || result!=true)
                          {
                            ResetLastError();
                            Sleep(2000);
                            RefreshRates(); 
                            bid = MarketInfo(symbol,MODE_BID);
                            lastAsk = bid;
                            if (!autsl) {slB=bid+sl;}
                            else {slB=iHigh(symbol,1440,0)+sl;}
                            if (opttp) {tpB=0;}
                            if (!opttp) {tpB=bid-tp;}
                            result=OrderSend(symbol,OP_SELL,lot,bid,dev,slB,tpB);
                            error=GetLastError();
                            if (error>0)
                              {
                                Print("Failed to Open New SELL ORDER ",symbol,"! Error code = ",
                                GetLastError(), ", ",ErrorDescription(error));
                                ResetLastError();
                                return(0);
                              }                            
                          }
                     }
                  //--
                  if (error==0 && result==true)
                     {    
                       Print("-----SmartAssTrade has Opened New SELL ORDER ",symbol," - OK!");
                       PlaySound("gun.wav");
                       hs=1;
                       ox++;
                       totfr++;
                     }
               }
         }
      //----
      return(hs);
      return(ox);
      return(totfr);
      //----
   } //-end OrdSell()
//---------//

int modif() //--function: OrderModify for MT4 ECN to put Order SL and TP
   {
      //----
      int merr;
      bool mrest;
      total=OrdersTotal();
      //----
      for (it=0; it<total; it++)
          {
             if (OrderSelect(it, SELECT_BY_POS, MODE_TRADES) == True)
                {
                   if (OrderSymbol() == symbol)
                      {
                         if (OrderType() == OP_BUY)
                            {
                               OptSL();
                               CalcProvit();
                               if (!opttp) {tpA=OrderOpenPrice()+tp;}
                               else {tpA = 0;}
                               if (!autsl) {slA = OrderOpenPrice()-sl;}
                               else {slA = iLow(symbol,1440,0)-sl;}
                               if ((!autsl && !opttp) && (OrderStopLoss()==0 && OrderTakeProfit()==0))  
                                  {mrest=OrderModify(OrderTicket(),OrderOpenPrice(),slA,tpA,0);}
                               else if ((!autsl && opttp) && (OrderStopLoss()==0))
                                  {mrest=OrderModify(OrderTicket(),OrderOpenPrice(),slA,OrderTakeProfit(),0);}
                               else if ((!opttp && autsl) && (OrderTakeProfit()==0))
                                  {mrest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),tpA,0);}
                               else {break; return(0);}
                               if(mrest!=true) {merr=GetLastError();}
                               if (merr>0)
                                  {
                                    ResetLastError();
                                    Sleep(2000);
                                    RefreshRates();
                                    mrest=OrderModify(OrderTicket(),OrderOpenPrice(),slA,tpA,0);
                                    merr=GetLastError();
                                    if (merr>0) 
                                     {
                                       Print("Failed to Modify BUY ORDER ",symbol,"! Error code = ",
                                       GetLastError(), ", ",ErrorDescription(merr));
                                       ResetLastError();
                                       return(0);
                                     }                                    
                                  }
                            }
                         //----
                         if (OrderType() == OP_SELL)
                            {
                               OptSL();
                               CalcProvit();
                               if (!opttp) {tpB=OrderOpenPrice()-tp;}
                               else {tpB = 0;}
                               if (!autsl) {slB = OrderOpenPrice()+sl;}
                               else {slB = iHigh(symbol,1440,0)+sl;}
                               if ((!autsl && !opttp) && (OrderStopLoss()==0 && OrderTakeProfit()==0))
                                  {mrest=OrderModify(OrderTicket(),OrderOpenPrice(),slB,tpB,0);}
                               else if ((!autsl && opttp) && (OrderStopLoss()==0))
                                  {mrest=OrderModify(OrderTicket(),OrderOpenPrice(),slB,OrderTakeProfit(),0);}
                               else if ((!opttp && autsl) && (OrderTakeProfit()==0))
                                  {mrest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),tpB,0);}
                               else {break; return(0);}
                               if(mrest!=true) {merr=GetLastError();}
                               if (merr>0)
                                  {
                                    ResetLastError();
                                    Sleep(2000);
                                    RefreshRates();
                                    mrest=OrderModify(OrderTicket(),OrderOpenPrice(),slB,tpB,0);
                                    merr=GetLastError();
                                    if (merr>0)
                                     {
                                       Print("Failed to Modify SELL ORDER ",symbol,"! Error code = ",
                                       GetLastError(), ", ",ErrorDescription(merr));
                                       ResetLastError();
                                       return(0);
                                     }
                                  }
                            }
                      }
                //----
                }
          }
       return(mrest);
      //----
   } //-end modif()
//---------//

int CkProfitN() //--function: Check if order is profit.
   {
      //----
      int error;
      bool result;
      totalpft=OrdersTotal();
      for (ip=0; ip<totalpft; ip++)   
          {
             if (OrderSelect(ip, SELECT_BY_POS, MODE_TRADES) == True)
                {
                   if (OrderSymbol() == symbol)
                      {
                         if (OrderType() == OP_BUY)
                            {
                              CalcProvit();
                              //----
                              RefreshRates();
                              bid = MarketInfo(symbol,MODE_BID);
                              if ((OrderOpenPrice() < (bid-tpBS)) && mdHC) //-check if order buy is profit
                                 {
                                    RefreshRates();
                                    bid = MarketInfo(symbol,MODE_BID);
                                    result=OrderClose(OrderTicket(),OrderLots(),bid,dev);
                                    if(result!=true) {error=GetLastError();}
                                    if (error>0)
                                       {
                                         ResetLastError();
                                         RefreshRates();
                                         bid = MarketInfo(symbol,MODE_BID);
                                         result=OrderClose(OrderTicket(),OrderLots(),bid,dev);
                                       }
                                    if(error==0 && result==true)
                                      {
                                        if (opttp && !nomintp)
                                        {Print("-----SmartAssTrade has CLOSED BUY ORDER ",symbol," - Minimum Take profit..OK!");}
                                        else {Print("-----SmartAssTrade has CLOSED BUY ORDER ",symbol," - Automatic Take profit..OK!");}
                                        PlaySound("ping.wav");
                                        ox=1;
                                        totfr=1;
                                      }
                                 } //-end BUY Order Profit.
                              //----
                            }
                         //----
                         if (OrderType() == OP_SELL)
                            {
                              CalcProvit();
                              //----                            
                              RefreshRates();
                              ask = MarketInfo(symbol,MODE_ASK);
                              if ((OrderOpenPrice() > (ask+tpAS)) && mdLC)  //-check if order sell is profit
                                 {   
                                    RefreshRates();
                                    ask = MarketInfo(symbol,MODE_ASK);
                                    result=OrderClose(OrderTicket(),OrderLots(),ask,dev);
                                    if(result!=true) {error=GetLastError();}
                                    if (error>0)
                                       {
                                         ResetLastError();
                                         RefreshRates();
                                         ask = MarketInfo(symbol,MODE_ASK);
                                         result=OrderClose(OrderTicket(),OrderLots(),ask,dev);
                                       }
                                    if(error==0 && result==true)
                                      {
                                        if (opttp && !nomintp)
                                        {Print("-----SmartAssTrade has CLOSED SELL ORDER ",symbol," - Minimum Take profit..OK!");}
                                        else {Print("-----SmartAssTrade has CLOSED SELL ORDER ",symbol," - Automatic Take profit..OK!");}
                                        PlaySound("ping.wav");
                                        ox=1;
                                        totfr=1;      
                                      }
                                 } //-end SELL Order Profit.
                              //----                                                          
                            }
                      }
                }
          }
       //---
       return(ox);
       return(totfr);          
      //----
   } //-end CkProfitN()
//---------//

int CkProfitR() //--function: Check if order is profit.
   {
//----
      int error;
      bool result;
      totalpft = OrdersTotal();
      for (ip = 0; ip < totalpft; ip++)   
          {
             if (OrderSelect(ip, SELECT_BY_POS, MODE_TRADES) == True)
                {
                   if (OrderSymbol() == symbol)
                      {
                         if (OrderType() == OP_BUY)
                            {
                              CalcProvit();
                              //----
                              RefreshRates();
                              bid = MarketInfo(symbol,MODE_BID);
                              if ((OrderOpenPrice() < (bid-tpBS)) && mdUp) //-check if order is profit
                                 {
                                    RefreshRates();
                                    bid = MarketInfo(symbol,MODE_BID); 
                                    result=OrderClose(OrderTicket(),OrderLots(),bid,dev);
                                    if(result!=true) {error=GetLastError();}
                                    if (error>0)
                                       {
                                         ResetLastError();
                                         RefreshRates();
                                         bid = MarketInfo(symbol,MODE_BID);
                                         result=OrderClose(OrderTicket(),OrderLots(),bid,dev);
                                       }
                                    if(error==0 && result==true)
                                      {
                                        if (opttp && !nomintp)
                                        {Print("-----SmartAssTrade has CLOSED BUY ORDER ",symbol," - Minimum Take profit..OK!");}
                                        else {Print("-----SmartAssTrade has CLOSED BUY ORDER ",symbol," - Automatic Take profit..OK!");}
                                        PlaySound("eagle1.wav");
                                        ox=1;
                                        totfr=1;                  
                                      }
                                 } //-end BUY Order Profit.
                              //---- 
                            }
                         //----
                         if (OrderType() == OP_SELL)
                            {
                              CalcProvit();
                              //----
                              RefreshRates();
                              ask = MarketInfo(symbol,MODE_ASK);
                              if ((OrderOpenPrice() > (ask+tpAS)) && mdDn) //-check if order is profit
                                 {   
                                    RefreshRates();
                                    ask = MarketInfo(symbol,MODE_ASK);
                                    result=OrderClose(OrderTicket(),OrderLots(),ask,dev);
                                    if(result!=true) {error=GetLastError();}
                                    if (error>0)
                                       {
                                         ResetLastError();
                                         RefreshRates();
                                         ask = MarketInfo(symbol,MODE_ASK);
                                         result=OrderClose(OrderTicket(),OrderLots(),ask,dev);
                                       }
                                    if(error==0 && result==true)
                                      {
                                        if (opttp && !nomintp)
                                        {Print("-----SmartAssTrade has CLOSED SELL ORDER ",symbol," - Minimum Take profit..OK!");}
                                        else {Print("-----SmartAssTrade has CLOSED SELL ORDER ",symbol," - Automatic Take profit..OK!");}
                                        PlaySound("eagle1.wav");
                                        ox=1;
                                        totfr=1;
                                      }
                                 } //-end SELL Order Profit.
                              //----
                            }
                      }
                }
          }
       //---
       return(ox);
       return(totfr);           
//----
   } //-end CkProfitR()
//---------//

int StartTrading() //--function: start trading for calculation order every 15 minutes
   {
//----
     CkOpen();
     if ((ob>0||os>0) && opttp) {CkProfitN(); CkProfitR();}
     CkOpen();
     if ((nx==0) && (ob>0||os>0)) {modif(); nx=1;}
     CkOpen();
     if ((ob>0||os>0) && (!opttp || !autsl)) {modif();}
     RefreshRates();
     Mnt=Minute();
     if (Mnt==0)
       {
          ox=0;
          totfr=0;
          SmartAss();
       }
     //--
     else if (Mnt>8 && MathMod(Mnt,15)==0)
        {
           ox=0;
           totfr=0;
           SmartAss();
        }
     //--
     RefreshRates();
     CkClose();
     CkOpen();
     if (ob==0||os==0) {SmartAss();}
     //----
     return(0);
//---- 
   } //-end StartTrading()
//---------//

//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
    if (!opttp) {tp=TakeProfit*pp;}
    else {tp=MinimumTP*pp;}
    if (!autsl) {sl=StopLoss*pp;}
    else {sl=180*pp;}
    total=OrdersTotal();
    //----
    for (it=0; it<total; it++)
        {
           if (OrderSelect(it, SELECT_BY_POS, MODE_TRADES)==True)
              {
                 if (OrderSymbol()==symbol)
                    {
                       if (OrderType()==OP_BUY)
                          {
                             ResetLastError();
                             RefreshRates();
                             tpA=OrderOpenPrice()+tp;
                             slA=iLow(symbol,1440,0)-sl;
                             OrderModify(OrderTicket(),OrderOpenPrice(),slA,tpA,0);
                          }
                       //----
                       if (OrderType()==OP_SELL)
                          {
                             ResetLastError();
                             RefreshRates();
                             tpB=OrderOpenPrice()-tp;
                             slB=iHigh(symbol,1440,0)+sl;
                             OrderModify(OrderTicket(),OrderOpenPrice(),slB,tpB,0);
                          }                                                              
                    }
              //----
              }
        }
//----
   return(0);
//----
  } //-end deinit()
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  { 
//---- 
   if (SmartAssTrade!=CopyR) {return(0);}
//----
   RefreshRates();
   if (IsTradeAllowed()==false) {return(0);}
   else if (DayOfWeek()==0 || DayOfWeek()==6) {return(0);}
   else if (DayOfWeek()==1 && Hour()==0 && Minute()<17) {NoOrder=true; StartTrading();}
   else if (Hour()==0 && Minute()<17) {NoOrder=true; StartTrading();}
   else {NoOrder=false; StartTrading();}
   return(NoOrder);
//---- 
  } //-end start()
//---------//  
//+------------------------------------------------------------------+