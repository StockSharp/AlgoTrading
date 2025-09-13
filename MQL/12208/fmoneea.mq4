//+------------------------------------------------------------------+
//|                                                      FMOneEA.mq4 |
//|                        Copyright 2014, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, MetaQuotes Software Corp. By 3rjfx Roy Philips-Jacobs ~ Created: 2014/12/20"
#property link      "http://www.mql5.com"
#property link      "http://www.gol2you.com ~ Forex Videos"
#property version   "7.00"
//--
#property description "FMOneEA is the Scalping Forex Expert Advisor for MT4."
#property description "FMOneEA  working on all Timeframes (M1 to MN1)."
#property description "FMOneEA calculation is based of MA and MACD indicator."
/*
   Last modify (update) 23/03/2015.
   //--
   Update (21/04/2015):
   ~ Improve signal formula, to make it more accurate.
   ~ Added Redemption function to Redemption Loss Order.
   //--
   Update (29/04/2015): 
   ~ Fix code for Redemption and option for multiplication Lost Redemption.
   ~ Improvement codes for braking movement.
   ~ Simplify signal formula.
   //--
   Update (26/05/2015):    
   ~ Fix code for Lots Redemption
   ~ Increase the speed of the EA process
   ~ Improves code for the Trailing Stop and Trailing Profit
     If setting: UseTrailingStop = True, and AutoTrailingStop = True, and AutomaticTakeProfit = True, 
     then, when Stop Loss in Traling, then Take Profit will be Trailing too, so take profit will be dynamic.
   //--
   Update (02/06/2015): 
   ~ Error correction on FM1Redemption() function.
   ~ Add scripts to move Stop Loss to the BEP, if order is profit, before the Trailing Stop is done.
   ~ Add Option for Maximum Lot, for multiplication Lots for Redemption.
   //--
   Update (16/07/2015):
   ~ Error correction for variable arrays timeframe because the array mat [4] was out of range (in update_5).
   
   Update (17/07/2015): (Update_7)
   ~ Simplify signal formula, without ZigZag indicator.
   //--
   Update (17/08/2015): (Update_8)
   ~ Improvement of the structure of the program
   ~ Fix bugs in open order
*/
//--
//--
#property description "-------Use this Expert Advisor at your own risk.-------"
//---
#include <stderror.mqh>
#include <stdlib.mqh>
//---
//--- User Input
input string             FMOneEA = "Copyright © 2014 3RJ ~ Roy Philips-Jacobs";
input string           FMOneEATF = "FMOneEA working on all Timeframes (M1 to MN1)";
input int          FMOneEAPeriod=PERIOD_H4; // Recommended use on Timeframes H4 (PERIOD_H4)
extern bool          FridayTrade = True; // If True, EA still trading at Friday
extern bool           Redemption = True; // Allow to Redemption Loss Order
input string    OptimizationLots = "Set LotsOptimization=True";
extern bool     LotsOptimization = True; // If True, Lots wil calculation by EA, default Lots size for optimization = 0.01"
extern double               Lots = 0.01; // If LotsOptimization=False, Lots adjusted by user
extern double        LotsRedempt = 2.0; // Value for redemption to multiplication Lots, default value 2.0
extern double     MaxLotsRedempt = 4.0; // Maximum Value of Lots redemption for multiplication Lots, default 4 times of initial lots
extern int          MaxOpenOrder=12; // Maximum Allowed for Open Order (Maximum Pairs to Trade = 12 pairs)
                                     // PAIRS: EURAUD,AUDUSD,EURUSD,NZDUSD,GBPUSD,GBPAUD,XAUUSD,GBPJPY,EURJPY,USDJPY,USDCHF,USDCAD //
input string   AutomaticSystemTP = "Set AutomaticTakeProfit=True or False";
extern bool  AutomaticTakeProfit = True; // TP will calculation by EA and Automatic TP by EA
extern bool  NoMinimumTakeProfit = True; // True or False -> If Set True, 100% TP by EA not use minimum TP.
input string     MinimumSystemTP="If Set NoMinimumTakeProfit=False"; // TP by EA on minimum TP values
extern double          MinimumTP=10; // Minimum TP by EA on the AutomaticTakeProfit=True function, default value 10
input string      ManualSystemTP="If Set AutomaticTakeProfit=False"; // TP by Terminal MT4 (same as manual trading)
extern double         TakeProfit=20; // TP by System, values can adjust by user, default value 20
input string   AutomaticSystemSL="Set AutomaticStopLoss=True";
extern bool    AutomaticStopLoss=True; // SL will calculation by EA
input string      ManualSystemSL="If Set AutomaticStopLoss=False"; // SL values can adjusted by user
extern double           StopLoss=157; // SL adjusted by user, default value 157
extern bool      UseTrailingStop = True; // Use Trailing Stop, True (Yes) or False (Not)
extern bool     AutoTrailingStop = True; // If Set True, 100% TS calculation by EA not use trailing stop value.
extern double       TrailingStop = 14.0; // If Use Trailing Stop True, input Trailing Stop Value, default value 14
extern double   TrailingStopStep=1.0; // Input Trailing Stop Step Value (default 1.0)
input string      UsingSecureBEP="Set UseSecureBEP == True or False"; //If  True, the EA wiil added BEP to secure order
extern bool         UseSecureBEP=False; // If  True, the EA wiil added BEP to secure your order
                                        //--
//--- Global scope
double lot,pp,rdu,rdd;
double AcEq,sl,tp,cr,ps;
double slA,slB,tpA,tpB,tpAB;
double lastAsk,lastBid,divAB,spread;
double redlotsB,redlotsS,dblLot,mxlr;
double difB,difS,pBo,pSo,pdifB,pdifS;
double tsstv,tstpv,trstart,bep,beplmt;
double bstp[]={14,28,42}; //arrays tp value
//---
bool nomintp,redem;
bool NoOrder,TrlStp;
bool ConsBuy,ConsSell;
bool lotopt,opttp,autsl;
bool frcClB,frcClS,scbep;
bool mdHC,mdLC,mdUp,mdDn;
bool SlashUp,SlashDn,SlashNt;
bool RedemUp,RedemDn,RedemNt,ATrS;
//---
//-- MqlTick variables
double ask; // Current Bid price
double bid; // Current Ask price
datetime time; // Time of the last prices update
double last; // Price of the last deal (Last)
ulong volume; // Volume for the current Last price
//---
int beh;
int bel;
int dev;
int trxb;
int trxs;
int step;
int OpOrd;
int tfx=0;
int CdB,CdS;
int RdB,RdS;
int coprt;
int cop1=100;
int FMOneMagic=1119; // magic number
int hb,hs,ob,os,s,ip;
int mat[]={2,5,10,30,20}; // timeframes arrays
int tfr[]={30,240}; // timeframes arrays tp
int totalord,totalpft,totalhst;
//---
string CR;
string CommentBuy;
string CommentSell;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(void)
  {
//---- create timer
   EventSetTimer(60);
   CR="Copyright © 2014 3RJ ~ Roy Philips-Jacobs";
   if(FMOneEA!=CR) {return(0);}
//---
//-- Checking the Digits Point
   if(Digits==3 || Digits==5 || _Symbol=="XAUUSD")
     {pp=Point*10; dev=3;}
   else {pp=Point; dev=3;}
//---
   hb=0;
   hs=0;
   ob=0;
   os=0;
   rdu=0;
   rdd=0;
   beh=0;
   bel=0;
   trxb=0;
   trxs=0;
   difB=0.8;
   difS=0.8;
//---
   NoOrder=true;
   redem=Redemption;
   lotopt=LotsOptimization;
   opttp=AutomaticTakeProfit;
   autsl=AutomaticStopLoss;
   TrlStp=UseTrailingStop;
   ATrS=AutoTrailingStop;
   nomintp=NoMinimumTakeProfit;
   OpOrd=MaxOpenOrder/(MaxOpenOrder*0.5); // safety Lots for trade
   mxlr=MaxLotsRedempt;
   scbep=UseSecureBEP;
//--
//--- initialization done
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| expert proccess working function                                 |
//+------------------------------------------------------------------+
void LotOptimize() //-- function: calculation Optimization Lots
  {
//----
   if(lotopt)
     {
      //--
      AcEq=AccountEquity();
      if(AcEq<=1000.00) {lot=NormalizeDouble(MarketInfo(_Symbol,MODE_MINLOT),2);}
      else {lot=NormalizeDouble((MathFloor(AcEq/10)*0.001)/OpOrd,2);}
      //--
      if(lot<MarketInfo(_Symbol,MODE_MINLOT))
        {lot=NormalizeDouble(MarketInfo(_Symbol,MODE_MINLOT),2);}
      //--
     }
//--
   else {lot=NormalizeDouble(Lots,2);}
//---
   return;
//----
  } //-end LotOptimize()
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SLSet() //-- function: calculation Stop Loss 
  {
//----
   if(!autsl) {sl=NormalizeDouble(StopLoss*pp,Digits);}
   else {sl=NormalizeDouble(157*pp,Digits);}
//--
   step=TrailingStopStep;
   if((TrlStp) && (ATrS))
     {
      if(_Period<tfr[0])
        {tstpv=NormalizeDouble((bstp[0]*pp*0.5),Digits);}
      else if((_Period>=tfr[0]) && (_Period<tfr[1]))
        {tstpv=NormalizeDouble((bstp[1]*pp*0.5),Digits);}
      else if((_Period>=tfr[1]))
        {tstpv=NormalizeDouble((bstp[2]*pp*0.5),Digits);}
      //-
      trstart=tstpv*0.5;
      tsstv=NormalizeDouble(step*pp,Digits);
     }
//-
   else if((TrlStp) && (!ATrS))
     {
      tstpv=NormalizeDouble(TrailingStop*pp,Digits);
      trstart=NormalizeDouble(tstpv*pp*0.5,Digits);
      tsstv=NormalizeDouble(step*pp,Digits);
     }
//--
   spread=Ask-Bid;
   bep=NormalizeDouble(pp*7.7,Digits);
   beplmt=NormalizeDouble(bep+spread,Digits);
//--
   return;
//----
  } //-end SLSet()
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TPSet() //-- function: for calculation Automatic Take Profit.
  {
//----
   RefreshRates();
//-
   frcClB=false;
   frcClS=false;
   double BSma2[],BSma4[];
   ArrayResize(BSma2,cop1);
   ArrayResize(BSma4,cop1);
   ArraySetAsSeries(BSma2,true);
   ArraySetAsSeries(BSma4,true);
//--
   RefreshRates();
//--
   for(int fm=cop1-1; fm>=0; fm--)
     {BSma2[fm]=iMA(_Symbol,0,mat[0],0,3,4,fm);}
   for(int fn=cop1-1; fn>=0; fn--)
     {BSma4[fn]=iMAOnArray(BSma2,0,4,0,3,fn);}
   double bsma0=(BSma2[0]-BSma4[0]);
   double bsma1=(BSma2[1]-BSma4[1]);
//--
//-
   if((bsma0>bsma1)&&(BSma2[0]>=BSma4[0])) {bool stgB=true;}
   if((bsma0<bsma1)&&(BSma2[0]<=BSma4[0])) {bool stgS=true;}
   if((!stgB) && (!stgS)) {bool stgN=true;}
   if((opttp) && (nomintp))
     {
      //--
      if(_Period<tfr[0])
        {tp=NormalizeDouble((bstp[0]*pp),Digits);}
      else if((_Period>=tfr[0]) && (_Period<tfr[1]))
        {tp=NormalizeDouble((bstp[1]*pp),Digits);}
      else if((_Period>=tfr[1]))
        {tp=NormalizeDouble((bstp[2]*pp),Digits);}
      divAB=tp*0.5;
      //-
      if((stgB==true)&&(!stgN)) {frcClS=true;}
      if((stgS==true)&&(!stgN)) {frcClB=true;}
      //--
     }
//--
   if((opttp) && (!nomintp))
     {
      //--
      if(_Period<tfr[0])
        {tp=NormalizeDouble((bstp[0]*pp),Digits);}
      else if((_Period>=tfr[0]) && (_Period<tfr[1]))
        {tp=NormalizeDouble((bstp[1]*pp),Digits);}
      else if((_Period>=tfr[1]))
        {tp=NormalizeDouble((bstp[2]*pp),Digits);}
      //-
      tpAB=NormalizeDouble(pp*MinimumTP,Digits);
      //--
     }
//--
   if(!opttp) {tp=NormalizeDouble(TakeProfit*pp,Digits);}
//--
   RefreshRates();
//-
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
  } //-end TPSet()
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckOpen() //-- function: CheckOpenTrade.
  {
//----
   totalord=OrdersTotal();
   ob=0;
   os=0;
//--
   for(s=0; s<totalord; s++)
     {
      if(OrderSelect(s,SELECT_BY_POS,MODE_TRADES)==True)
        {
         if((OrderSymbol()==_Symbol) && OrderMagicNumber()==FMOneMagic)
           {
            //--
            if(OrderType()==0) {ob++; hb++;}
            if(OrderType()==1) {os++; hs++;}
            //--
           }
        }
     }
//--
   return;
//----
  } //-end CheckOpen()
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckClose() //-- function: CheckOrderClose.
  {
//----
   CheckOpen();
   datetime octm;
   totalhst=OrdersHistoryTotal();
//--
   for(s=0; s<totalhst; s++)
     {
      //--
      if(OrderSelect(s,SELECT_BY_POS,MODE_HISTORY)==True)
        {
         if((OrderSymbol()==_Symbol) && OrderMagicNumber()==FMOneMagic)
           {
            //--
            if(OrderType()==OP_BUY)
              {
               RefreshRates();
               octm=OrderCloseTime();
               if((OrderClosePrice()<OrderOpenPrice()) && (redem))
                 {dblLot=OrderLots()*LotsRedempt;}
               else if((OrderClosePrice()>OrderOpenPrice()) && (redem))
                 {dblLot=0.0;}
               if(hb>0 && ob==0 && octm>0)
                 {
                  if(OrderProfit()<0.0)
                    {
                     Print("-----CLOSED BUY ORDER ",_Symbol," - Instant Close By System..in loss.. $",
                           NormalizeDouble(OrderProfit(),2)," ..OK!");
                    }
                  else if(OrderProfit()>0.0)
                    {
                     Print("-----CLOSED BUY ORDER ",_Symbol," - Instant Close By System..in profits.. $",
                           NormalizeDouble(OrderProfit(),2)," ..OK!");
                    }
                  //-
                  PlaySound("ping.wav");
                  hb=0;
                  beh=0;
                  rdd=0;
                  trxb=0;
                  break;
                 }
              }
            //--
            if(OrderType()==OP_SELL)
              {
               RefreshRates();
               octm=OrderCloseTime();
               if((OrderClosePrice()>OrderOpenPrice()) && (redem))
                 {dblLot=OrderLots()*LotsRedempt;}
               else if((OrderClosePrice()<OrderOpenPrice()) && (redem))
                 {dblLot=0.0;}
               if(hs>0 && os==0 && octm>0)
                 {
                  if(OrderProfit()<0.0)
                    {
                     Print("-----CLOSED SELL ORDER ",_Symbol," - Instant Close By System..in loss.. $",
                           NormalizeDouble(OrderProfit(),2)," ..OK!");
                    }
                  else if(OrderProfit()>0.0)
                    {
                     Print("-----CLOSED SELL ORDER ",_Symbol," - Instant Close By System..in profits.. $",
                           NormalizeDouble(OrderProfit(),2)," ..OK!");
                    }
                  //-
                  PlaySound("ping.wav");
                  hs =0;
                  bel=0;
                  rdu=0;
                  trxs=0;
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
  } //-end CheckClose()
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void FMOneCalculation() //-- function: Check trend and calculation order
  {
//----
//--
   ResetLastError();
   RefreshRates();
//--
   ConsBuy=false;
   ConsSell=false;
   SlashUp=false;
   SlashDn=false;
   SlashNt=false;
   RedemUp=false;
   RedemDn=false;
   RedemNt=false;
//--
   double bsOpen;
   double bsClos;
   double MACDM[];
   double MACDS[];
   double MACDD[];
   double MACDU[];
   double BB200[];
   double BB201[];
   double BB202[];
   double BSma02[],BSma06[];
   double BSma14[],BSma30[];
   ArrayResize(MACDM,cop1);
   ArrayResize(MACDS,cop1);
   ArrayResize(MACDD,cop1);
   ArrayResize(MACDU,cop1);
   ArrayResize(BB200,cop1);
   ArrayResize(BB201,cop1);
   ArrayResize(BB202,cop1);
   ArrayResize(BSma02,cop1);
   ArrayResize(BSma06,cop1);
   ArrayResize(BSma14,cop1);
   ArrayResize(BSma30,cop1);
   ArraySetAsSeries(MACDM,true);
   ArraySetAsSeries(MACDS,true);
   ArraySetAsSeries(MACDD,true);
   ArraySetAsSeries(MACDU,true);
   ArraySetAsSeries(BB200,true);
   ArraySetAsSeries(BB201,true);
   ArraySetAsSeries(BB202,true);
   ArraySetAsSeries(BSma02,true);
   ArraySetAsSeries(BSma06,true);
   ArraySetAsSeries(BSma14,true);
   ArraySetAsSeries(BSma30,true);
   CdB=0; CdS=0; RdB=0; RdS=0;
//--
   for(int bb=cop1-1; bb>=0; bb--)
     {
      BB200[bb]=iBands(_Symbol,tfx,mat[4],2,0,4,0,bb);
      BB201[bb]=iBands(_Symbol,tfx,mat[4],2,0,4,1,bb);
      BB202[bb]=iBands(_Symbol,tfx,mat[4],2,0,4,2,bb);
     }
//-
   for(int bs=cop1-1; bs>=0; bs--)
     {BSma02[bs]=iMA(_Symbol,tfx,mat[0],0,1,4,bs);}
   for(int b1=cop1-1; b1>=0; b1--)
     {BSma06[b1]=iMAOnArray(BSma02,0,mat[1],0,3,b1);}
   for(int b2=cop1-1; b2>=0; b2--)
     {BSma14[b2]=iMAOnArray(BSma06,0,mat[2],0,3,b2);}
   for(int b3=cop1-1; b3>=0; b3--)
     {BSma30[b3]=iMAOnArray(BSma14,0,mat[3],0,3,b3);}
   for(int md=cop1-1; md>=0; md--)
     {MACDM[md]=iMA(_Symbol,60,12,0,1,0,md)-iMA(_Symbol,60,26,0,1,0,md);}
   for(int mo=cop1-1; mo>=0; mo--)
     {MACDS[mo]=iMAOnArray(MACDM,0,9,0,0,mo);}
   for(int mr=cop1-1; mr>=0; mr--)
     {
      //--
      cr=MACDM[mr]-MACDS[mr];
      if(mr==0)
        {
         ps=MACDM[1]-MACDS[1];
         if(cr>ps) {bool mcmsup=true; MACDU[0]=MACDM[0]; MACDD[mr]=0.0;}
         if(cr<ps) {bool mcmsdn=true; MACDD[0]=MACDM[0]; MACDU[mr]=0.0;}
        }
      //--
     }
//--
   double mdms0=MACDM[0]-MACDS[0];
   double mdms1=MACDM[1]-MACDS[1];
   bool mretup=(mcmsup)&&(mdms0>mdms1);
   bool mretdn=(mcmsdn)&&(mdms0<mdms1);
   if((mretup) && (MACDM[0]>MACDM[1]))
     {bool macdup=true;}
   else if((mretdn) && (MACDM[0]<MACDM[1]))
     {bool macddn=true;}
   else if((!macddn) && (!macdup)) {return;}
//--
   double mabs0=BSma02[0]-BSma06[0];
   double mabs1=BSma02[1]-BSma06[1];
   double mab20=BSma06[0]-BSma14[0];
   double mab21=BSma06[1]-BSma14[1];
   double mabu0=BSma02[0]-BB201[0];
   double mabu1=BSma02[1]-BB201[1];
   double mabd0=BSma02[0]-BB202[0];
   double mabd1=BSma02[1]-BB202[1];
   double mb230=BB200[0]-BSma30[0];
   double mb231=BB200[1]-BSma30[1];
//--
   if((BSma02[0]>=BSma02[1]) && (BSma06[0]>BSma06[1]) && (mab20>=mab21) && (iOpen(_Symbol,tfx,0)<=BSma02[0]))
     {bool mamup=true;}
   else if((BSma02[0]<=BSma02[1]) && (BSma06[0]<BSma06[1]) && (mab20<=mab21) && (iOpen(_Symbol,tfx,0)>=BSma02[0]))
     {bool mamdn=true;}
   else if((BSma02[0]>=BSma02[1]) && (BSma06[0]>BSma14[0]) && (BSma14[0]>BB200[0]) && (BB200[0]>BB200[1]) && (iOpen(_Symbol,tfx,0)<=BSma02[0]))
     {mamup=true; mamdn=false;}
   else if((BSma02[0]<=BSma02[1]) && (BSma06[0]<BSma14[0]) && (BSma14[0]<BB200[0]) && (BB200[0]<BB200[1]) && (iOpen(_Symbol,tfx,0)>=BSma02[0]))
     {mamdn=true; mamup=false;}
   else if((BSma02[0]>BSma02[1]) && (BSma02[0]>BB201[0]) && (mabs0>mabs1) && (mabu0>mabu1) && (iOpen(_Symbol,tfx,0)>BSma02[0]))
     {mamup=true; mamdn=false;}
   else if((BSma02[0]<BSma02[1]) && (BSma02[0]<BB202[0]) && (mabs0<mabs1) && (mabd0<mabd1) && (iOpen(_Symbol,tfx,0)<BSma02[0]))
     {mamdn=true; mamup=false;}
   else if((BSma30[0]>BSma30[1]) && (BB200[0]>BB200[1]) && (BB200[0]>BSma30[0]) && (BSma02[0]>BB200[0]) && (BSma02[0]>BSma02[1]))
     {mamup=true; mamdn=false;}
   else if((BSma30[0]<BSma30[1]) && (BB200[0]<BB200[1]) && (BB200[0]<BSma30[0]) && (BSma02[0]<BB200[0]) && (BSma02[0]<BSma02[1]))
     {mamdn=true; mamup=false;}
   else if((BSma02[0]>=BSma02[1]) && (mb230>mb231) && (BB200[0]<BSma30[0]) && (BSma02[0]>BSma30[0]))
     {mamup=true; mamdn=false;}
   else if((BSma02[0]<=BSma02[1]) && (mb230<mb231) && (BB200[0]>BSma30[0]) && (BSma02[0]<BSma30[0]))
     {mamdn=true; mamup=false;}
   else {mamdn=false; mamup=false; return;}
//--
   if((macddn==true) && (BSma02[0]<BSma06[0]) && (mamdn))
     {bool WaveDn=true;}
   else if((macdup==true) && (BSma02[0]>BSma06[0]) && (mamup))
     {bool WaveUp=true;}
   else if((!WaveUp) && (!WaveDn)) {return;}
//--
   bsOpen=(iHigh(_Symbol,tfx,1)+iLow(_Symbol,tfx,1)+iClose(_Symbol,tfx,1)+iClose(_Symbol,tfx,1))/4;
   bsClos=(iHigh(_Symbol,tfx,0)+iLow(_Symbol,tfx,0)+iClose(_Symbol,tfx,0)+iClose(_Symbol,tfx,0))/4;
//-
   RefreshRates();
   if((WaveDn==true) && (bsClos<bsOpen) && (Close[0]<BSma02[0]) && 
      (iClose(_Symbol,0,0)<iOpen(_Symbol,0,0))) {bool OpsDn=true;} // price goes down
   else if((WaveUp==true) && (bsClos>bsOpen) && (Close[0]>BSma02[0]) && 
      (iClose(_Symbol,0,0)>iOpen(_Symbol,0,0))) {bool OpsUp=true;} // price goes up
   else if((!OpsUp) && (!OpsDn)) {bool OpsNt=true; OpsUp=false; OpsDn=false; return;}
//--
   TPSet();
   bool limitH=(iHigh(_Symbol,tfx,0)<=(BSma02[0]+(tp*0.66)));
   bool limitL=(iLow(_Symbol,tfx,0)>=(BSma02[0]-(tp*0.66)));
//--
   if((OpsDn==true) && (limitL))
     {SlashDn=true; SlashUp=false; CdS=1; RedemDn=true; RedemUp=false; RdS=1;}
   if((OpsUp==true) && (limitH))
     {SlashUp=true; SlashDn=false; CdB=1; RedemUp=true; RedemDn=false; RdB=1;}
   else if((!SlashDn) && (!SlashUp))
     {SlashNt=true; SlashUp=false; SlashDn=false; CdB=0; CdS=0; RedemNt=true; RedemUp=false; RedemDn=false; RdB=0; RdS=0;}
//-- prepare to redemption
//--
   CheckOpen();
   if((ob+os>0) && (redem) && (!RedemNt) && (!NoOrder))
     {FM1Redemption();}
//--
   RefreshRates();
   if(!NoOrder)
     {
      //--
      if((SlashUp==true)&&(CdB>0)&&(!SlashNt)) {ConsBuy=true;}
      if((SlashDn==true)&&(CdS>0)&&(!SlashNt)) {ConsSell=true;}
      //--
     }
//--
   if(ConsBuy) {OpenBUY();} // Open New Order BUY
                            //--
   if(ConsSell) {OpenSELL();} // Open New Order SELL
                              //--
//---
   return;
//----
  } //--end FMOneCalculation()
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void FM1Redemption() //-- function: Check and close if order is loss
  {                //-- and open opposite order
//----
   int error;
   bool result;
   TPSet();
   LotOptimize();
   totalpft=OrdersTotal()-1;
   for(ip=totalpft; ip>=0; ip--)
     {
      if(OrderSelect(ip,SELECT_BY_POS,MODE_TRADES)==True)
        {
         if(OrderSymbol()==_Symbol && OrderMagicNumber()==FMOneMagic)
           {
            if(OrderType()==OP_BUY)
              {
               //---
               if((RedemDn==true) && (RdS>0))
                 {
                  //--
                  RefreshRates();
                  bid=MarketInfo(_Symbol,MODE_BID);
                  if(bid<=OrderOpenPrice()) {redlotsS=OrderLots()*LotsRedempt;}
                  else {redlotsS=OrderLots()*1.0;}
                  rdu=NormalizeDouble(redlotsS/OrderLots(),2);
                  result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrYellow);
                  if(result!=true) {error=GetLastError();}
                  if(error>0)
                    {
                     ResetLastError();
                     RefreshRates();
                     bid=MarketInfo(_Symbol,MODE_BID);
                     result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrYellow);
                    }
                  //--
                  if(error==0 && result==true)
                    {
                     Print("-----FMOneEA has CLOSED BUY ORDER ",_Symbol," - Forcibly closed BUY to Redemption..OK!");
                     PlaySound("ping.wav");
                     Sleep(500);
                     RefreshRates();
                     OpenSELL();
                     break;
                    }
                  //--- end Forcibly closed BUY
                 }
               //--- end Redemption BUY
              }
            //--- end OrderType() BUY
            //----
            if(OrderType()==OP_SELL)
              {
               //---
               if((RedemUp==true) && (RdB>0))
                 {
                  //--
                  RefreshRates();
                  ask=MarketInfo(_Symbol,MODE_ASK);
                  if(ask>=OrderOpenPrice()) {redlotsB=OrderLots()*LotsRedempt;}
                  else {redlotsB=OrderLots()*1.0;}
                  rdd=NormalizeDouble(redlotsB/OrderLots(),2);
                  result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                  if(result!=true) {error=GetLastError();}
                  if(error>0)
                    {
                     ResetLastError();
                     RefreshRates();
                     ask=MarketInfo(_Symbol,MODE_ASK);
                     result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                    }
                  //--
                  if(error==0 && result==true)
                    {
                     Print("-----FMOneEA has CLOSED SELL ORDER ",_Symbol," - Forcibly closed SELL to Redemption..OK!");
                     PlaySound("ping.wav");
                     Sleep(500);
                     RefreshRates();
                     OpenBUY();
                     break;
                    }
                  //--- end Forcibly closed SELL
                 }
               //--- end Redemption SELL
              }
            //--- end OrderType() SELL
           }
         //--- end Order_Symbol
        }
      //--- end OrderSelect 
     }
//--- end for(ip)
   return;
//----
  } //-end FM1Redemption()
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OpenBUY() //-- function: Open order if price was up
  {
//----
   CheckOpen();
   LotOptimize();
   SLSet();
   TPSet();
   CommentBuy=StringConcatenate(WindowExpertName()," #BUY#ins",(string)CdB); // buy orders comments
   if(rdd>0.0) {double OpLots=redlotsB;}
   if((rdd==0.0) && (dblLot>0.0)) {OpLots=dblLot;}
   if((rdd==0.0) && (dblLot==0.0)) {OpLots=lot;}
   if(OpLots>(mxlr*lot)) {OpLots=mxlr*lot;}
   if(redem) {bool alwbu=ob+os==0;}
   if(!redem) {alwbu=ob==0;}
//--
   if(ob+os==0)
     {
      //-- Error checking
      if((AccountFreeMarginCheck(_Symbol,0,OpLots)<=0) || (GetLastError()==134))
        {Print("-----OPEN NEW BUY ORDER ",_Symbol," - NOT ENOUGH MONEY..!"); ResetLastError(); return;}
      //--
      else
        {
         //--
         ResetLastError();
         int error;
         bool result;
         RefreshRates();
         ask=MarketInfo(_Symbol,MODE_ASK);
         bid=MarketInfo(_Symbol,MODE_BID);
         lastBid=ask;
         slA=bid-sl;
         tpA=ask+tp;
         //--
         result=OrderSend(_Symbol,OP_BUY,OpLots,ask,dev,slA,tpA,CommentBuy,FMOneMagic,0,clrGreen);
         //--
         Sleep(1000);
         RefreshRates();
         CheckOpen();
         if(result!=TRUE) {error=GetLastError();}
         if((error>0) || (result!=true) || (ob==0))
           {
            //--
            ResetLastError();
            Sleep(2000);
            RefreshRates();
            ask=MarketInfo(_Symbol,MODE_ASK);
            bid=MarketInfo(_Symbol,MODE_BID);
            lastBid=ask;
            slA=bid-sl;
            tpA=ask+tp;
            result=OrderSend(_Symbol,OP_BUY,OpLots,ask,dev,slA,tpA,CommentBuy,FMOneMagic,0,clrGreen);
            error=GetLastError();
            if(error>0)
              {
               Print("Failed to Open New BUY ORDER ",_Symbol,"! Error code = ",
                     GetLastError(),", ",ErrorDescription(error));
               ResetLastError();
               return;
              }
           }
         //--
         if(error==0 && result==true)
           {
            Print("-----FMOneEA has Opened New BUY ORDER ",
                  _Symbol,", Buy Instruction: ",CdB,", Redemption: ",rdd,", - OK!");
            PlaySound("gun.wav");
            hb=1;
            trxb=0;
            rdd=0;
            dblLot=0;
           }
        }
     }
//--
   return;
//----
  } //-end OpenBUY() 
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OpenSELL() //-- function: Open order if price was down
  {
//----
   CheckOpen();
   LotOptimize();
   SLSet();
   TPSet();
   CommentSell=StringConcatenate(WindowExpertName()," #SELL#ins",(string)CdS); // sell orders comments
   if(rdu>0.0) {double OpLots=redlotsS;}
   if((rdu==0.0) && (dblLot>0.0)) {OpLots=dblLot;}
   if((rdu==0.0) && (dblLot==0.0)) {OpLots=lot;}
   if(OpLots>(mxlr*lot)) {OpLots=mxlr*lot;}
   if(redem) {bool alwse=os+ob==0;}
   if(!redem) {alwse=os==0;}
//--
   if(ob+os==0)
     {
      //-- Error checking
      if((AccountFreeMarginCheck(_Symbol,1,OpLots)<=0) || (GetLastError()==134))
        {Print("-----OPEN NEW SELL ORDER ",_Symbol," - NOT ENOUGH MONEY..!"); ResetLastError(); return;}
      else
        {
         //--
         ResetLastError();
         int error;
         bool result;
         RefreshRates();
         ask=MarketInfo(_Symbol,MODE_ASK);
         bid=MarketInfo(_Symbol,MODE_BID);
         lastAsk=bid;
         slB=ask+sl;
         tpB=bid-tp;
         //--
         result=OrderSend(_Symbol,OP_SELL,OpLots,bid,dev,slB,tpB,CommentSell,FMOneMagic,0,clrRed);
         //--
         Sleep(1000);
         RefreshRates();
         CheckOpen();
         if(result!=true) {error=GetLastError();}
         if((error>0) || (result!=true) || (os==0))
           {
            //--
            ResetLastError();
            Sleep(2000);
            RefreshRates();
            ask=MarketInfo(_Symbol,MODE_ASK);
            bid=MarketInfo(_Symbol,MODE_BID);
            lastAsk=bid;
            slB=ask+sl;
            tpB=bid-tp;
            result=OrderSend(_Symbol,OP_SELL,OpLots,bid,dev,slB,tpB,CommentSell,FMOneMagic,0,clrRed);
            error=GetLastError();
            if(error>0)
              {
               Print("Failed to Open New SELL ORDER ",_Symbol,"! Error code = ",
                     GetLastError(),", ",ErrorDescription(error));
               ResetLastError();
               return;
              }
           }
         //--
         if(error==0 && result==true)
           {
            Print("-----FMOneEA has Opened New SELL ORDER ",
                  _Symbol,", Sell Instruction: ",CdS,", Redemption: ",rdu,", - OK!");
            PlaySound("gun.wav");
            hs=1;
            trxs=0;
            rdu=0;
            dblLot=0;
           }
        }
     }
//--
   return;
//----
  } //-end OpenSELL()
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TpSlTrPrep() //-- function: Check if order is profit.
  {
//----
   SLSet();
   TPSet();
   int error;
   bool result,trest;
   totalpft=OrdersTotal()-1;
   for(ip=totalpft; ip>=0; ip--)
     {
      if(OrderSelect(ip,SELECT_BY_POS,MODE_TRADES)==True)
        {
         if(OrderSymbol()==_Symbol && OrderMagicNumber()==FMOneMagic)
           {
            if(OrderType()==0)
              {
               //---
               RefreshRates();
               bid=MarketInfo(_Symbol,MODE_BID);
               if((opttp) && (!nomintp) && ((OrderOpenPrice()<(bid-tpAB)) && (mdHC || mdUp))) //-check if order buy is profit
                 {
                  RefreshRates();
                  bid=MarketInfo(_Symbol,MODE_BID);
                  result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                  if(result!=true) {error=GetLastError();}
                  if(error>0)
                    {
                     ResetLastError();
                     RefreshRates();
                     bid=MarketInfo(_Symbol,MODE_BID);
                     result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                    }
                  //--
                  if(error==0 && result==true)
                    {
                     Print("-----FMOneEA has CLOSED BUY ORDER ",_Symbol," - Automatic Min Take profit..OK!");
                     PlaySound("ping.wav");
                     break;
                    }
                 } //-end BUY Order Profit minimum TP
               //--                       
               RefreshRates();
               bid=MarketInfo(_Symbol,MODE_BID);
               if((opttp) && (nomintp) && (frcClB) && (OrderOpenPrice()<(bid-divAB))) //-check if order buy is profit 
                 {
                  RefreshRates();
                  bid=MarketInfo(_Symbol,MODE_BID);
                  result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                  if(result!=true) {error=GetLastError();}
                  if(error>0)
                    {
                     ResetLastError();
                     RefreshRates();
                     bid=MarketInfo(_Symbol,MODE_BID);
                     result=OrderClose(OrderTicket(),OrderLots(),bid,dev,clrAqua);
                    }
                  //--
                  if(error==0 && result==true)
                    {
                     Print("-----FMOneEA has CLOSED BUY ORDER ",_Symbol," - Forcibly closed Automatic Take profit..OK!");
                     PlaySound("ping.wav");
                     break;
                    }
                 } //-end BUY Order Profit ZigZag TP
               //-- Trailing Stop & Step
               ResetLastError();
               RefreshRates();
               bid=MarketInfo(_Symbol,MODE_BID);
               if((scbep) && (bid>(OrderOpenPrice()+beplmt)) && (beh==0))
                 {trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+bep,OrderTakeProfit(),0); beh++;}
               if(TrlStp && (trxb==0) && (bid>(OrderOpenPrice()+tstpv)))
                 {
                  trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+trstart,OrderTakeProfit(),0);
                  trxb++;
                 }
               //--
               if(TrlStp && (trxb>0) && (bid>(OrderStopLoss()+trstart)))
                 {
                  trxb++;
                  if(opttp) {double otpb=OrderTakeProfit()+(step*pp);}
                  else {otpb=OrderTakeProfit();}
                  trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+(trxb*tsstv),otpb,0);
                 } // end BUY Order Trailing Stop.
               //---
              }
            //----
            if(OrderType()==1)
              {
               //---
               RefreshRates();
               ask=MarketInfo(_Symbol,MODE_ASK);
               if((opttp) && (!nomintp) && ((OrderOpenPrice()>(ask+tpAB)) && (mdLC || mdDn))) //-check if order sell is profit
                 {
                  RefreshRates();
                  ask=MarketInfo(_Symbol,MODE_ASK);
                  result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                  if(result!=true) {error=GetLastError();}
                  if(error>0)
                    {
                     ResetLastError();
                     RefreshRates();
                     ask=MarketInfo(_Symbol,MODE_ASK);
                     result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                    }
                  //--
                  if(error==0 && result==true)
                    {
                     Print("-----FMOneEA has CLOSED SELL ORDER ",_Symbol," - Automatic Min Take profit..OK!");
                     PlaySound("ping.wav");
                     break;
                    }
                 } //-end SELL Order Profit minimum TP
               //--
               RefreshRates();
               ask=MarketInfo(_Symbol,MODE_ASK);
               if((opttp) && (nomintp) && (frcClS) && (OrderOpenPrice()>(ask+divAB))) //-check if order sell is profit
                 {
                  RefreshRates();
                  ask=MarketInfo(_Symbol,MODE_ASK);
                  result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                  if(result!=true) {error=GetLastError();}
                  if(error>0)
                    {
                     ResetLastError();
                     RefreshRates();
                     ask=MarketInfo(_Symbol,MODE_ASK);
                     result=OrderClose(OrderTicket(),OrderLots(),ask,dev,clrYellow);
                    }
                  //--
                  if(error==0 && result==true)
                    {
                     Print("-----FMOneEA has CLOSED SELL ORDER ",_Symbol," - Forcibly closed Automatic Take profit..OK!");
                     PlaySound("ping.wav");
                     break;
                    }
                 } //-end SELL Order Profit ZigZag TP                         
               //-- Trailing Stop & Step
               ResetLastError();
               RefreshRates();
               ask=MarketInfo(_Symbol,MODE_ASK);
               if((scbep) && (ask<(OrderOpenPrice()-beplmt)) && (bel==0))
                 {trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-bep,OrderTakeProfit(),0); bel++;}
               if(TrlStp && (trxs==0) && (ask<(OrderOpenPrice()-tstpv)))
                 {
                  trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-trstart,OrderTakeProfit(),0);
                  trxs++;
                 }
               //--
               if(TrlStp && (trxs>0) && (ask<(OrderStopLoss()-trstart)))
                 {
                  trxs++;
                  if(opttp) {double otps=OrderTakeProfit()-(step*pp);}
                  else {otps=OrderTakeProfit();}
                  trest=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-(trxs*tsstv),otps,0);
                 } // end SELL Order Trailing Stop.                   
               //----
              }
           }
        }
     }
//---
   return;
//----
  } //-end TpSlTrPrep()
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void FMOneStart() //-- function: start trading for calculation order
  {
//----
   CheckOpen();
   if((ob+os>0) && (opttp || autsl || TrlStp))
     {TpSlTrPrep();}
   RefreshRates();
   FMOneCalculation();
//--
   RefreshRates();
   CheckClose();
//---
   return;
//---- 
  } //-end FMOneStart()
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();
   GlobalVariablesDeleteAll();
//--
   return;
//----
  } //-end OnDeinit()
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//----
//--
   RefreshRates();
   MqlTick last_tick;
   time=last_tick.time;
   ask=last_tick.ask;
   bid=last_tick.bid;
   volume=last_tick.volume;
//--
//----
   RefreshRates();
   if(IsTradeAllowed()==false) {return;}
   if(DayOfWeek()==0 || DayOfWeek()==6) {NoOrder=true; return;}
   if(FridayTrade==false && DayOfWeek()==5) {NoOrder=true; return;}
   if(Hour()==0) {NoOrder=true; FMOneStart();}
   else {NoOrder=false; FMOneStart();}
//--
   return;
//----
  } //-end OnTick()
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
//---
   MqlRates rate1[];
   ArraySetAsSeries(rate1,true);
   coprt=CopyRates(_Symbol,tfx,0,cop1,rate1);
   if(coprt==0) return;
//---
  }
//+------------------------------------------------------------------+
