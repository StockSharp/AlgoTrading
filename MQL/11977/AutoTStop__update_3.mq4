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
*/
//--
#property description "AutoTStop EA does not work to open the order, but only help trader for"
#property description "optimization TakeProfit, put a StopLoss and implement TrailingStop."
//--- User Input
input string             AutoTStop = "Copyright © 2014 3RJ ~ created by Roy Philips-Jacobs";
extern bool            FridayTrade = True; // If True, EA still trading at Friday
extern bool        UseTrailingStop = True; // Use Trailing Stop, True (Yes) or False (Not)
extern bool       AutoTrailingStop = True; // Default Trailing Stop value = 6.0
extern double         TrailingStop = 24.0; // If Auto Trailing Stop False, input Trailing Stop Value
extern double     TrailingStopStep = 1.0; // Input Trailing Stop Step Value (default 1.0)
input string     AutomaticSystemTP = "Set AutomaticTakeProfit=True or False";
extern bool OptimizationTakeProfit = True; // TP will calculation by EA and Automatic TP by EA
extern bool    NoMinimumTakeProfit = True; // True or False -> If Set True, 100% TP by EA not use minimum TP.
input string       MinimumSystemTP = "If Set NoMinimumTakeProfit=False"; // TP by EA on minimum TP values
extern double            MinimumTP = 10; // Minimum TP by EA on the AutomaticTakeProfit=True function, default value 10
input string        ManualSystemTP = "If Set AutomaticTakeProfit=False"; // TP by Terminal MT4 (same as manual trading)
extern double           TakeProfit = 20; // TP by System, values can adjust by user
input string     AutomaticSystemSL = "Set AutomaticStopLoss=True or False";
extern bool      AutomaticStopLoss = True; // SL will calculation by EA
input string        ManualSystemSL = "If Set AutomaticStopLoss=False"; // SL values can adjusted by user
extern double             StopLoss = 96; // SL adjusted by user
//---
//--- Global scope
double ask,bid,dev;
double pp,sl,tp,digit;
double lastAsk,lastBid;
double tsstv,tstpv,trstart;
double dvab,slA,slB,tpA,tpB,tpABS;
double difB,difS,pBo,pSo,pdifB,pdifS;
//---
bool nomintp;
bool fClB,fClS;
bool TrlStp,opttp,autsl;
bool mdHC,mdLC,mdUp,mdDn;
//---
int ort=0;
int tfh=60;
int bar=100;
int stx,trx;
int hb,hs,ob,os,s,ip,ox;
int totalord,totalpft,totalhst;
//---
string CopyR;
string symbol;
//---
void EventSetTimer();
//----
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(void)
  {
//----
   symbol=Symbol(); 
   CopyR="Copyright © 2014 3RJ ~ created by Roy Philips-Jacobs";
   if(AutoTStop!=CopyR) {return(0);}
//---
   //-- Checking the Digits Point
   digit=Digits;
   if (digit==3 || digit==5)
      {pp=Point*10;}
   else {pp=Point;}
//---
   hb=0;
   hs=0;
   ob=0;
   os=0;
   stx=0;
   trx=0;
   difB=0.8;
   difS=0.8;
   dvab=pp*3;
//---
   autsl=AutomaticStopLoss;
   opttp=OptimizationTakeProfit;
   nomintp=NoMinimumTakeProfit;
   TrlStp=UseTrailingStop;
   //--
   tstpv=NormalizeDouble(TrailingStop*pp,digit);
   tsstv=NormalizeDouble(TrailingStopStep*pp,digit);
   trstart=NormalizeDouble((TrailingStop*0.50)*pp,digit);
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
      double spread=Ask-Bid;
      //-
      fClB=false;
      fClS=false;
      int zh1,zl1;
      //--
      for(int iz=bar-1; iz>=0; iz--) //- for(iz tfh)
        {
          //--     
          if(iHigh(symbol,tfh,iz)==iCustom(symbol,tfh,"ZigZag",1,iz))
            {zh1=iz;}
          if(iLow(symbol,tfh,iz)==iCustom(symbol,tfh,"ZigZag",2,iz))
            {zl1=iz;}
          //--
        } //-end for(iz tfh)
      //-
      if(zl1==0) {bool stB=true;}
      if(zh1==0) {bool stS=true;}
      //-
      if(opttp && nomintp)
        {   
          //--
          tp=NormalizeDouble((20*pp)+dvab,digit);
          //-
          if(stB==true) {fClS=true;}
          if(stS==true) {fClB=true;}
          //--
        }
      //--
      if(opttp && !nomintp)
        {      
          //--
          tp=NormalizeDouble((20*pp)+dvab,digit);
          //-
          tpABS=NormalizeDouble((MinimumTP*pp)+dvab,digit);
          //--
        }
      else {tp=NormalizeDouble(TakeProfit*pp,digit);}
      //--
      if(autsl) 
        {
          //--
          int pH=iHighest(symbol,tfh,MODE_HIGH,50,0);
          int pL=iLowest(symbol,tfh,MODE_LOW,50,0);
          if(pH!=-1) double tslS=iHigh(symbol,tfh,pH);
          if(pL!=-1) double tslB=iLow(symbol,tfh,pL);
          if(tslS==MarketInfo(symbol,MODE_HIGH))
            {tslS=MarketInfo(symbol,MODE_HIGH)+(pp*28);}
          if(tslB==MarketInfo(symbol,MODE_LOW))
            {tslB=MarketInfo(symbol,MODE_LOW)-(pp*28);}
          //--
          if(ort==1)
            {double tsl=(Bid-tslB)/pp;}
          if(ort==2)
            {tsl=(tslS-Ask)/pp;}
          sl=NormalizeDouble(pp*tsl,digit)+spread;
        } 
      else {sl=StopLoss;}
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
      ob=0; 
      os=0;
      totalord=OrdersTotal();
      //--
      for (s=0; s<totalord; s++)
          {
             if (OrderSelect(s,SELECT_BY_POS,MODE_TRADES)==True)
                {
                   if (OrderSymbol()==symbol)
                      {
                        //--
                        if (OrderType()==OP_BUY) {ob++; hb++; lastBid=OrderOpenPrice();}
                        if (OrderType()==OP_SELL) {os++; hs++; lastAsk=OrderOpenPrice();}
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
      totalhst=OrdersHistoryTotal();
      //--
      for (s=0; s<totalhst; s++)   
          {
            //--
            if(OrderSelect(s,SELECT_BY_POS,MODE_HISTORY)==True)
              {
                if(OrderSymbol()==symbol)
                  {
                    //--
                    if(OrderType()==OP_BUY)
                      {
                        RefreshRates();
                        octm=OrderCloseTime();
                        if(hb>0 && ob==0 && octm>0)
                          {
                            Print("-----CLOSED BUY ORDER ",symbol," - Instant Close By System..OK!");
                            PlaySound("ping.wav");
                            hb=0;
                            stx=0;
                            trx=0;
                            break;
                          }                             
                      }
                    //--
                    if(OrderType()==OP_SELL)
                      {
                        RefreshRates();
                        octm=OrderCloseTime();
                        if(hs>0 && os==0 && octm>0)
                          {
                            Print("-----CLOSED SELL ORDER ",symbol," - Instant Close By System..OK!");
                            PlaySound("ping.wav");
                            hs=0;
                            stx=0;
                            trx=0;
                            break;
                          }                                
                      }
                    //--                        
                  }
              }
            //--
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
      ort=0;
      int error;
      bool result,trest,modst;
      totalpft=OrdersTotal()-1;
      for(ip=totalpft; ip>=0; ip--)
         {
           if(OrderSelect(ip,SELECT_BY_POS,MODE_TRADES)==True)
             {
               if(OrderSymbol()==symbol)
                 {
                   if(OrderType()==OP_BUY)
                     {
                       //---
                       ort=1;
                       CalcSTP();
                       //--
                       if((autsl)&&(stx==0)&&(OrderStopLoss()==0.00||OrderTakeProfit()==0.00))
                         {
                           if(OrderStopLoss()==0.00) {slA=NormalizeDouble(OrderOpenPrice()-sl,digit);}
                           else {slA=OrderStopLoss();}
                           if(OrderTakeProfit()==0.00) {tpA=NormalizeDouble(OrderOpenPrice()+tp,digit);}
                           else {tpA=OrderTakeProfit();}
                           modst=OrderModify(OrderTicket(),OrderOpenPrice(),slA,tpA,0,clrGreen);
                           if(modst!=true) {error=GetLastError();}
                           if(error>0)
                             {
                               ResetLastError();
                               RefreshRates();
                               ask=MarketInfo(symbol,MODE_ASK);
                               slA=NormalizeDouble(ask-sl,digit);
                               tpA=NormalizeDouble(ask+tp,digit);
                               modst=OrderModify(OrderTicket(),OrderOpenPrice(),slA,tpA,0,clrGreen);
                             }
                           if(modst==true) {stx++; break;}
                           else {stx=0; break;}
                         }
                       //--
                       RefreshRates();
                       bid=MarketInfo(symbol,MODE_BID);
                       if((!nomintp)&&((OrderOpenPrice()<(bid-tpABS))&&(mdHC||mdUp))) //-check if order buy is profit 
                         {
                           RefreshRates();
                           bid=MarketInfo(symbol,MODE_BID);
                           result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                           if(result!=true) {error=GetLastError();}
                           if(error>0)
                             {
                               ResetLastError();
                               RefreshRates();
                               bid=MarketInfo(symbol,MODE_BID);
                               result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                             }
                           //--
                           if(error==0 && result==true)
                             {
                               Print("-----AutoTStop has CLOSED BUY ORDER ",symbol," - Automatic Min Take profit..OK!");
                               PlaySound("ping.wav");
                               break;
                             }
                         } //-end BUY Order Profit minimum TP
                       //--                       
                       RefreshRates();
                       bid=MarketInfo(symbol,MODE_BID);
                       if((nomintp)&&(fClB)&&(OrderOpenPrice()<(bid-dvab))) //-check if order buy is profit 
                         {
                           RefreshRates();
                           bid=MarketInfo(symbol,MODE_BID);
                           result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                           if(result!=true) {error=GetLastError();}
                           if(error>0)
                             {
                               ResetLastError();
                               RefreshRates();
                               bid=MarketInfo(symbol,MODE_BID);
                               result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                             }
                           //--
                           if(error==0 && result==true)
                             {
                               Print("-----AutoTStop has CLOSED BUY ORDER ",symbol," - Forcibly closed Automatic Take profit..OK!");
                               PlaySound("ping.wav");
                               break;
                             }
                         } //-end BUY Order Profit ZigZag TP
                       //-- Trailing Stop & Step
                       RefreshRates();
                       bid=MarketInfo(symbol,MODE_BID);
                       if(TrlStp && (trx==0) && (bid>(OrderOpenPrice()+trstart)))
                         {
                           trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+trstart,OrderTakeProfit(),0);
                           trx++;
                           break;
                         }
                       //--
                       if(TrlStp && (trx>0) && (bid>(OrderStopLoss()+(trstart/2))))
                         {
                           trx++;
                           trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+(trx*tsstv),OrderTakeProfit(),0);
                           break;
                         } // end BUY Order Trailing Stop.
                     //---
                     }
                   //----
                   if(OrderType()==OP_SELL)
                     {
                       //---
                       ort=2;
                       CalcSTP();
                       //--
                       if((autsl)&&(stx==0)&&(OrderStopLoss()==0.00||OrderTakeProfit()==0.00))
                         {
                           if(OrderStopLoss()==0.00) {slB=NormalizeDouble(OrderOpenPrice()+sl,digit);}
                           else {slB=OrderStopLoss();}
                           if(OrderTakeProfit()==0.00) {tpB=NormalizeDouble(OrderOpenPrice()-tp,digit);}
                           else {tpB=OrderTakeProfit();}
                           modst=OrderModify(OrderTicket(),OrderOpenPrice(),slB,tpB,0,clrRed);
                           if(modst!=true) {error=GetLastError();}
                           if(error>0)
                             {
                               ResetLastError();
                               RefreshRates();
                               bid=MarketInfo(symbol,MODE_BID);
                               slB=NormalizeDouble(bid+sl,digit);
                               tpB=NormalizeDouble(bid-tp,digit);
                               modst=OrderModify(OrderTicket(),OrderOpenPrice(),slB,tpB,0,clrRed);
                             }
                           if(modst==true) {stx++; break;}
                           else {stx=0; break;}
                         }
                       //--
                       RefreshRates();
                       ask=MarketInfo(symbol,MODE_ASK);
                       if((!nomintp)&&((OrderOpenPrice()>(ask+tpABS))&&(mdLC||mdDn))) //-check if order sell is profit
                         {
                           RefreshRates();
                           ask=MarketInfo(symbol,MODE_ASK);
                           result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                           if(result!=true) {error=GetLastError();}
                           if(error>0)
                             {
                               ResetLastError();
                               RefreshRates();
                               ask=MarketInfo(symbol,MODE_ASK);
                               result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                             }
                           //--
                           if(error==0 && result==true)
                             {
                               Print("-----AutoTStop has CLOSED SELL ORDER ",symbol," - Automatic Min Take profit..OK!");
                               PlaySound("ping.wav");
                               break;
                             }
                         } //-end SELL Order Profit minimum TP
                       //--                       
                       RefreshRates();
                       ask=MarketInfo(symbol,MODE_ASK);
                       if((nomintp)&&(fClS)&&(OrderOpenPrice()>(ask+dvab))) //-check if order sell is profit
                         {
                           RefreshRates();
                           ask=MarketInfo(symbol,MODE_ASK);
                           result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                           if(result!=true) {error=GetLastError();}
                           if(error>0)
                             {
                               ResetLastError();
                               RefreshRates();
                               ask=MarketInfo(symbol,MODE_ASK);
                               result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                             }
                           //--
                           if(error==0 && result==true)
                             {
                               Print("-----AutoTStop has CLOSED SELL ORDER ",symbol," - Forcibly closed Automatic Take profit..OK!");
                               PlaySound("ping.wav");
                               break;
                             }
                         } //-end SELL Order Profit ZigZag TP                            
                       //-- Trailing Stop & Step
                       RefreshRates();
                       ask=MarketInfo(symbol,MODE_ASK);
                       if(TrlStp && (trx==0) && (ask<(OrderOpenPrice()-trstart)))
                         {
                           trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-trstart,OrderTakeProfit(),0);
                           trx++;
                           break;
                         }
                       //--
                       if(TrlStp && (trx>0) && (ask<(OrderStopLoss()-(trstart/2))))
                         {
                           trx++;
                           trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-(trx*tsstv),OrderTakeProfit(),0);
                           break;
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
     CkOpen();
     if (ob+os>0) {TpSlTrlS();}
     RefreshRates();
     CkClose();
     CkOpen();
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
   EventKillTimer();
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
   if (IsTradeAllowed()==false) {return(0);}
   if (DayOfWeek()==0 || DayOfWeek()==6) {return(0);}
   if(FridayTrade==false && DayOfWeek()==5) {return(0);}
   else StartAction();
   //--
   return(0);
//----
  } //-end start()
//---------//
//+------------------------------------------------------------------+