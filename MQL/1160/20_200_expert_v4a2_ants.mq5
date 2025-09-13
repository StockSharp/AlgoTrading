//+------------------------------------------------------------------+
//|                                      20_200 expert_v4.2_AntS.mq5 |
//|                                          Copyright 2012, Integer |
//|                          https://login.mql5.com/ru/users/Integer |
//+------------------------------------------------------------------+
#property copyright "Integer"
#property link "https://login.mql5.com/ru/users/Integer"
#property description "Expert rewritten from MQL4, the author is http://www.mql4.com/ru/users/AntS, link to original - http://codebase.mql4.com/ru/2629"
#property version   "1.00"

#include <Trade/Trade.mqh>
#include <Trade/SymbolInfo.mqh>
#include <Trade/DealInfo.mqh>
#include <Trade/PositionInfo.mqh>

#include <Trade/AccountInfo.mqh>

CTrade Trade;
CDealInfo Deal;
CSymbolInfo Sym;
CPositionInfo Pos;
CAccountInfo Ac;

//--- input parameters

input int      t1                =  6;          /*t1*/               // shift of one bar
input int      t2                =  2;          /*t2*/               // shift of second bar 
input int      Delta_L           =  6;          /*Delta_L*/          // difference of price of the first and second bars for open the long position(buy)
input int      Delta_S           =  21;         /*Delta_S*/          // difference of price of the first and second bars for open the short position(sell)
input int      TakeProfit_L      =  390;        /*TakeProfit_L*/     // Takeprofit of long position in points
input int      StopLoss_L        =  1470;       /*StopLoss_L*/       // Stoploss of long position in points
input int      TakeProfit_S      =  320;        /*TakeProfit_S*/     // Takeprofit of short position in points
input int      StopLoss_S        =  2670;       /*StopLoss_S*/       // Stoploss of short position in points
input double   Lots              =  0.1;        /*Lots*/             // Volume of start position at AutoLot=false
input bool     AutoLot           =  true;       /*AutoLot*/          // Inclusion of proportional lot
input double   BigLotSize        =  6;          /*BigLotSize*/       // lot multiplication coefficient after losing
input bool     OneMult           =  true;       /*OneMult*/          // one lot multiplication. After losing, the lot is multiplied. With such a lot the expert opens the positions before the profit obtaining. If false - the multiplication is performed for each newly opened position
input int      TradeTime         =  14;         /*TradeTime*/        // Hour for entering to the market
input int      MaxOpenTime       =  504;        /*MaxOpenTime*/      // Maximum time of position existence (in hours)

double op1[1],op2[1];
datetime ctm[1];
datetime LastTime;
double lots,lots2,slv,msl,tpv,mtp;
string gvp;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit(){

   // Preparation of global variables names
   gvp=MQL5InfoString(MQL5_PROGRAM_NAME)+"_"+_Symbol+"_"+IntegerToString(PeriodSeconds()/60)+"_"+IntegerToString(AccountInfoInteger(ACCOUNT_LOGIN));
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_DEMO)gvp=gvp+"_d";
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_REAL)gvp=gvp+"_r";
   if(MQL5InfoInteger(MQL5_TESTING))gvp=gvp+"_t";
   DeleteGV();

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
   DeleteGV();   
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){

   if(CopyTime(_Symbol,PERIOD_CURRENT,0,1,ctm)==-1){
      return;
   }
   
   if(MaxOpenTime>0){
      if(Pos.Select(_Symbol)){
         double tmp=(TimeCurrent()-Pos.Time())/3600.0;
         if(((NormalizeDouble(tmp,8)-MaxOpenTime)>=0)){   
            if(!Trade.PositionClose(_Symbol,Sym.Spread()*3)){
               return;
            }
         }
      }
   }   
   
   if(ctm[0]!=LastTime){
      
      // Indicators
      if(!Indicators()){
         return;
      }   
      
      // Signals
      bool OpenBuy=SignalOpenBuy();
      bool OpenSell=SignalOpenSell();

      // Open
      if(!Pos.Select(_Symbol)){
         MqlDateTime dt;
         TimeToStruct(TimeCurrent(),dt);
            if(dt.hour==TradeTime){
               if(OpenBuy && !OpenSell){ 
                  if(!Sym.RefreshRates())return;         
                  if(!SolveLots(lots,lots2))return;
                  
                  slv=SolveBuySL(StopLoss_L);
                  tpv=SolveBuyTP(TakeProfit_L);
                     if(CheckBuySL(slv) && CheckBuyTP(tpv)){
                        Trade.SetDeviationInPoints(Sym.Spread()*3);
                           if(Trade.Buy(lots,_Symbol,0,slv,tpv,"")){
                              GlobalVariableSet(gvp+"globalBalans",Ac.Balance());
                                 if(OneMult){
                                    GlobalVariableSet(gvp+"PreLots",lots2);
                                 }
                                 else{
                                    GlobalVariableSet(gvp+"PreLots",lots);
                                 }
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
               if(OpenSell && !OpenBuy){
                  if(!Sym.RefreshRates())return;         
                  if(!SolveLots(lots,lots2))return;
                  slv=SolveSellSL(StopLoss_S);
                  tpv=SolveSellTP(TakeProfit_S);
                     if(CheckSellSL(slv) && CheckSellTP(tpv)){
                        Trade.SetDeviationInPoints(Sym.Spread()*3);
                           if(Trade.Sell(lots,_Symbol,0,slv,tpv,"")){
                              GlobalVariableSet(gvp+"globalBalans",Ac.Balance());
                                 if(OneMult){
                                    GlobalVariableSet(gvp+"PreLots",lots2);
                                 }
                                 else{
                                    GlobalVariableSet(gvp+"PreLots",lots);
                                 }
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
      }            
      LastTime=ctm[0];
   }

}

//+------------------------------------------------------------------+
//|   Function of data copy for indicators and price                 |
//+------------------------------------------------------------------+
bool Indicators(){
   if(
      CopyOpen(_Symbol,PERIOD_CURRENT,t1,1,op1)==-1 ||
      CopyOpen(_Symbol,PERIOD_CURRENT,t2,1,op2)==-1
   )return(false);
   return(true);
}

//+------------------------------------------------------------------+
//|   Function for determining buy signals                           |
//+------------------------------------------------------------------+
bool SignalOpenBuy(){
   return(op2[0]-op1[0]>_Point*Delta_L);
}

//+------------------------------------------------------------------+
//|   Function for determining sell signals                          |
//+------------------------------------------------------------------+
bool SignalOpenSell(){
   return(op1[0]-op2[0]>_Point*Delta_S);
   return(false);
}

//+------------------------------------------------------------------+
//|   Function for determining buy close signals                     |
//+------------------------------------------------------------------+
bool SignalCloseBuy(){

   return (false);
}

//+------------------------------------------------------------------+
//|   Function for determining sell close signals                    |
//+------------------------------------------------------------------+
bool SignalCloseSell(){

   return (false);
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
bool SolveLots(double & aLots,double & aLots2){
   aLots=Lots;
      if(AutoLot){
         aLots=LotSize();      
      }
   aLots2=aLots;
      if(GlobalVariableCheck(gvp+"globalBalans")){
         Print("zzzzzzzzzzzzzzz2");
         if(GlobalVariableGet(gvp+"globalBalans")>Ac.Balance()){
               if(AutoLot){
                  if(GlobalVariableCheck(gvp+"PreLots")){
                     aLots=GlobalVariableGet(gvp+"PreLots");
                  }
               }   
            Print("zzzzzzzzzzzzzzz");
            aLots*=BigLotSize;
            aLots=fLotsNormalize(aLots);
         }            
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
//|   Function of lot determination                                  |
//+------------------------------------------------------------------+
double LotSize(){
   double AcBal=Ac.Balance();
   double lot=Lots;
   if (AcBal>=300) lot=0.01;
   if (AcBal>=500) lot=0.02;
   if (AcBal>=800) lot=0.03;
   if (AcBal>=1000) lot=0.04;
   if (AcBal>=1300) lot=0.05;
   if (AcBal>=1600) lot=0.06;
   if (AcBal>=1800) lot=0.07;
   if (AcBal>=2100) lot=0.08;
   if (AcBal>=2400) lot=0.09;
   if (AcBal>=2700) lot=0.10;
   if (AcBal>=3000) lot=0.11;
   if (AcBal>=3300) lot=0.12;
   if (AcBal>=3500) lot=0.13;
   if (AcBal>=3785) lot=0.14;
   if (AcBal>=4058) lot=0.15;
   if (AcBal>=4332) lot=0.16;
   if (AcBal>=4605) lot=0.17;
   if (AcBal>=4879) lot=0.18;
   if (AcBal>=5153) lot=0.19;
   if (AcBal>=5626) lot=0.20;
   if (AcBal>=5700) lot=0.21;
   if (AcBal>=5974) lot=0.22;
   if (AcBal>=6247) lot=0.23;
   if (AcBal>=6521) lot=0.24;
   if (AcBal>=6795) lot=0.25;
   if (AcBal>=7068) lot=0.26;
   if (AcBal>=7342) lot=0.27;
   if (AcBal>=7615) lot=0.28;
   if (AcBal>=7889) lot=0.29;
   if (AcBal>=8163) lot=0.30;
   if (AcBal>=8436) lot=0.31;
   if (AcBal>=8710) lot=0.32;
   if (AcBal>=8984) lot=0.33;
   if (AcBal>=9257) lot=0.34;
   if (AcBal>=9531) lot=0.35;
   if (AcBal>=9804) lot=0.36;
   if (AcBal>=10078) lot=0.37;
   if (AcBal>=10352) lot=0.38;
   if (AcBal>=10625) lot=0.39;
   if (AcBal>=10899) lot=0.40;
   if (AcBal>=11173) lot=0.41;
   if (AcBal>=11446) lot=0.42;
   if (AcBal>=11720) lot=0.43;
   if (AcBal>=11993) lot=0.44;
   if (AcBal>=12267) lot=0.45;
   if (AcBal>=12541) lot=0.46;
   if (AcBal>=12814) lot=0.47;
   if (AcBal>=13088) lot=0.48;
   if (AcBal>=13362) lot=0.49;
   if (AcBal>=13635) lot=0.50;
   if (AcBal>=13909) lot=0.51;
   if (AcBal>=14182) lot=0.52;
   if (AcBal>=14456) lot=0.53;
   if (AcBal>=14730) lot=0.54;
   if (AcBal>=15003) lot=0.55;
   if (AcBal>=15277) lot=0.56;
   if (AcBal>=15551) lot=0.57;
   if (AcBal>=15824) lot=0.58;
   if (AcBal>=16098) lot=0.59;
   if (AcBal>=16371) lot=0.60;
   if (AcBal>=16645) lot=0.61;
   if (AcBal>=16919) lot=0.62;
   if (AcBal>=17192) lot=0.63;
   if (AcBal>=17466) lot=0.64;
   if (AcBal>=17740) lot=0.65;
   if (AcBal>=18013) lot=0.66;
   if (AcBal>=18287) lot=0.67;
   if (AcBal>=18560) lot=0.68;
   if (AcBal>=18834) lot=0.69;
   if (AcBal>=19108) lot=0.70;
   if (AcBal>=19381) lot=0.71;
   if (AcBal>=19655) lot=0.72;
   if (AcBal>=19929) lot=0.73;
   if (AcBal>=20202) lot=0.74;
   if (AcBal>=20476) lot=0.75;
   if (AcBal>=20749) lot=0.76;
   if (AcBal>=21023) lot=0.77;
   if (AcBal>=21297) lot=0.78;
   if (AcBal>=21570) lot=0.79;
   if (AcBal>=21844) lot=0.80;
   if (AcBal>=22118) lot=0.81;
   if (AcBal>=22391) lot=0.82;
   if (AcBal>=22665) lot=0.83;
   if (AcBal>=22938) lot=0.84;
   if (AcBal>=23212) lot=0.85;
   if (AcBal>=23486) lot=0.86;
   if (AcBal>=23759) lot=0.87;
   if (AcBal>=24033) lot=0.88;
   if (AcBal>=24307) lot=0.89;
   if (AcBal>=24580) lot=0.90;
   if (AcBal>=24854) lot=0.91;
   if (AcBal>=25127) lot=0.92;
   if (AcBal>=25401) lot=0.93;
   if (AcBal>=25675) lot=0.94;
   if (AcBal>=25948) lot=0.95;
   if (AcBal>=26222) lot=0.96;
   if (AcBal>=26496) lot=0.97;
   if (AcBal>=26795) lot=0.98;
   if (AcBal>=27043) lot=0.99;
   if (AcBal>=27316) lot=1.00;
   if (AcBal>=27590) lot=1.01;
   if (AcBal>=27864) lot=1.02;
   if (AcBal>=28137) lot=1.03;
   if (AcBal>=28411) lot=1.04;
   if (AcBal>=28685) lot=1.05;
   if (AcBal>=28958) lot=1.06;
   if (AcBal>=29232) lot=1.07;
   if (AcBal>=29505) lot=1.08;
   if (AcBal>=29779) lot=1.09;
   if (AcBal>=30053) lot=1.10;
   if (AcBal>=30326) lot=1.11;
   if (AcBal>=30600) lot=1.12;
   if (AcBal>=30874) lot=1.13;
   if (AcBal>=31147) lot=1.14;
   if (AcBal>=31421) lot=1.15;
   if (AcBal>=31695) lot=1.16;
   if (AcBal>=31968) lot=1.17;
   if (AcBal>=32242) lot=1.18;
   if (AcBal>=32515) lot=1.19;
   if (AcBal>=32789) lot=1.20;
   if (AcBal>=33063) lot=1.21;
   if (AcBal>=33336) lot=1.22;
   if (AcBal>=33610) lot=1.23;
   if (AcBal>=33884) lot=1.24;
   if (AcBal>=34157) lot=1.25;
   if (AcBal>=34431) lot=1.26;
   if (AcBal>=34704) lot=1.27;
   if (AcBal>=34978) lot=1.28;
   if (AcBal>=35252) lot=1.29;
   if (AcBal>=35525) lot=1.30;
   if (AcBal>=35799) lot=1.31;
   if (AcBal>=36073) lot=1.32;
   if (AcBal>=36346) lot=1.33;
   if (AcBal>=36620) lot=1.34;
   if (AcBal>=36893) lot=1.35;
   if (AcBal>=37167) lot=1.36;
   if (AcBal>=37441) lot=1.37;
   if (AcBal>=	37714	) lot=	1.38	;
   if (AcBal>=	37988	) lot=	1.39	;
   if (AcBal>=	38262	) lot=	1.40	;
   if (AcBal>=	38535	) lot=	1.41	;
   if (AcBal>=	38809	) lot=	1.42	;
   if (AcBal>=	39082	) lot=	1.43	;
   if (AcBal>=	39356	) lot=	1.44	;
   if (AcBal>=	39630	) lot=	1.45	;
   if (AcBal>=	39903	) lot=	1.46	;
   if (AcBal>=	40177	) lot=	1.47	;
   if (AcBal>=	40451	) lot=	1.48	;
   if (AcBal>=	40724	) lot=	1.49	;
   if (AcBal>=	40998	) lot=	1.50	;
   if (AcBal>=	41271	) lot=	1.51	;
   if (AcBal>=	41545	) lot=	1.52	;
   if (AcBal>=	41819	) lot=	1.53	;
   if (AcBal>=	42092	) lot=	1.54	;
   if (AcBal>=	42366	) lot=	1.55	;
   if (AcBal>=	42640	) lot=	1.56	;
   if (AcBal>=	42913	) lot=	1.57	;
   if (AcBal>=	43187	) lot=	1.58	;
   if (AcBal>=	43460	) lot=	1.59	;
   if (AcBal>=	43734	) lot=	1.60	;
   if (AcBal>=	44008	) lot=	1.61	;
   if (AcBal>=	44281	) lot=	1.62	;
   if (AcBal>=	44555	) lot=	1.63	;
   if (AcBal>=	44829	) lot=	1.64	;
   if (AcBal>=	45102	) lot=	1.65	;
   if (AcBal>=	45376	) lot=	1.66	;
   if (AcBal>=	45649	) lot=	1.67	;
   if (AcBal>=	45923	) lot=	1.68	;
   if (AcBal>=	46197	) lot=	1.69	;
   if (AcBal>=	46470	) lot=	1.70	;
   if (AcBal>=	46744	) lot=	1.71	;
   if (AcBal>=	47018	) lot=	1.72	;
   if (AcBal>=	47291	) lot=	1.73	;
   if (AcBal>=	47565	) lot=	1.74	;
   if (AcBal>=	47838	) lot=	1.75	;
   if (AcBal>=	48112	) lot=	1.76	;
   if (AcBal>=	48386	) lot=	1.77	;
   if (AcBal>=	48659	) lot=	1.78	;
   if (AcBal>=	48933	) lot=	1.79	;
   if (AcBal>=	49207	) lot=	1.80	;
   if (AcBal>=	49480	) lot=	1.81	;
   if (AcBal>=	49754	) lot=	1.82	;
   if (AcBal>=	50027	) lot=	1.83	;
   if (AcBal>=	50301	) lot=	1.84	;
   if (AcBal>=	50575	) lot=	1.85	;
   if (AcBal>=	50848	) lot=	1.86	;
   if (AcBal>=	51122	) lot=	1.87	;
   if (AcBal>=	51396	) lot=	1.88	;
   if (AcBal>=	51669	) lot=	1.89	;
   if (AcBal>=	51943	) lot=	1.90	;
   if (AcBal>=	52216	) lot=	1.91	;
   if (AcBal>=	52490	) lot=	1.92	;
   if (AcBal>=	52764	) lot=	1.93	;
   if (AcBal>=	53037	) lot=	1.94	;
   if (AcBal>=	53311	) lot=	1.95	;
   if (AcBal>=	53585	) lot=	1.96	;
   if (AcBal>=	53858	) lot=	1.97	;
   if (AcBal>=	54132	) lot=	1.98	;
   if (AcBal>=	54405	) lot=	1.99	;
   if (AcBal>=	54679	) lot=	2.00	;
   if (AcBal>=	54953	) lot=	2.01	;
   if (AcBal>=	55226	) lot=	2.02	;
   if (AcBal>=	55500	) lot=	2.03	;
   if (AcBal>=	55774	) lot=	2.04	;
   if (AcBal>=	56047	) lot=	2.05	;
   if (AcBal>=	56321	) lot=	2.06	;
   if (AcBal>=	56595	) lot=	2.07	;
   if (AcBal>=	56868	) lot=	2.08	;
   if (AcBal>=	57142	) lot=	2.09	;
   if (AcBal>=	57415	) lot=	2.10	;
   if (AcBal>=	57689	) lot=	2.11	;
   if (AcBal>=	57963	) lot=	2.12	;
   if (AcBal>=	58236	) lot=	2.13	;
   if (AcBal>=	58510	) lot=	2.14	;
   if (AcBal>=	58784	) lot=	2.15	;
   if (AcBal>=	59057	) lot=	2.16	;
   if (AcBal>=	59331	) lot=	2.17	;
   if (AcBal>=	59604	) lot=	2.18	;
   if (AcBal>=	59878	) lot=	2.19	;
   if (AcBal>=	60152	) lot=	2.20	;
   if (AcBal>=	60425	) lot=	2.21	;
   if (AcBal>=	60699	) lot=	2.22	;
   if (AcBal>=	60973	) lot=	2.23	;
   if (AcBal>=	61246	) lot=	2.24	;
   if (AcBal>=	61520	) lot=	2.25	;
   if (AcBal>=	61793	) lot=	2.26	;
   if (AcBal>=	62067	) lot=	2.27	;
   if (AcBal>=	62341	) lot=	2.28	;
   if (AcBal>=	62614	) lot=	2.29	;
   if (AcBal>=	62888	) lot=	2.30	;
   if (AcBal>=	63162	) lot=	2.31	;
   if (AcBal>=	63435	) lot=	2.32	;
   if (AcBal>=	63709	) lot=	2.33	;
   if (AcBal>=	63982	) lot=	2.34	;
   if (AcBal>=	64256	) lot=	2.35	;
   if (AcBal>=	64530	) lot=	2.36	;
   if (AcBal>=	64803	) lot=	2.37	;
   if (AcBal>=	65077	) lot=	2.38	;
   if (AcBal>=	65351	) lot=	2.39	;
   if (AcBal>=	65624	) lot=	2.40	;
   if (AcBal>=	65898	) lot=	2.41	;
   if (AcBal>=	66171	) lot=	2.42	;
   if (AcBal>=	66445	) lot=	2.43	;
   if (AcBal>=	66719	) lot=	2.44	;
   if (AcBal>=	66992	) lot=	2.45	;
   if (AcBal>=	67266	) lot=	2.46	;
   if (AcBal>=	67540	) lot=	2.47	;
   if (AcBal>=	67813	) lot=	2.48	;
   if (AcBal>=	68087	) lot=	2.49	;
   if (AcBal>=	68360	) lot=	2.50	;
   if (AcBal>=	68634	) lot=	2.51	;
   if (AcBal>=	68908	) lot=	2.52	;
   if (AcBal>=	69181	) lot=	2.53	;
   if (AcBal>=	69455	) lot=	2.54	;
   if (AcBal>=	69729	) lot=	2.55	;
   if (AcBal>=	70002	) lot=	2.56	;
   if (AcBal>=	70276	) lot=	2.57	;
   if (AcBal>=	70549	) lot=	2.58	;
   if (AcBal>=	70823	) lot=	2.59	;
   if (AcBal>=	71097	) lot=	2.60	;
   if (AcBal>=	71370	) lot=	2.61	;
   if (AcBal>=	71644	) lot=	2.62	;
   if (AcBal>=	71918	) lot=	2.63	;
   if (AcBal>=	72191	) lot=	2.64	;
   if (AcBal>=	72465	) lot=	2.65	;
   if (AcBal>=	72738	) lot=	2.66	;
   if (AcBal>=	73012	) lot=	2.67	;
   if (AcBal>=	73286	) lot=	2.68	;
   if (AcBal>=	73559	) lot=	2.69	;
   if (AcBal>=	73833	) lot=	2.70	;
   if (AcBal>=	74107	) lot=	2.71	;
   if (AcBal>=	74380	) lot=	2.72	;
   if (AcBal>=	74654	) lot=	2.73	;
   if (AcBal>=	74927	) lot=	2.74	;
   if (AcBal>=	75201	) lot=	2.75	;
   if (AcBal>=	75475	) lot=	2.76	;
   if (AcBal>=	75748	) lot=	2.77	;
   if (AcBal>=	76022	) lot=	2.78	;
   if (AcBal>=	76296	) lot=	2.79	;
   if (AcBal>=	76569	) lot=	2.80	;
   if (AcBal>=	76843	) lot=	2.81	;
   if (AcBal>=	77116	) lot=	2.82	;
   if (AcBal>=	77390	) lot=	2.83	;
   if (AcBal>=	77664	) lot=	2.84	;
   if (AcBal>=	77937	) lot=	2.85	;
   if (AcBal>=	78211	) lot=	2.86	;
   if (AcBal>=	78485	) lot=	2.87	;
   if (AcBal>=	78758	) lot=	2.88	;
   if (AcBal>=	79032	) lot=	2.89	;
   if (AcBal>=	79305	) lot=	2.90	;
   if (AcBal>=	79579	) lot=	2.91	;
   if (AcBal>=	79853	) lot=	2.92	;
   if (AcBal>=	80126	) lot=	2.93	;
   if (AcBal>=	80400	) lot=	2.94	;
   if (AcBal>=	80674	) lot=	2.95	;
   if (AcBal>=	80947	) lot=	2.96	;
   if (AcBal>=	81221	) lot=	2.97	;
   if (AcBal>=	81495	) lot=	2.98	;
   if (AcBal>=	81768	) lot=	2.99	;
   if (AcBal>=	82042	) lot=	3.00	;
   if (AcBal>=	82315	) lot=	3.01	;
   if (AcBal>=	82589	) lot=	3.02	;
   if (AcBal>=	82863	) lot=	3.03	;
   if (AcBal>=	83136	) lot=	3.04	;
   if (AcBal>=	83410	) lot=	3.05	;
   if (AcBal>=	83684	) lot=	3.06	;
   if (AcBal>=	83957	) lot=	3.07	;
   if (AcBal>=	84231	) lot=	3.08	;
   if (AcBal>=	84504	) lot=	3.09	;
   if (AcBal>=	84778	) lot=	3.10	;
   if (AcBal>=	85052	) lot=	3.11	;
   if (AcBal>=	85325	) lot=	3.12	;
   if (AcBal>=	85599	) lot=	3.13	;
   if (AcBal>=	85873	) lot=	3.14	;
   if (AcBal>=	86146	) lot=	3.15	;
   if (AcBal>=	86420	) lot=	3.16	;
   if (AcBal>=	86693	) lot=	3.17	;
   if (AcBal>=	86967	) lot=	3.18	;
   if (AcBal>=	87241	) lot=	3.19	;
   if (AcBal>=	87514	) lot=	3.20	;
   if (AcBal>=	87788	) lot=	3.21	;
   if (AcBal>=	88062	) lot=	3.22	;
   if (AcBal>=	88335	) lot=	3.23	;
   if (AcBal>=	88609	) lot=	3.24	;
   if (AcBal>=	88882	) lot=	3.25	;
   if (AcBal>=	89156	) lot=	3.26	;
   if (AcBal>=	89430	) lot=	3.27	;
   if (AcBal>=	89703	) lot=	3.28	;
   if (AcBal>=	89977	) lot=	3.29	;
   if (AcBal>=	90251	) lot=	3.30	;
   if (AcBal>=	90524	) lot=	3.31	;
   if (AcBal>=	90798	) lot=	3.32	;
   if (AcBal>=	91071	) lot=	3.33	;
   if (AcBal>=	91345	) lot=	3.34	;
   if (AcBal>=	91619	) lot=	3.35	;
   if (AcBal>=	91892	) lot=	3.36	;
   if (AcBal>=	92166	) lot=	3.37	;
   if (AcBal>=	92440	) lot=	3.38	;
   if (AcBal>=	92713	) lot=	3.39	;
   if (AcBal>=	92987	) lot=	3.40	;
   if (AcBal>=	93260	) lot=	3.41	;
   if (AcBal>=	93534	) lot=	3.42	;
   if (AcBal>=	93808	) lot=	3.43	;
   if (AcBal>=	94081	) lot=	3.44	;
   if (AcBal>=	94355	) lot=	3.45	;
   if (AcBal>=	94629	) lot=	3.46	;
   if (AcBal>=	94902	) lot=	3.47	;
   if (AcBal>=	95176	) lot=	3.48	;
   if (AcBal>=	95449	) lot=	3.49	;
   if (AcBal>=	95723	) lot=	3.50	;
   if (AcBal>=	95997	) lot=	3.51	;
   if (AcBal>=	96270	) lot=	3.52	;
   if (AcBal>=	96544	) lot=	3.53	;
   if (AcBal>=	96818	) lot=	3.54	;
   if (AcBal>=	97091	) lot=	3.55	;
   if (AcBal>=	97365	) lot=	3.56	;
   if (AcBal>=	97638	) lot=	3.57	;
   if (AcBal>=	97912	) lot=	3.58	;
   if (AcBal>=	98186	) lot=	3.59	;
   if (AcBal>=	98459	) lot=	3.60	;
   if (AcBal>=	98733	) lot=	3.61	;
   if (AcBal>=	99007	) lot=	3.62	;
   if (AcBal>=	99280	) lot=	3.63	;
   if (AcBal>=	99554	) lot=	3.64	;
   if (AcBal>=	99827	) lot=	3.65	;
   if (AcBal>=	100101	) lot=	3.66	;
   if (AcBal>=	100375	) lot=	3.67	;
   if (AcBal>=	100648	) lot=	3.68	;
   if (AcBal>=	100922	) lot=	3.69	;
   if (AcBal>=	101196	) lot=	3.70	;
   if (AcBal>=	101469	) lot=	3.71	;
   if (AcBal>=	101743	) lot=	3.72	;
   if (AcBal>=	102016	) lot=	3.73	;
   if (AcBal>=	102290	) lot=	3.74	;
   if (AcBal>=	102564	) lot=	3.75	;
   if (AcBal>=	102837	) lot=	3.76	;
   if (AcBal>=	103111	) lot=	3.77	;
   if (AcBal>=	103385	) lot=	3.78	;
   if (AcBal>=	103658	) lot=	3.79	;
   if (AcBal>=	103932	) lot=	3.80	;
   if (AcBal>=	104205	) lot=	3.81	;
   if (AcBal>=	104479	) lot=	3.82	;
   if (AcBal>=	104753	) lot=	3.83	;
   if (AcBal>=	105026	) lot=	3.84	;
   if (AcBal>=	105300	) lot=	3.85	;
   if (AcBal>=	105574	) lot=	3.86	;
   if (AcBal>=	105847	) lot=	3.87	;
   if (AcBal>=	106121	) lot=	3.88	;
   if (AcBal>=	106395	) lot=	3.89	;
   if (AcBal>=	106668	) lot=	3.90	;
   if (AcBal>=	106942	) lot=	3.91	;
   if (AcBal>=	107215	) lot=	3.92	;
   if (AcBal>=	107489	) lot=	3.93	;
   if (AcBal>=	107763	) lot=	3.94	;
   if (AcBal>=	108036	) lot=	3.95	;
   if (AcBal>=	108310	) lot=	3.96	;
   if (AcBal>=	108584	) lot=	3.97	;
   if (AcBal>=	108857	) lot=	3.98	;
   if (AcBal>=	109131	) lot=	3.99	;
   if (AcBal>=	109404	) lot=	4.00	;
   if (AcBal>=	109678	) lot=	4.01	;
   if (AcBal>=	109952	) lot=	4.02	;
   if (AcBal>=	110225	) lot=	4.03	;
   if (AcBal>=	110499	) lot=	4.04	;
   if (AcBal>=	110773	) lot=	4.05	;
   if (AcBal>=	111046	) lot=	4.06	;
   if (AcBal>=	111320	) lot=	4.07	;
   if (AcBal>=	111593	) lot=	4.08	;
   if (AcBal>=	111867	) lot=	4.09	;
   if (AcBal>=	112141	) lot=	4.10	;
   if (AcBal>=	112414	) lot=	4.11	;
   if (AcBal>=	112688	) lot=	4.12	;
   if (AcBal>=	112962	) lot=	4.13	;
   if (AcBal>=	113235	) lot=	4.14	;
   if (AcBal>=	113509	) lot=	4.15	;
   if (AcBal>=	113782	) lot=	4.16	;
   if (AcBal>=	114056	) lot=	4.17	;
   if (AcBal>=	114330	) lot=	4.18	;
   if (AcBal>=	114603	) lot=	4.19	;
   if (AcBal>=	114877	) lot=	4.20	;
   if (AcBal>=	115151	) lot=	4.21	;
   if (AcBal>=	115424	) lot=	4.22	;
   if (AcBal>=	115698	) lot=	4.23	;
   if (AcBal>=	115971	) lot=	4.24	;
   if (AcBal>=	116245	) lot=	4.25	;
   if (AcBal>=	116519	) lot=	4.26	;
   if (AcBal>=	116792	) lot=	4.27	;
   if (AcBal>=	117066	) lot=	4.28	;
   if (AcBal>=	117340	) lot=	4.29	;
   if (AcBal>=	117613	) lot=	4.30	;
   if (AcBal>=	117887	) lot=	4.31	;
   if (AcBal>=	118160	) lot=	4.32	;
   if (AcBal>=	118434	) lot=	4.33	;
   if (AcBal>=	118708	) lot=	4.34	;
   if (AcBal>=	118981	) lot=	4.35	;
   if (AcBal>=	119255	) lot=	4.36	;
   if (AcBal>=	119529	) lot=	4.37	;
   if (AcBal>=	119802	) lot=	4.38	;
   if (AcBal>=	120076	) lot=	4.39	;
   if (AcBal>=	120349	) lot=	4.40	;
   if (AcBal>=	120623	) lot=	4.41	;
   if (AcBal>=	120897	) lot=	4.42	;
   if (AcBal>=	121170	) lot=	4.43	;
   if (AcBal>=	121444	) lot=	4.44	;
   if (AcBal>=	121718	) lot=	4.45	;
   if (AcBal>=	121991	) lot=	4.46	;
   if (AcBal>=	122265	) lot=	4.47	;
   if (AcBal>=	122538	) lot=	4.48	;
   if (AcBal>=	122812	) lot=	4.49	;
   if (AcBal>=	123086	) lot=	4.50	;
   if (AcBal>=	123359	) lot=	4.51	;
   if (AcBal>=	123633	) lot=	4.52	;
   if (AcBal>=	123907	) lot=	4.53	;
   if (AcBal>=	124180	) lot=	4.54	;
   if (AcBal>=	124454	) lot=	4.55	;
   if (AcBal>=	124727	) lot=	4.56	;
   if (AcBal>=	125001	) lot=	4.57	;
   if (AcBal>=	125275	) lot=	4.58	;
   if (AcBal>=	125548	) lot=	4.59	;
   if (AcBal>=	125822	) lot=	4.60	;
   if (AcBal>=	126096	) lot=	4.61	;
   if (AcBal>=	126369	) lot=	4.62	;
   if (AcBal>=	126643	) lot=	4.63	;
   if (AcBal>=	126916	) lot=	4.64	;
   if (AcBal>=	127190	) lot=	4.65	;
   if (AcBal>=	127464	) lot=	4.66	;
   if (AcBal>=	127737	) lot=	4.67	;
   if (AcBal>=	128011	) lot=	4.68	;
   if (AcBal>=	128285	) lot=	4.69	;
   if (AcBal>=	128558	) lot=	4.70	;
   if (AcBal>=	128832	) lot=	4.71	;
   if (AcBal>=	129105	) lot=	4.72	;
   if (AcBal>=	129379	) lot=	4.73	;
   if (AcBal>=	129653	) lot=	4.74	;
   if (AcBal>=	129926	) lot=	4.75	;
   if (AcBal>=	130200	) lot=	4.76	;
   if (AcBal>=	130474	) lot=	4.77	;
   if (AcBal>=	130747	) lot=	4.78	;
   if (AcBal>=	131021	) lot=	4.79	;
   if (AcBal>=	131295	) lot=	4.80	;
   if (AcBal>=	131568	) lot=	4.81	;
   if (AcBal>=	131842	) lot=	4.82	;
   if (AcBal>=	132115	) lot=	4.83	;
   if (AcBal>=	132389	) lot=	4.84	;
   if (AcBal>=	132663	) lot=	4.85	;
   if (AcBal>=	132936	) lot=	4.86	;
   if (AcBal>=	133210	) lot=	4.87	;
   if (AcBal>=	133484	) lot=	4.88	;
   if (AcBal>=	133757	) lot=	4.89	;
   if (AcBal>=	134031	) lot=	4.90	;
   if (AcBal>=	134304	) lot=	4.91	;
   if (AcBal>=	134578	) lot=	4.92	;
   if (AcBal>=	134852	) lot=	4.93	;
   if (AcBal>=	135125	) lot=	4.94	;
   if (AcBal>=	135399	) lot=	4.95	;
   if (AcBal>=	135673	) lot=	4.96	;
   if (AcBal>=	135946	) lot=	4.97	;
   if (AcBal>=	136220	) lot=	4.98	;
   if (AcBal>=	136493	) lot=	4.99	;
   if (AcBal>=	136767	) lot=	5.00	;
   if (AcBal>=	137041	) lot=	5.01	;
   if (AcBal>=	137314	) lot=	5.02	;
   if (AcBal>=	137588	) lot=	5.03	;
   if (AcBal>=	137862	) lot=	5.04	;
   if (AcBal>=	138135	) lot=	5.05	;
   if (AcBal>=	138409	) lot=	5.06	;
   if (AcBal>=	138682	) lot=	5.07	;
   if (AcBal>=	138956	) lot=	5.08	;
   if (AcBal>=	139230	) lot=	5.09	;
   if (AcBal>=	139503	) lot=	5.10	;
   if (AcBal>=	139777	) lot=	5.11	;
   if (AcBal>=	140051	) lot=	5.12	;
   if (AcBal>=	140324	) lot=	5.13	;
   if (AcBal>=	140598	) lot=	5.14	;
   if (AcBal>=	140871	) lot=	5.15	;
   if (AcBal>=	141145	) lot=	5.16	;
   if (AcBal>=	141419	) lot=	5.17	;
   if (AcBal>=	141692	) lot=	5.18	;
   if (AcBal>=	141966	) lot=	5.19	;
   if (AcBal>=	142240	) lot=	5.20	;
   if (AcBal>=	142513	) lot=	5.21	;
   if (AcBal>=	142787	) lot=	5.22	;
   if (AcBal>=	143060	) lot=	5.23	;
   if (AcBal>=	143334	) lot=	5.24	;
   if (AcBal>=	143608	) lot=	5.25	;
   if (AcBal>=	143881	) lot=	5.26	;
   if (AcBal>=	144155	) lot=	5.27	;
   if (AcBal>=	144429	) lot=	5.28	;
   if (AcBal>=	144702	) lot=	5.29	;
   if (AcBal>=	144976	) lot=	5.30	;
   if (AcBal>=	145249	) lot=	5.31	;
   if (AcBal>=	145523	) lot=	5.32	;
   if (AcBal>=	145797	) lot=	5.33	;
   if (AcBal>=	146070	) lot=	5.34	;
   if (AcBal>=	146344	) lot=	5.35	;
   if (AcBal>=	146618	) lot=	5.36	;
   if (AcBal>=	146891	) lot=	5.37	;
   if (AcBal>=	147165	) lot=	5.38	;
   if (AcBal>=	147438	) lot=	5.39	;
   if (AcBal>=	147712	) lot=	5.40	;
   if (AcBal>=	147986	) lot=	5.41	;
   if (AcBal>=	148259	) lot=	5.42	;
   if (AcBal>=	148533	) lot=	5.43	;
   if (AcBal>=	148807	) lot=	5.44	;
   if (AcBal>=	149080	) lot=	5.45	;
   if (AcBal>=	149354	) lot=	5.46	;
   if (AcBal>=	149627	) lot=	5.47	;
   if (AcBal>=	149901	) lot=	5.48	;
   if (AcBal>=	150175	) lot=	5.49	;
   if (AcBal>=	150448	) lot=	5.50	;
   if (AcBal>=	150722	) lot=	5.51	;
   if (AcBal>=	150996	) lot=	5.52	;
   if (AcBal>=	151269	) lot=	5.53	;
   if (AcBal>=	151543	) lot=	5.54	;
   if (AcBal>=	151816	) lot=	5.55	;
   if (AcBal>=	152090	) lot=	5.56	;
   if (AcBal>=	152364	) lot=	5.57	;
   if (AcBal>=	152637	) lot=	5.58	;
   if (AcBal>=	152911	) lot=	5.59	;
   if (AcBal>=	153185	) lot=	5.60	;
   if (AcBal>=	153458	) lot=	5.61	;
   if (AcBal>=	153732	) lot=	5.62	;
   if (AcBal>=	154005	) lot=	5.63	;
   if (AcBal>=	154279	) lot=	5.64	;
   if (AcBal>=	154553	) lot=	5.65	;
   if (AcBal>=	154826	) lot=	5.66	;
   if (AcBal>=	155100	) lot=	5.67	;
   if (AcBal>=	155374	) lot=	5.68	;
   if (AcBal>=	155647	) lot=	5.69	;
   if (AcBal>=	155921	) lot=	5.70	;
   if (AcBal>=	156195	) lot=	5.71	;
   if (AcBal>=	156468	) lot=	5.72	;
   if (AcBal>=	156742	) lot=	5.73	;
   if (AcBal>=	157015	) lot=	5.74	;
   if (AcBal>=	157289	) lot=	5.75	;
   if (AcBal>=	157563	) lot=	5.76	;
   if (AcBal>=	157836	) lot=	5.77	;
   if (AcBal>=	158110	) lot=	5.78	;
   if (AcBal>=	158384	) lot=	5.79	;
   if (AcBal>=	158657	) lot=	5.80	;
   if (AcBal>=	158931	) lot=	5.81	;
   if (AcBal>=	159204	) lot=	5.82	;
   if (AcBal>=	159478	) lot=	5.83	;
   if (AcBal>=	159752	) lot=	5.84	;
   if (AcBal>=	160025	) lot=	5.85	;
   if (AcBal>=	160299	) lot=	5.86	;
   if (AcBal>=	160573	) lot=	5.87	;
   if (AcBal>=	160846	) lot=	5.88	;
   if (AcBal>=	161120	) lot=	5.89	;
   if (AcBal>=	161393	) lot=	5.90	;
   if (AcBal>=	161667	) lot=	5.91	;
   if (AcBal>=	161941	) lot=	5.92	;
   if (AcBal>=	162214	) lot=	5.93	;
   if (AcBal>=	162488	) lot=	5.94	;
   if (AcBal>=	162762	) lot=	5.95	;
   if (AcBal>=	163035	) lot=	5.96	;
   if (AcBal>=	163309	) lot=	5.97	;
   if (AcBal>=	163582	) lot=	5.98	;
   if (AcBal>=	163856	) lot=	5.99	;
   if (AcBal>=	164130	) lot=	6.00	;
   if (AcBal>=	164403	) lot=	6.01	;
   if (AcBal>=	164677	) lot=	6.02	;
   if (AcBal>=	164951	) lot=	6.03	;
   if (AcBal>=	165224	) lot=	6.04	;
   if (AcBal>=	165498	) lot=	6.05	;
   if (AcBal>=	165771	) lot=	6.06	;
   if (AcBal>=	166045	) lot=	6.07	;
   if (AcBal>=	166319	) lot=	6.08	;
   if (AcBal>=	166592	) lot=	6.09	;
   if (AcBal>=	166866	) lot=	6.10	;
   if (AcBal>=	167140	) lot=	6.11	;
   if (AcBal>=	167413	) lot=	6.12	;
   if (AcBal>=	167687	) lot=	6.13	;
   if (AcBal>=	167960	) lot=	6.14	;
   if (AcBal>=	168234	) lot=	6.15	;
   if (AcBal>=	168508	) lot=	6.16	;
   if (AcBal>=	168781	) lot=	6.17	;
   if (AcBal>=	169055	) lot=	6.18	;
   if (AcBal>=	169329	) lot=	6.19	;
   if (AcBal>=	169602	) lot=	6.20	;
   if (AcBal>=	169876	) lot=	6.21	;
   if (AcBal>=	170149	) lot=	6.22	;
   if (AcBal>=	170423	) lot=	6.23	;
   if (AcBal>=	170697	) lot=	6.24	;
   if (AcBal>=	170970	) lot=	6.25	;
   if (AcBal>=	171244	) lot=	6.26	;
   if (AcBal>=	171518	) lot=	6.27	;
   if (AcBal>=	171791	) lot=	6.28	;
   if (AcBal>=	172065	) lot=	6.29	;
   if (AcBal>=	172338	) lot=	6.30	;
   if (AcBal>=	172612	) lot=	6.31	;
   if (AcBal>=	172886	) lot=	6.32	;
   if (AcBal>=	173159	) lot=	6.33	;
   if (AcBal>=	173433	) lot=	6.34	;
   if (AcBal>=	173707	) lot=	6.35	;
   if (AcBal>=	173980	) lot=	6.36	;
   if (AcBal>=	174254	) lot=	6.37	;
   if (AcBal>=	174527	) lot=	6.38	;
   if (AcBal>=	174801	) lot=	6.39	;
   if (AcBal>=	175075	) lot=	6.40	;
   if (AcBal>=	175348	) lot=	6.41	;
   if (AcBal>=	175622	) lot=	6.42	;
   if (AcBal>=	175896	) lot=	6.43	;
   if (AcBal>=	176169	) lot=	6.44	;
   if (AcBal>=	176443	) lot=	6.45	;
   if (AcBal>=	176716	) lot=	6.46	;
   if (AcBal>=	176990	) lot=	6.47	;
   if (AcBal>=	177264	) lot=	6.48	;
   if (AcBal>=	177537	) lot=	6.49	;
   if (AcBal>=	177811	) lot=	6.50	;
   if (AcBal>=	178085	) lot=	6.51	;
   if (AcBal>=	178358	) lot=	6.52	;
   if (AcBal>=	178632	) lot=	6.53	;
   if (AcBal>=	178905	) lot=	6.54	;
   if (AcBal>=	179179	) lot=	6.55	;
   if (AcBal>=	179453	) lot=	6.56	;
   if (AcBal>=	179726	) lot=	6.57	;
   if (AcBal>=	180000	) lot=	6.58	;
   if (AcBal>=	180274	) lot=	6.59	;
   if (AcBal>=	180547	) lot=	6.60	;
   if (AcBal>=	180821	) lot=	6.61	;
   if (AcBal>=	181095	) lot=	6.62	;
   if (AcBal>=	181368	) lot=	6.63	;
   if (AcBal>=	181642	) lot=	6.64	;
   if (AcBal>=	181915	) lot=	6.65	;
   if (AcBal>=	182189	) lot=	6.66	;
   if (AcBal>=	182463	) lot=	6.67	;
   if (AcBal>=	182736	) lot=	6.68	;
   if (AcBal>=	183010	) lot=	6.69	;
   if (AcBal>=	183284	) lot=	6.70	;
   if (AcBal>=	183557	) lot=	6.71	;
   if (AcBal>=	183831	) lot=	6.72	;
   if (AcBal>=	184104	) lot=	6.73	;
   if (AcBal>=	184378	) lot=	6.74	;
   if (AcBal>=	184652	) lot=	6.75	;
   if (AcBal>=	184925	) lot=	6.76	;
   if (AcBal>=	185199	) lot=	6.77	;
   if (AcBal>=	185473	) lot=	6.78	;
   if (AcBal>=	185746	) lot=	6.79	;
   if (AcBal>=	186020	) lot=	6.80	;
   if (AcBal>=	186293	) lot=	6.81	;
   if (AcBal>=	186567	) lot=	6.82	;
   if (AcBal>=	186841	) lot=	6.83	;
   if (AcBal>=	187114	) lot=	6.84	;
   if (AcBal>=	187388	) lot=	6.85	;
   if (AcBal>=	187662	) lot=	6.86	;
   if (AcBal>=	187935	) lot=	6.87	;
   if (AcBal>=	188209	) lot=	6.88	;
   if (AcBal>=	188482	) lot=	6.89	;
   if (AcBal>=	188756	) lot=	6.90	;
   if (AcBal>=	189030	) lot=	6.91	;
   if (AcBal>=	189303	) lot=	6.92	;
   if (AcBal>=	189577	) lot=	6.93	;
   if (AcBal>=	189851	) lot=	6.94	;
   if (AcBal>=	190124	) lot=	6.95	;
   if (AcBal>=	190398	) lot=	6.96	;
   if (AcBal>=	190671	) lot=	6.97	;
   if (AcBal>=	190945	) lot=	6.98	;
   if (AcBal>=	191219	) lot=	6.99	;
   if (AcBal>=	191492	) lot=	7.00	;
   if (AcBal>=	191766	) lot=	7.01	;
   if (AcBal>=	192040	) lot=	7.02	;
   if (AcBal>=	192313	) lot=	7.03	;
   if (AcBal>=	192587	) lot=	7.04	;
   if (AcBal>=	192860	) lot=	7.05	;
   if (AcBal>=	193134	) lot=	7.06	;
   if (AcBal>=	193408	) lot=	7.07	;
   if (AcBal>=	193681	) lot=	7.08	;
   if (AcBal>=	193955	) lot=	7.09	;
   if (AcBal>=	194229	) lot=	7.10	;
   if (AcBal>=	194502	) lot=	7.11	;
   if (AcBal>=	194776	) lot=	7.12	;
   if (AcBal>=	195049	) lot=	7.13	;
   if (AcBal>=	195323	) lot=	7.14	;
   if (AcBal>=	195597	) lot=	7.15	;
   if (AcBal>=	195870	) lot=	7.16	;
   if (AcBal>=	196144	) lot=	7.17	;
   if (AcBal>=	196418	) lot=	7.18	;
   if (AcBal>=	196691	) lot=	7.19	;
   if (AcBal>=	196965	) lot=	7.20	;
   if (AcBal>=	197238	) lot=	7.21	;
   if (AcBal>=	197512	) lot=	7.22	;
   if (AcBal>=	197786	) lot=	7.23	;
   if (AcBal>=	198059	) lot=	7.24	;
   if (AcBal>=	198333	) lot=	7.25	;
   if (AcBal>=	198607	) lot=	7.26	;
   if (AcBal>=	198880	) lot=	7.27	;
   if (AcBal>=	199154	) lot=	7.28	;
   if (AcBal>=	199427	) lot=	7.29	;
   if (AcBal>=	199701	) lot=	7.30	;
   if (AcBal>=	199975	) lot=	7.31	;
   if (AcBal>=	200248	) lot=	7.32	;
   if (AcBal>=	200522	) lot=	7.33	;
   if (AcBal>=	200796	) lot=	7.34	;
   if (AcBal>=	201069	) lot=	7.35	;
   if (AcBal>=	201343	) lot=	7.36	;
   if (AcBal>=	201616	) lot=	7.37	;
   if (AcBal>=	201890	) lot=	7.38	;
   if (AcBal>=	202164	) lot=	7.39	;
   if (AcBal>=	202437	) lot=	7.40	;
   if (AcBal>=	202711	) lot=	7.41	;
   if (AcBal>=	202985	) lot=	7.42	;
   if (AcBal>=	203258	) lot=	7.43	;
   if (AcBal>=	203532	) lot=	7.44	;
   if (AcBal>=	203805	) lot=	7.45	;
   if (AcBal>=	204079	) lot=	7.46	;
   if (AcBal>=	204353	) lot=	7.47	;
   if (AcBal>=	204626	) lot=	7.48	;
   if (AcBal>=	204900	) lot=	7.49	;
   if (AcBal>=	205174	) lot=	7.50	;
   if (AcBal>=	205447	) lot=	7.51	;
   if (AcBal>=	205721	) lot=	7.52	;
   if (AcBal>=	205995	) lot=	7.53	;
   if (AcBal>=	206268	) lot=	7.54	;
   if (AcBal>=	206542	) lot=	7.55	;
   if (AcBal>=	206815	) lot=	7.56	;
   if (AcBal>=	207089	) lot=	7.57	;
   if (AcBal>=	207363	) lot=	7.58	;
   if (AcBal>=	207636	) lot=	7.59	;
   if (AcBal>=	207910	) lot=	7.60	;
   if (AcBal>=	208184	) lot=	7.61	;
   if (AcBal>=	208457	) lot=	7.62	;
   if (AcBal>=	208731	) lot=	7.63	;
   if (AcBal>=	209004	) lot=	7.64	;
   if (AcBal>=	209278	) lot=	7.65	;
   if (AcBal>=	209552	) lot=	7.66	;
   if (AcBal>=	209825	) lot=	7.67	;
   if (AcBal>=	210099	) lot=	7.68	;
   if (AcBal>=	210373	) lot=	7.69	;
   if (AcBal>=	210646	) lot=	7.70	;
   if (AcBal>=	210920	) lot=	7.71	;
   if (AcBal>=	211193	) lot=	7.72	;
   if (AcBal>=	211467	) lot=	7.73	;
   if (AcBal>=	211741	) lot=	7.74	;
   if (AcBal>=	212014	) lot=	7.75	;
   if (AcBal>=	212288	) lot=	7.76	;
   if (AcBal>=	212562	) lot=	7.77	;
   if (AcBal>=	212835	) lot=	7.78	;
   if (AcBal>=	213109	) lot=	7.79	;
   if (AcBal>=	213382	) lot=	7.80	;
   if (AcBal>=	213656	) lot=	7.81	;
   if (AcBal>=	213930	) lot=	7.82	;
   if (AcBal>=	214203	) lot=	7.83	;
   if (AcBal>=	214477	) lot=	7.84	;
   if (AcBal>=	214751	) lot=	7.85	;
   if (AcBal>=	215024	) lot=	7.86	;
   if (AcBal>=	215298	) lot=	7.87	;
   if (AcBal>=	215571	) lot=	7.88	;
   if (AcBal>=	215845	) lot=	7.89	;
   if (AcBal>=	216119	) lot=	7.90	;
   if (AcBal>=	216392	) lot=	7.91	;
   if (AcBal>=	216666	) lot=	7.92	;
   if (AcBal>=	216940	) lot=	7.93	;
   if (AcBal>=	217213	) lot=	7.94	;
   if (AcBal>=	217487	) lot=	7.95	;
   if (AcBal>=	217760	) lot=	7.96	;
   if (AcBal>=	218034	) lot=	7.97	;
   if (AcBal>=	218308	) lot=	7.98	;
   if (AcBal>=	218581	) lot=	7.99	;
   if (AcBal>=	218855	) lot=	8.00	;
   if (AcBal>=	219129	) lot=	8.01	;
   if (AcBal>=	219402	) lot=	8.02	;
   if (AcBal>=	219676	) lot=	8.03	;
   if (AcBal>=	219949	) lot=	8.04	;
   if (AcBal>=	220223	) lot=	8.05	;
   if (AcBal>=	220497	) lot=	8.06	;
   if (AcBal>=	220770	) lot=	8.07	;
   if (AcBal>=	221044	) lot=	8.08	;
   if (AcBal>=	221318	) lot=	8.09	;
   if (AcBal>=	221591	) lot=	8.10	;
   if (AcBal>=	221865	) lot=	8.11	;
   if (AcBal>=	222138	) lot=	8.12	;
   if (AcBal>=	222412	) lot=	8.13	;
   if (AcBal>=	222686	) lot=	8.14	;
   if (AcBal>=	222959	) lot=	8.15	;
   if (AcBal>=	223233	) lot=	8.16	;
   if (AcBal>=	223507	) lot=	8.17	;
   if (AcBal>=	223780	) lot=	8.18	;
   if (AcBal>=	224054	) lot=	8.19	;
   if (AcBal>=	224327	) lot=	8.20	;
   if (AcBal>=	224601	) lot=	8.21	;
   if (AcBal>=	224875	) lot=	8.22	;
   if (AcBal>=	225148	) lot=	8.23	;
   if (AcBal>=	225422	) lot=	8.24	;
   if (AcBal>=	225696	) lot=	8.25	;
   if (AcBal>=	225969	) lot=	8.26	;
   if (AcBal>=	226243	) lot=	8.27	;
   if (AcBal>=	226516	) lot=	8.28	;
   if (AcBal>=	226790	) lot=	8.29	;
   if (AcBal>=	227064	) lot=	8.30	;
   if (AcBal>=	227337	) lot=	8.31	;
   if (AcBal>=	227611	) lot=	8.32	;
   if (AcBal>=	227885	) lot=	8.33	;
   if (AcBal>=	228158	) lot=	8.34	;
   if (AcBal>=	228432	) lot=	8.35	;
   if (AcBal>=	228705	) lot=	8.36	;
   if (AcBal>=	228979	) lot=	8.37	;
   if (AcBal>=	229253	) lot=	8.38	;
   if (AcBal>=	229526	) lot=	8.39	;
   if (AcBal>=	229800	) lot=	8.40	;
   if (AcBal>=	230074	) lot=	8.41	;
   if (AcBal>=	230347	) lot=	8.42	;
   if (AcBal>=	230621	) lot=	8.43	;
   if (AcBal>=	230895	) lot=	8.44	;
   if (AcBal>=	231168	) lot=	8.45	;
   if (AcBal>=	231442	) lot=	8.46	;
   if (AcBal>=	231715	) lot=	8.47	;
   if (AcBal>=	231989	) lot=	8.48	;
   if (AcBal>=	232263	) lot=	8.49	;
   if (AcBal>=	232536	) lot=	8.50	;
   if (AcBal>=	232810	) lot=	8.51	;
   if (AcBal>=	233084	) lot=	8.52	;
   if (AcBal>=	233357	) lot=	8.53	;
   if (AcBal>=	233631	) lot=	8.54	;
   if (AcBal>=	233904	) lot=	8.55	;
   if (AcBal>=	234178	) lot=	8.56	;
   if (AcBal>=	234452	) lot=	8.57	;
   if (AcBal>=	234725	) lot=	8.58	;
   if (AcBal>=	234999	) lot=	8.59	;
   if (AcBal>=	235273	) lot=	8.60	;
   if (AcBal>=	235546	) lot=	8.61	;
   if (AcBal>=	235820	) lot=	8.62	;
   if (AcBal>=	236093	) lot=	8.63	;
   if (AcBal>=	236367	) lot=	8.64	;
   if (AcBal>=	236641	) lot=	8.65	;
   if (AcBal>=	236914	) lot=	8.66	;
   if (AcBal>=	237188	) lot=	8.67	;
   if (AcBal>=	237462	) lot=	8.68	;
   if (AcBal>=	237735	) lot=	8.69	;
   if (AcBal>=	238009	) lot=	8.70	;
   if (AcBal>=	238282	) lot=	8.71	;
   if (AcBal>=	238556	) lot=	8.72	;
   if (AcBal>=	238830	) lot=	8.73	;
   if (AcBal>=	239103	) lot=	8.74	;
   if (AcBal>=	239377	) lot=	8.75	;
   if (AcBal>=	239651	) lot=	8.76	;
   if (AcBal>=	239924	) lot=	8.77	;
   if (AcBal>=	240198	) lot=	8.78	;
   if (AcBal>=	240471	) lot=	8.79	;
   if (AcBal>=	240745	) lot=	8.80	;
   if (AcBal>=	241019	) lot=	8.81	;
   if (AcBal>=	241292	) lot=	8.82	;
   if (AcBal>=	241566	) lot=	8.83	;
   if (AcBal>=	241840	) lot=	8.84	;
   if (AcBal>=	242113	) lot=	8.85	;
   if (AcBal>=	242387	) lot=	8.86	;
   if (AcBal>=	242660	) lot=	8.87	;
   if (AcBal>=	242934	) lot=	8.88	;
   if (AcBal>=	243208	) lot=	8.89	;
   if (AcBal>=	243481	) lot=	8.90	;
   if (AcBal>=	243755	) lot=	8.91	;
   if (AcBal>=	244029	) lot=	8.92	;
   if (AcBal>=	244302	) lot=	8.93	;
   if (AcBal>=	244576	) lot=	8.94	;
   if (AcBal>=	244849	) lot=	8.95	;
   if (AcBal>=	245123	) lot=	8.96	;
   if (AcBal>=	245397	) lot=	8.97	;
   if (AcBal>=	245670	) lot=	8.98	;
   if (AcBal>=	245944	) lot=	8.99	;
   if (AcBal>=	246218	) lot=	9.00	;
   if (AcBal>=	246491	) lot=	9.01	;
   if (AcBal>=	246765	) lot=	9.02	;
   if (AcBal>=	247038	) lot=	9.03	;
   if (AcBal>=	247312	) lot=	9.04	;
   if (AcBal>=	247586	) lot=	9.05	;
   if (AcBal>=	247859	) lot=	9.06	;
   if (AcBal>=	248133	) lot=	9.07	;
   if (AcBal>=	248407	) lot=	9.08	;
   if (AcBal>=	248680	) lot=	9.09	;
   if (AcBal>=	248954	) lot=	9.10	;
   if (AcBal>=	249227	) lot=	9.11	;
   if (AcBal>=	249501	) lot=	9.12	;
   if (AcBal>=	249775	) lot=	9.13	;
   if (AcBal>=	250048	) lot=	9.14	;
   if (AcBal>=	250322	) lot=	9.15	;
   if (AcBal>=	250596	) lot=	9.16	;
   if (AcBal>=	250869	) lot=	9.17	;
   if (AcBal>=	251143	) lot=	9.18	;
   if (AcBal>=	251416	) lot=	9.19	;
   if (AcBal>=	251690	) lot=	9.20	;
   if (AcBal>=	251964	) lot=	9.21	;
   if (AcBal>=	252237	) lot=	9.22	;
   if (AcBal>=	252511	) lot=	9.23	;
   if (AcBal>=	252785	) lot=	9.24	;
   if (AcBal>=	253058	) lot=	9.25	;
   if (AcBal>=	253332	) lot=	9.26	;
   if (AcBal>=	253605	) lot=	9.27	;
   if (AcBal>=	253879	) lot=	9.28	;
   if (AcBal>=	254153	) lot=	9.29	;
   if (AcBal>=	254426	) lot=	9.30	;
   if (AcBal>=	254700	) lot=	9.31	;
   if (AcBal>=	254974	) lot=	9.32	;
   if (AcBal>=	255247	) lot=	9.33	;
   if (AcBal>=	255521	) lot=	9.34	;
   if (AcBal>=	255795	) lot=	9.35	;
   if (AcBal>=	256068	) lot=	9.36	;
   if (AcBal>=	256342	) lot=	9.37	;
   if (AcBal>=	256615	) lot=	9.38	;
   if (AcBal>=	256889	) lot=	9.39	;
   if (AcBal>=	257163	) lot=	9.40	;
   if (AcBal>=	257436	) lot=	9.41	;
   if (AcBal>=	257710	) lot=	9.42	;
   if (AcBal>=	257984	) lot=	9.43	;
   if (AcBal>=	258257	) lot=	9.44	;
   if (AcBal>=	258531	) lot=	9.45	;
   if (AcBal>=	258804	) lot=	9.46	;
   if (AcBal>=	259078	) lot=	9.47	;
   if (AcBal>=	259352	) lot=	9.48	;
   if (AcBal>=	259625	) lot=	9.49	;
   if (AcBal>=	259899	) lot=	9.50	;
   if (AcBal>=	260173	) lot=	9.51	;
   if (AcBal>=	260446	) lot=	9.52	;
   if (AcBal>=	260720	) lot=	9.53	;
   if (AcBal>=	260993	) lot=	9.54	;
   if (AcBal>=	261267	) lot=	9.55	;
   if (AcBal>=	261541	) lot=	9.56	;
   if (AcBal>=	261814	) lot=	9.57	;
   if (AcBal>=	262088	) lot=	9.58	;
   if (AcBal>=	262362	) lot=	9.59	;
   if (AcBal>=	262635	) lot=	9.60	;
   if (AcBal>=	262909	) lot=	9.61	;
   if (AcBal>=	263182	) lot=	9.62	;
   if (AcBal>=	263456	) lot=	9.63	;
   if (AcBal>=	263730	) lot=	9.64	;
   if (AcBal>=	264003	) lot=	9.65	;
   if (AcBal>=	264277	) lot=	9.66	;
   if (AcBal>=	264551	) lot=	9.67	;
   if (AcBal>=	264824	) lot=	9.68	;
   if (AcBal>=	265098	) lot=	9.69	;
   if (AcBal>=	265371	) lot=	9.70	;
   if (AcBal>=	265645	) lot=	9.71	;
   if (AcBal>=	265919	) lot=	9.72	;
   if (AcBal>=	266192	) lot=	9.73	;
   if (AcBal>=	266466	) lot=	9.74	;
   if (AcBal>=	266740	) lot=	9.75	;
   if (AcBal>=	267013	) lot=	9.76	;
   if (AcBal>=	267287	) lot=	9.77	;
   if (AcBal>=	267560	) lot=	9.78	;
   if (AcBal>=	267834	) lot=	9.79	;
   if (AcBal>=	268108	) lot=	9.80	;
   if (AcBal>=	268381	) lot=	9.81	;
   if (AcBal>=	268655	) lot=	9.82	;
   if (AcBal>=	268929	) lot=	9.83	;
   if (AcBal>=	269202	) lot=	9.84	;
   if (AcBal>=	269476	) lot=	9.85	;
   if (AcBal>=	269749	) lot=	9.86	;
   if (AcBal>=	270023	) lot=	9.87	;
   if (AcBal>=	270297	) lot=	9.88	;
   if (AcBal>=	270570	) lot=	9.89	;
   if (AcBal>=	270844	) lot=	9.90	;
   if (AcBal>=	271118	) lot=	9.91	;
   if (AcBal>=	271391	) lot=	9.92	;
   if (AcBal>=	271665	) lot=	9.93	;
   if (AcBal>=	271938	) lot=	9.94	;
   if (AcBal>=	272212	) lot=	9.95	;
   if (AcBal>=	272486	) lot=	9.96	;
   if (AcBal>=	272759	) lot=	9.97	;
   if (AcBal>=	273033	) lot=	9.98	;
   if (AcBal>=	273307	) lot=	9.99	;
   if (AcBal>=	273580	) lot=	10.00	;
   return(lot);
}
