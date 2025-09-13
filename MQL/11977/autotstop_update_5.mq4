//+------------------------------------------------------------------+
//|                                                    AutoTStop.mq4 |
//|                        Copyright 2014, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//|       Create: 17/10/2014   http://www.gol2you.com ~ Forex Videos |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

/* Last Update (Update_01 ~ 2014-12-05).
   Last Update (Update_02 ~ 2015-01-05).
   Last Update (Update_03 ~ 2015-03-27).
   Last Update (Update_04 ~ 2015-06-13).
   Last Update (Update_05 ~ 2015-07-03).
*/
//--
#property description "AutoTStop EA does not work to open the order, but only help trader for"
#property description "optimization TakeProfit, put a BEP, StopLoss and implement TrailingStop."
//--- User Input
input string             AutoTStop = "Copyright © 2014 3RJ ~ created by Roy Philips-Jacobs";
extern bool            FridayTrade = True; // If True, EA still trading at Friday
extern bool        UseTrailingStop = True; // Use Trailing Stop, True (Yes) or False (Not)
extern bool       AutoTrailingStop = True; // Default Trailing Stop value = 24.0
extern double         TrailingStop = 24.0; // If Auto Trailing Stop False, input Trailing Stop Value
extern double     TrailingStopStep = 1.0; // Input Trailing Stop Step Value (default 1.0)
input string     AutomaticSystemTP = "Set AutomaticTakeProfit=True or False";
extern bool OptimizationTakeProfit = True; // TP will calculation by EA and Automatic TP by EA
extern bool    NoMinimumTakeProfit = True; // True or False -> If Set True, 100% TP by EA not use minimum TP.
input string       MinimumSystemTP = "If Set NoMinimumTakeProfit=False"; // TP by EA on minimum TP values
extern double            MinimumTP = 12; // Minimum TP by EA on the AutomaticTakeProfit=True function, default value 12
input string        ManualSystemTP = "If Set AutomaticTakeProfit=False"; // TP by Terminal MT4 (same as manual trading)
extern double           TakeProfit = 20; // TP by System, values can adjust by user, default 20
input string     AutomaticSystemSL = "Set AutomaticStopLoss=True or False";
extern bool      AutomaticStopLoss = True; // SL will calculation by EA
input string        ManualSystemSL = "If Set AutomaticStopLoss=False"; // SL values can adjusted by user
extern double             StopLoss = 108; // SL adjusted by user, default 108
//---
//--- Global scope
double pp,sl,tp;
double ask,bid,dev;
double lastAsk,lastBid;
double dvab,slA,slB,tpA,tpB,tpABS;
double difB,difS,pBo,pSo,pdifB,pdifS;
double tsstv,tstpv,trstart,bep,beplmt;
//---
bool nomintp;
bool fClB,fClS;
bool TrlStp,opttp,autsl;
bool mdHC,mdLC,mdUp,mdDn;
//---
int beh;
int bel;
int trxb;
int trxs;
int step;
int bar=100;
int stxb,stxs;
int hb,hs,ob,os,s,ip;
int totalord,totalpft,totalhst;
//---
string CopyR;
//----
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(void)
  {
//---
   CopyR="Copyright © 2014 3RJ ~ created by Roy Philips-Jacobs";
   if(AutoTStop!=CopyR) {return(0);}
//---
//-- Checking the Digits Point
   if(Digits==3 || Digits==5 || _Symbol=="XAUUSD" || _Symbol=="GOLD")
     {pp=Point*10;}
   else {pp=Point;}
//---
   beh=0;
   bel=0;
   stxb=0;
   stxs=0;
   trxb=0;
   trxs=0;
   difB=0.8;
   difS=0.8;
//---
   autsl=AutomaticStopLoss;
   opttp=OptimizationTakeProfit;
   nomintp=NoMinimumTakeProfit;
   TrlStp=UseTrailingStop;
//--
//--- initialization done
   return(INIT_SUCCEEDED);
  }
//----
//+------------------------------------------------------------------+
//| expert proccess working function                                 |
//+------------------------------------------------------------------+
//----
void CalcSTP() //-- function: for calculation Automatic Take Profit.
  {
//----
   RefreshRates();
   fClB=false;
   fClS=false;
   double spread=Ask-Bid;
   int tfr[]={30,240}; // timeframes arrays tp
   double bstp[]={24,48,72}; //arrays tp value
   double BSma2[],BSma6[];
   ArrayResize(BSma2,bar);
   ArrayResize(BSma6,bar);
   ArraySetAsSeries(BSma2,true);
   ArraySetAsSeries(BSma6,true);
   bep=NormalizeDouble(pp*8.9,Digits);
   beplmt=NormalizeDouble(bep+spread,Digits);
//-
   for(int fm=bar-1; fm>=0; fm--)
     {BSma2[fm]=iMA(_Symbol,0,2,0,1,4,fm);}
   for(int fn=bar-1; fn>=0; fn--)
     {BSma6[fn]=iMAOnArray(BSma2,0,6,0,3,fn);}
   double bsma0=BSma2[0]-BSma6[0];
   double bsma1=BSma2[1]-BSma6[1];
   if((bsma0>bsma1)) {bool stB=true;}
   if((bsma0<bsma1)) {bool stS=true;}
   if((!stB) && (!stS)) {bool stN=true;}
//--
//-
   if((opttp==true) && (nomintp==true))
     {
      //--
      if(_Period<tfr[0])
        {tp=NormalizeDouble((bstp[0]*pp),Digits);}
      else if((_Period>=tfr[0]) && (_Period<tfr[1]))
        {tp=NormalizeDouble((bstp[1]*pp),Digits);}
      else if((_Period>=tfr[1]))
        {tp=NormalizeDouble((bstp[2]*pp),Digits);}
      //-
      dvab=tp*0.5;
      //--
      if((stB==true)&&(!stN)) {fClS=true;}
      if((stS==true)&&(!stN)) {fClB=true;}
      //--
     }
//--
   else if((opttp==true) && (!nomintp))
     {
      //--
      if(_Period<tfr[0])
        {tp=NormalizeDouble((bstp[0]*pp),Digits);}
      else if((_Period>=tfr[0]) && (_Period<tfr[1]))
        {tp=NormalizeDouble((bstp[1]*pp),Digits);}
      else if((_Period>=tfr[1]))
        {tp=NormalizeDouble((bstp[2]*pp),Digits);}
      //-
      tpABS=NormalizeDouble((MinimumTP*pp),Digits);
      //--
     }
   else {tp=NormalizeDouble(TakeProfit*pp,Digits);}
//--
   if(autsl) {sl=NormalizeDouble((pp*108)+spread,Digits);}
   else {sl=StopLoss*pp;}
//--
   if(AutoTrailingStop) {tstpv=NormalizeDouble(bstp[0]*pp,Digits);}
   else {tstpv=NormalizeDouble(TrailingStop*pp,Digits);}
   tsstv=NormalizeDouble(TrailingStopStep*pp,Digits);
   if(tstpv<beplmt) {tstpv=beplmt;}
   trstart=NormalizeDouble((tstpv*0.38)*pp,Digits);
   step=TrailingStopStep;
//--
   RefreshRates();
   pBo=High[0]-Close[0];
   pSo=Close[0]-Low[0];
//--
   pdifB=pp*difB;
   pdifS=pp*difS;
   mdHC=(pBo>pdifB);
   mdLC=(pSo>pdifS);
   mdUp=(Close[0]<(lastBid-pdifB));
   mdDn=(Close[0]>(lastAsk+pdifS));
//--
   return;
//----
  } //-end CalcSTP()
//---------//

void CkOpen() //-- function: CheckOpenTrade.
  {
//----
   ob=0; os=0;
   totalord=OrdersTotal();
//--
   for(s=0; s<totalord; s++)
     {
      if(OrderSelect(s,SELECT_BY_POS,MODE_TRADES)==True)
        {
         if(OrderSymbol()==_Symbol)
           {
            //--
            if(OrderType()==OP_BUY) {ob++; lastBid=OrderOpenPrice();}
            if(OrderType()==OP_SELL) {os++; lastAsk=OrderOpenPrice();}
            //--
           }
        }
     }
//--
   return;
//----
  } //-end CkOpen()
//---------//

void CkClose() //-- function: CheckOrderClose.
  {
//----
   CkOpen();
   datetime octm;
   RefreshRates();
   totalhst=OrdersHistoryTotal();
//--
   if((ob+os)<(hb+hs))
     {
      for(s=totalhst; s>0; s--)
        {
         //--
         if(OrderSelect(s,SELECT_BY_POS,MODE_HISTORY)==True)
           {
            if(OrderSymbol()==_Symbol)
              {
               //--
               if(OrderType()==OP_BUY)
                 {
                  RefreshRates();
                  octm=OrderCloseTime();
                  if((octm>0) && (ob<hb))
                    {
                     Print("-----CLOSED BUY ORDER ",_Symbol," - Instant Close By System..OK!");
                     PlaySound("ping.wav");
                     hb=ob;
                     beh=0;
                     stxb=0;
                     trxb=0;
                    }
                 }
               //--
               if(OrderType()==OP_SELL)
                 {
                  RefreshRates();
                  octm=OrderCloseTime();
                  if((octm>0) && (os<hs))
                    {
                     Print("-----CLOSED SELL ORDER ",_Symbol," - Instant Close By System..OK!");
                     PlaySound("ping.wav");
                     hs=os;
                     bel=0;
                     stxs=0;
                     trxs=0;
                    }
                 }
               //--                        
              }
           }
         //--
        }
     }
//---
   return;
//----
  } //-end CkClose()
//---------//

void TpSlTrlS() //-- function: Check if order is profit.
  {
//----
   ResetLastError();
   CkOpen();
   CalcSTP();
   hb=ob;
   hs=os;
   int error;
   bool result,trest,modst;
   totalpft=OrdersTotal()-1;
   for(ip=totalpft; ip>=0; ip--)
     {
      if(OrderSelect(ip,SELECT_BY_POS,MODE_TRADES)==True)
        {
         if(OrderSymbol()==_Symbol)
           {
            if(OrderType()==OP_BUY)
              {
               //---
               if((stxb<ob) && (OrderStopLoss()==0.00 || OrderTakeProfit()==0.00))
                 {
                  for(int stb=ob; stb>0; stb--)
                    {
                     if(OrderStopLoss()==0.00) {slA=NormalizeDouble(OrderOpenPrice()-sl,Digits);}
                     else {slA=OrderStopLoss();}
                     if(OrderTakeProfit()==0.00) {tpA=NormalizeDouble(OrderOpenPrice()+tp,Digits);}
                     else {tpA=OrderTakeProfit();}
                     modst=OrderModify(OrderTicket(),OrderOpenPrice(),slA,tpA,0,clrGreen);
                     if(modst!=true) {error=GetLastError();}
                     if(error>0)
                       {
                        ResetLastError();
                        Sleep(500);
                        RefreshRates();
                        modst=OrderModify(OrderTicket(),OrderOpenPrice(),slA,tpA,0,clrGreen);
                       }
                    }
                  if(modst==true) {stxb++;}
                  else {stxb=0;}
                 }
               //--
               RefreshRates();
               bid=MarketInfo(_Symbol,MODE_BID);
               if((opttp) && (!nomintp) && ((OrderOpenPrice()<(bid-tpABS)) && (mdHC || mdUp))) //-check if order buy is profit
                 {
                  for(int opb=ob; opb>0; opb--)
                    {
                     RefreshRates();
                     bid=MarketInfo(_Symbol,MODE_BID);
                     result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                     if(result!=true) {error=GetLastError();}
                     if(error>0)
                       {
                        ResetLastError();
                        Sleep(500);
                        RefreshRates();
                        bid=MarketInfo(_Symbol,MODE_BID);
                        result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                       }
                     //--
                     if(error==0 && result==true)
                       {
                        Print("-----AutoTStop has CLOSED BUY ORDER ",_Symbol," - Automatic Min Take profit..OK!");
                        PlaySound("ping.wav");
                       }
                    }
                  break;
                 } //-end BUY Order Profit minimum TP
               //--                       
               RefreshRates();
               bid=MarketInfo(_Symbol,MODE_BID);
               if((opttp) && (nomintp) && (fClB) && (OrderOpenPrice()<(bid-dvab))) //-check if order buy is profit 
                 {
                  for(int nob=ob; nob>0; nob--)
                    {
                     RefreshRates();
                     bid=MarketInfo(_Symbol,MODE_BID);
                     result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                     if(result!=true) {error=GetLastError();}
                     if(error>0)
                       {
                        ResetLastError();
                        Sleep(500);
                        RefreshRates();
                        bid=MarketInfo(_Symbol,MODE_BID);
                        result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                       }
                     //--
                     if(error==0 && result==true)
                       {
                        Print("-----AutoTStop has CLOSED BUY ORDER ",_Symbol," - Forcibly closed Automatic Take profit..OK!");
                        PlaySound("ping.wav");
                       }
                    }
                  break;
                 } //-end BUY Order Profit Auto TP
               //-- BEP, Trailing Stop & Step
               RefreshRates();
               bid=MarketInfo(_Symbol,MODE_BID);
               if((bid>(OrderOpenPrice()+beplmt)) && (beh<hb))
                 {
                  for(int be=ob; be>0; be--)
                    {trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+bep,OrderTakeProfit(),0);}
                  beh++;
                 }
               //--
               if(TrlStp && (trxb==0) && (bid>(OrderOpenPrice()+tstpv)))
                 {
                  for(int tsb=ob; tsb>0; tsb--)
                    {trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+trstart,OrderTakeProfit(),0);}
                  trxb++;
                 }
               //--
               if(TrlStp && (trxb>0) && (bid>(OrderStopLoss()+(trstart*0.6))))
                 {
                  trxb++;
                  for(int tbx=ob; tbx>0; tbx--)
                    {
                     if(opttp) {double otpb=OrderTakeProfit()+(step*pp);}
                     else {otpb=OrderTakeProfit();}
                     trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+(trxb*tsstv),otpb,0);
                    }
                 } // end BUY Order Trailing Stop.
               //---
              }
            //----
            if(OrderType()==OP_SELL)
              {
               //---
               if((stxs<os) && (OrderStopLoss()==0.00 || OrderTakeProfit()==0.00))
                 {
                  for(int sts=os; sts>0; sts--)
                    {
                     if(OrderStopLoss()==0.00) {slB=NormalizeDouble(OrderOpenPrice()+sl,Digits);}
                     else {slB=OrderStopLoss();}
                     if(OrderTakeProfit()==0.00) {tpB=NormalizeDouble(OrderOpenPrice()-tp,Digits);}
                     else {tpB=OrderTakeProfit();}
                     modst=OrderModify(OrderTicket(),OrderOpenPrice(),slB,tpB,0,clrRed);
                     if(modst!=true) {error=GetLastError();}
                     if(error>0)
                       {
                        ResetLastError();
                        Sleep(500);
                        RefreshRates();
                        modst=OrderModify(OrderTicket(),OrderOpenPrice(),slB,tpB,0,clrRed);
                       }
                    }
                  if(modst==true) {stxs++;}
                  else {stxs=0;}
                 }
               //--
               RefreshRates();
               ask=MarketInfo(_Symbol,MODE_ASK);
               if((opttp) && (!nomintp) && ((OrderOpenPrice()>(ask+tpABS)) && (mdLC || mdDn))) //-check if order sell is profit
                 {
                  for(int ops=os; ops>0; ops--)
                    {
                     RefreshRates();
                     ask=MarketInfo(_Symbol,MODE_ASK);
                     result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                     if(result!=true) {error=GetLastError();}
                     if(error>0)
                       {
                        ResetLastError();
                        Sleep(500);
                        RefreshRates();
                        ask=MarketInfo(_Symbol,MODE_ASK);
                        result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                       }
                     //--
                     if(error==0 && result==true)
                       {
                        Print("-----AutoTStop has CLOSED SELL ORDER ",_Symbol," - Automatic Min Take profit..OK!");
                        PlaySound("ping.wav");
                       }
                    }
                  break;
                 } //-end SELL Order Profit minimum TP
               //--                       
               RefreshRates();
               ask=MarketInfo(_Symbol,MODE_ASK);
               if((opttp) && (nomintp) && (fClS) && (OrderOpenPrice()>(ask+dvab))) //-check if order sell is profit
                 {
                  for(int nos=os; nos>0; nos--)
                    {
                     RefreshRates();
                     ask=MarketInfo(_Symbol,MODE_ASK);
                     result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                     if(result!=true) {error=GetLastError();}
                     if(error>0)
                       {
                        ResetLastError();
                        Sleep(500);
                        RefreshRates();
                        ask=MarketInfo(_Symbol,MODE_ASK);
                        result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                       }
                     //--
                     if(error==0 && result==true)
                       {
                        Print("-----AutoTStop has CLOSED SELL ORDER ",_Symbol," - Forcibly closed Automatic Take profit..OK!");
                        PlaySound("ping.wav");
                       }
                    }
                  break;
                 } //-end SELL Order Profit Auto TP                        
               //-- BEP, Trailing Stop & Step
               RefreshRates();
               ask=MarketInfo(_Symbol,MODE_ASK);
               if((ask<(OrderOpenPrice()-beplmt)) && (bel<hs))
                 {
                  for(int se=os; se>0; se--)
                    {trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-bep,OrderTakeProfit(),0);}
                  bel++;
                 }
               //--
               if(TrlStp && (trxs==0) && (ask<(OrderOpenPrice()-tstpv)))
                 {
                  for(int tss=os; tss>0; tss--)
                    {trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-trstart,OrderTakeProfit(),0);}
                  trxs++;
                 }
               //--
               if(TrlStp && (trxs>0) && (ask<(OrderStopLoss()-trstart)))
                 {
                  trxs++;
                  for(int tsx=os; tsx>0; tsx--)
                    {
                     if(opttp) {double otps=OrderTakeProfit()-(step*pp);}
                     else {otps=OrderTakeProfit();}
                     trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-(trxs*tsstv),otps,0);
                    }
                 } // end SELL Order Trailing Stop.                   
               //----
              }
           }
        }
     }
//---
   return;
//----
  } //-end TpSlTrlS()
//---------//

void StartAction() //-- function: start take action for Auto TP or Trailing Stop.
  {
//----
   RefreshRates();
   CkClose();
   CkOpen();
   if((ob+os>0) || ((stxb+stxs)<(ob+os)) || (opttp || TrlStp))
     {TpSlTrlS();}
//----
   return;
//---- 
  } //-end StartAction()
//---------//

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//----
   GlobalVariablesDeleteAll();
//--
   return;
//----
  } //-end OnDeinit()
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
int start()
  {
//----
   RefreshRates();
   if(IsTradeAllowed()==false) {return(0);}
   if(DayOfWeek()==0 || DayOfWeek()==6) {return(0);}
   if(FridayTrade==false && DayOfWeek()==5) {return(0);}
   else StartAction();
//--
   return(0);
//----
  } //-end start()
//---------//
//+------------------------------------------------------------------+
