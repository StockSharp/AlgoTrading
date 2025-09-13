//+------------------------------------------------------------------+
//|   Base : E04L-cover-07.mq5                          E04LC07-v3   |
//+------------------------------------------------------------------+
#property version   "111.008"
#property description "D'Alembert betting system :"
#property description "The stake should be decreasing after the winning bet,"
#property description "... and increasing after the loosing one."

input string   WARNING="NOT all Input is check to be correct !";
input datetime LastRiskDate=D'2012.12.17 23:59:59'; // Do NOT take risk after this date ! (COMPETITION)
input double   MinMargin=200.1;                     // Don't increase risk below this MARGIN_LEVEL !
input double   KTP=0.0025;                          // TakeProfit as part of price ( 0.01=1.0% ) same to all Symbol
input double   Limit_Position_Lot=2.5;              // Maximal Position_volume per Symbol
input double   Limit_Exposure_Lot=1.25;             // Maximal Exposure per Value ( total from all symbols )
input double   LotAtRisk=1.75;                      // Maximal Exposure at send +/-(Valu1-Valu2) = Accepted Risk
input bool     Inverse = false;                     // Change position direction
input bool     ManageTP = true;                     // Quickly close profitable position
input bool     ManageLOSS=true;                     // Lower open price for loosing position
input int      NoWorkTime=9;                        // Time betwen two executions (seconds) = accelerate tests
input int      LotMX=3;                             // Multiply minimal order volume. Work only if 0.01
input int      KStart=7;                            // Multiply Start volume

enum SymbolPosition {PNon,PBuy,PSell}; // This is a enum TYPE declaration !
/*
// STANDART SETTING
const int Nmax = 10;      // Used Symbol (valu-pair) ; max=10
const int Vmax=5;         // index of last used Value +1 , first index=0 !
double V[5];              // Value exposure ; 0=EUR,1=USD,2=JPY,3=CHF,4=GBP,5=CAD ... no other for now.
string S[10] = {"EURUSD","USDJPY","EURJPY","GBPJPY","GBPCHF","EURCHF","GBPUSD","EURGBP","USDCHF","CHFJPY"};
SymbolPosition A[10] = {PSell,PSell,PBuy,PBuy,PSell,PSell,PBuy,PBuy,PBuy,PSell}; // What is action for each symbol
SymbolPosition SPos[10]; // What is position for each symbol
*/
/*
const int Vmax=7;          // index of last used Value +1 , first index=0 !
double V[7];               // Value exposure ; 0=EUR,1=USD,2=JPY,3=CHF,4=GBP,5=CAD,6=AUD.
// COMPETITION SETTING
 const int Nmax = 12;      // Used Symbol (valu-pair) ;
 string S[12] = {"USDCHF","GBPUSD","EURUSD","USDJPY","USDCAD","AUDUSD","EURGBP","EURAUD","EURCHF","EURJPY","GBPJPY","GBPCHF"};
 SymbolPosition A[12] = {PBuy,PSell,PBuy,PSell,PSell,PSell,PSell,PSell,PSell,PBuy,PBuy,PSell}; // What is action for each symbol
 SymbolPosition SPos[12]; // What is position for each symbol
*/
// V3-setting
const int Vmax=5;       // index of last used Value +1 , first index=0 !
double V[5];            // Value exposure ; 0=EUR,1=USD,2=JPY,3=CHF,4=GBP,5=CAD ... no other for now.
const int Nmax=3;       // Used Symbol (valu-pair) ;
string S[3]={"EURUSD","EURGBP","GBPUSD"};
SymbolPosition A[3]={PBuy,PSell,PSell}; // What is action for each symbol
SymbolPosition SPos[3];                 // What is position for each symbol . ==> MUST BE a HAMMILTON settings !
double LotV;              // Sum all lots : EXPOSURE
double LotP;              // Sum all lots : POSITION
string Val1,Val2;         // Current Symbol ( valu-pair == Val1/Val2 )
datetime LastMessageTime; // When Last Message was print
int I,J;                  // main program index
uchar SDig;               // Current symbol digits
string Out;               // Text on the screen
MqlTick T;                // Tick value
MqlTradeRequest R;        // Request - order
MqlTradeResult D;         // Deal = result of Request
double LotMin;            // Server setting
double NLots;             // Current symbol : Position volume
double Take;              // TakeProfit for open position (price).
double TP;                // TakeProfit for each order (delta_price)
double SStop;             // Market stop for current Symbol
datetime LastWorkTime;    // Used to accelerate tests , with "NoWorkTime"
double BestBallance;      // Used to protect from too much close with loss
bool   CloseOne;          // Used to stop manual close after firt one
double LotAtSend;         // varible : Same as "LotAtRisk"
string Reason;            // Why make Close_With_Loss
double FreeBalance;       // Available money for correction
double TPMove;

string MaxExpSym;         // Symbol with maximal position ( 1-"worst" position )
double MaxExpPos;         // Volume of maximal existing position
string MaxLosSym;         // Symbol with maximal loss ( 2-"worst" position )
double MaxLosPos;         // Profit(Loss) of most loosing position.
string MaxProSym;         // Symbol with maximal profit
double MaxProPos;         // Value of maximal profit
//+------------------------------------------------------------------+
//| PrintNM                                                          |
//+------------------------------------------------------------------+
void PrintNM(int NMTLimit,string NMS) // Print NMS with minimum interval NMTLimit seconds
  {
   if(LastMessageTime+NMTLimit<TimeCurrent())
     { LastMessageTime=TimeCurrent(); Print(NMS); }
  }
//+------------------------------------------------------------------+
//| ValueToIndex                                                     |
//+------------------------------------------------------------------+
int ValueToIndex(string SIndex) // in=Value_string; out=Index_of_Value;
  {
   if( SIndex=="EUR" ) return(0);
   if( SIndex=="USD" ) return(1);
   if( SIndex=="JPY" ) return(2);
   if( SIndex=="CHF" ) return(3);
   if( SIndex=="GBP" ) return(4);
   if( SIndex=="CAD" ) return(5);
   if( SIndex=="AUD" ) return(6);
   return(-1);
  }
//+------------------------------------------------------------------+
//| IndexToValue                                                     |
//+------------------------------------------------------------------+
string IndexToValue(int VIndex) // in=Index_of_Value; out=Value_string;
  {
   if( VIndex==0 ) return("EUR");
   if( VIndex==1 ) return("USD");
   if( VIndex==2 ) return("JPY");
   if( VIndex==3 ) return("CHF");
   if( VIndex==4 ) return("GBP");
   if( VIndex==5 ) return("CAD");
   if( VIndex==6 ) return("AUD");
   return("ANY");
  }
//+------------------------------------------------------------------+
//| Exposure                                                         |
//+------------------------------------------------------------------+
void Exposure() // result is : V[] , LotV , LotP, MaxExpSym - global
  {
   int EI,EJ; // index - local
   double ELotP; // Position_Lot for current symbol in base EUR - local
   for(EI=0;EI<Vmax;EI++) V[EI]=0.0; LotP=0.0;
   for(EI=0;EI<PositionsTotal();EI++)
     {
      Val1=StringSubstr(PositionGetSymbol(EI),0,3); Val2=StringSubstr(PositionGetSymbol(EI),3,3); // here position is selected !!!
      EJ=ValueToIndex(Val1);
      switch(PositionGetInteger(POSITION_TYPE))
        {
         case POSITION_TYPE_BUY :
           {
            if(Val1=="EUR") ELotP=PositionGetDouble(POSITION_VOLUME);
            else            ELotP=PositionGetDouble(POSITION_VOLUME)/SymbolInfoDouble("EUR"+Val1,SYMBOL_ASK);
            V[EJ]=V[EJ]+ELotP;
            LotP=LotP+ELotP;
            break;
           }
         case POSITION_TYPE_SELL :
           {
            if(Val1=="EUR") ELotP=PositionGetDouble(POSITION_VOLUME);
            else            ELotP=PositionGetDouble(POSITION_VOLUME)/SymbolInfoDouble("EUR"+Val1,SYMBOL_ASK);
            V[EJ]=V[EJ]-ELotP; LotP=LotP+ELotP; break;
           }
        } // end : swich Val1
      EJ=ValueToIndex(Val2);
      switch(PositionGetInteger(POSITION_TYPE))
        {
         case POSITION_TYPE_SELL :
           {
            if(Val1=="EUR") ELotP=PositionGetDouble(POSITION_VOLUME);
            else            ELotP=PositionGetDouble(POSITION_VOLUME)/SymbolInfoDouble("EUR"+Val1,SYMBOL_ASK);
            V[EJ]=V[EJ]+ELotP;
            LotP=LotP+ELotP;
            break;
           }
         case POSITION_TYPE_BUY :
           {
            if(Val1=="EUR") ELotP=PositionGetDouble(POSITION_VOLUME);
            else            ELotP=PositionGetDouble(POSITION_VOLUME)/SymbolInfoDouble("EUR"+Val1,SYMBOL_ASK);
            V[EJ]=V[EJ]-ELotP;
            LotP=LotP+ELotP;
           }
        } // end : swich Val2
     } // end : for all symbol
   LotP=LotP/2.0;
   LotV=0.0; for(EI=0;EI<Vmax;EI++) LotV=LotV+MathAbs(V[EI]);
  }
//+------------------------------------------------------------------+
//| Extreme                                                          |
//+------------------------------------------------------------------+
void Extreme()
  {
   int EI;
   MaxExpPos=0.0; MaxLosPos=0.0; MaxProPos=0.0;
   for(EI=0;EI<PositionsTotal();EI++)
     {
      PositionGetSymbol(EI);
      if(MaxExpPos<PositionGetDouble(POSITION_VOLUME)) // Maximal Position Volume and Symbol
        { MaxExpPos=PositionGetDouble(POSITION_VOLUME); MaxExpSym=PositionGetSymbol(EI); }
      if(MaxLosPos>PositionGetDouble(POSITION_PROFIT)) // Maximal LOSS position and Symbol
        { MaxLosPos=PositionGetDouble(POSITION_PROFIT); MaxLosSym=PositionGetSymbol(EI); }
      if(MaxProPos<PositionGetDouble(POSITION_PROFIT))
        { MaxProPos=PositionGetDouble(POSITION_PROFIT); MaxProSym=PositionGetSymbol(EI); }
     }

  }
//+------------------------------------------------------------------+
//| BUY                                                              |
//+------------------------------------------------------------------+
void BUY() //  // send order is BUY, Position can be ANY !!!
  {
   R.action=TRADE_ACTION_DEAL;
   R.symbol=S[I];
   R.price=T.ask;
   R.sl=0.0;
   R.deviation=99;
   R.type=ORDER_TYPE_BUY;
   R.type_filling=ORDER_FILLING_AON; // or ORDER_FILLING_RETURN
   switch(SPos[I])
     {
      case PNon  :
        {
         R.tp=NormalizeDouble(T.ask+KStart*TP,(uchar)SymbolInfoInteger(S[I],SYMBOL_DIGITS));
         R.volume=KStart*LotMin;
         R.comment="OPEN_BUY "+DoubleToString(V[ValueToIndex(Val1)],3)+" : "+
                   DoubleToString(V[ValueToIndex(Val2)],3);
         break;
        }                         // New position
      case PSell :
        {
         R.tp=PositionGetDouble(POSITION_TP);
         R.volume=LotMin;
         if(CloseOne) R.comment=Reason;                                                   // Losing close
         else R.comment="PART_CLOSE "+DoubleToString(PositionGetDouble(POSITION_PROFIT),2)+" / "+
                        DoubleToString(PositionGetDouble(POSITION_VOLUME),2);       // Part close
         break;
        }
      case PBuy  :
        {
         R.tp=PositionGetDouble(POSITION_TP);
         R.volume=LotMin;
         if(CloseOne) R.comment=Reason;
         else
         R.comment="ADD_BUY "+DoubleToString(V[ValueToIndex(Val1)],3)+" : "+
                   DoubleToString(V[ValueToIndex(Val2)],3);
         break;
        }                         // Increase
     } // end :switch
   Exposure();
   Out=" POSITIONS [EUR-lot] = "+DoubleToString(LotP,2)+"  FreeBalance="+DoubleToString(FreeBalance,2)+
       "\n EXPOSURE [EUR-lot] = "+DoubleToString(LotV,2)+" :";
   for(J=0;J<Vmax;J++) Out=Out+"  "+IndexToValue(J)+"="+DoubleToString(V[J],3);
   Print(Out);
   OrderSend(R,D);
   Print("1->","OrderSend_BUY_",S[I],"@_",DoubleToString(R.price,SDig)," RetCode=",D.retcode);
  }
//+------------------------------------------------------------------+
//| SELL                                                             |
//+------------------------------------------------------------------+
void SELL() //  // send order is SELL, Position can be ANY !!!
  {
   R.action=TRADE_ACTION_DEAL;
   R.symbol=S[I];
   R.price=T.bid;
   R.sl=0.0;
   R.deviation=99;
   R.type=ORDER_TYPE_SELL;
   R.type_filling=ORDER_FILLING_AON; // or ORDER_FILLING_RETURN
   switch(SPos[I])
     {
      case PNon  :
        {
         R.tp=NormalizeDouble(T.bid-KStart*TP,(uchar)SymbolInfoInteger(S[I],SYMBOL_DIGITS));
         R.volume=KStart*LotMin;
         R.comment="OPEN_SELL "+DoubleToString(V[ValueToIndex(Val1)],3)+" : "+
                   DoubleToString(V[ValueToIndex(Val2)],3);
         break;
        }                    // New position
      case PBuy  :
        {
         R.tp=PositionGetDouble(POSITION_TP);
         R.volume=LotMin;
         if(CloseOne) R.comment=Reason;                                                        // Losing close
         else R.comment="PART_CLOSE "+DoubleToString(PositionGetDouble(POSITION_PROFIT),2)+" / "+
                        DoubleToString(PositionGetDouble(POSITION_VOLUME),2);       // Part close
         break;
        }
      case PSell :
        {
         R.tp=PositionGetDouble(POSITION_TP);
         R.volume=LotMin;
         if(CloseOne) R.comment=Reason;
         else
         R.comment="ADD_SELL "+DoubleToString(V[ValueToIndex(Val1)],3)+" : "+
                   DoubleToString(V[ValueToIndex(Val2)],3);
         break;
        }                          // Increase
     } // end :switch
   Exposure();
   Out=" POSITIONS [EUR-lot] = "+DoubleToString(LotP,2)+"  FreeBalance="+DoubleToString(FreeBalance,2)+
       "\n EXPOSURE [EUR-lot] = "+DoubleToString(LotV,2)+" :";
   for(J=0;J<Vmax;J++) Out=Out+"  "+IndexToValue(J)+"="+DoubleToString(V[J],3);
   Print(Out);
   OrderSend(R,D);
   Print("2->","OrderSend_SELL_",S[I],"@_",DoubleToString(R.price,SDig)," RetCode=",D.retcode);
  }
//+------------------------------------------------------------------+
//| AcceptedLOSS                                                     |
//+------------------------------------------------------------------+
bool AcceptedLOSS() // Keep Balance go up, after loosing close
  {
   if(PositionGetDouble(POSITION_PROFIT)<0.0 && 
      FreeBalance+PositionGetDouble(POSITION_PROFIT)*LotMin/PositionGetDouble(POSITION_VOLUME)>0.0) return(true);
   else return(false);
  }
//+------------------------------------------------------------------+
//| ORDER_TP_BUY                                                     |
//+------------------------------------------------------------------+
void ORDER_TP_BUY() // Modify TP , position==BUY
  {
   R.action=TRADE_ACTION_SLTP;
   R.symbol=MaxProSym;
   R.tp=NormalizeDouble((1.0-KTP)*PositionGetDouble(POSITION_TP),
                        (uchar)SymbolInfoInteger(MaxProSym,SYMBOL_DIGITS));
   R.sl=0.0;
   R.deviation=99;
   if(R.tp-SymbolInfoDouble(MaxProSym,SYMBOL_ASK)>
      SymbolInfoInteger(MaxProSym,SYMBOL_TRADE_STOPS_LEVEL)*SymbolInfoDouble(MaxProSym,SYMBOL_POINT))
     {
      Out=DoubleToString(PositionGetDouble(POSITION_VOLUME),2)+"*"
          +DoubleToString(KTP*PositionGetDouble(POSITION_TP),(uchar)SymbolInfoInteger(MaxProSym,SYMBOL_DIGITS));
      R.comment="TP-BUY="+Out;
      OrderSend(R,D);
      Print("3->","OrderSend_MODIFY_TP_BUY_",MaxProSym," RetCode=",D.retcode);
     }
  }
//+------------------------------------------------------------------+
//| ORDER_TP_SELL                                                    |
//+------------------------------------------------------------------+
void ORDER_TP_SELL() // Modify TP , position==SELL
  {
   R.action=TRADE_ACTION_SLTP;
   R.symbol=MaxProSym;
   R.tp=NormalizeDouble((1.0+KTP)*PositionGetDouble(POSITION_TP),
                        (uchar)SymbolInfoInteger(MaxProSym,SYMBOL_DIGITS));
   R.sl=0.0;
   R.deviation=99;
   if(SymbolInfoDouble(MaxProSym,SYMBOL_BID)-R.tp>
      SymbolInfoInteger(MaxProSym,SYMBOL_TRADE_STOPS_LEVEL)*SymbolInfoDouble(MaxProSym,SYMBOL_POINT))
     {
      Out=DoubleToString(PositionGetDouble(POSITION_VOLUME),2)+"*"
          +DoubleToString(KTP*PositionGetDouble(POSITION_TP),(uchar)SymbolInfoInteger(MaxProSym,SYMBOL_DIGITS));
      R.comment="TP-SELL="+Out;
      OrderSend(R,D);
      Print("4->","OrderSend_MODIFY_TP_SELL_",MaxProSym," RetCode=",D.retcode);
     }
  }
//+------------------------------------------------------------------+
//| CleanBad                                                         |
//+------------------------------------------------------------------+
bool CleanBad() // Decreace Position_Volume for MaxLosSym with LotMin
  {
   bool CDone=false;
   for(I=0;I<Nmax;I++)
      if(S[I]==MaxLosSym) // Overloaded Symbol
        {
         PositionSelect(S[I]);
         SymbolInfoTick(S[I],T);
         SStop=SymbolInfoDouble(S[I],SYMBOL_POINT)*SymbolInfoInteger(S[I],SYMBOL_TRADE_STOPS_LEVEL);
         TP=MathMax(KTP*PositionGetDouble(POSITION_TP),2.0*SStop);
         switch(PositionGetInteger(POSITION_TYPE))
           {
            case POSITION_TYPE_BUY :
              {
               if(AcceptedLOSS() || CloseOne)
                 {
                  CloseOne=true; BUY(); SELL();
                  if(BestBallance<AccountInfoDouble(ACCOUNT_BALANCE)) BestBallance=AccountInfoDouble(ACCOUNT_BALANCE);
                  CDone=true;
                 }
               break;
              }
            case POSITION_TYPE_SELL :
              {
               if(AcceptedLOSS() || CloseOne)
                 {
                  CloseOne=true; SELL(); BUY();
                  if(BestBallance<AccountInfoDouble(ACCOUNT_BALANCE)) BestBallance=AccountInfoDouble(ACCOUNT_BALANCE);
                  CDone=true;
                 }
               break;
              }
           } // end : switch
         break; // exit : for
        }
   return(CDone); // Return true if made something
  }
//+------------------------------------------------------------------+
//| FreeTradeF                                                       |
//+------------------------------------------------------------------+
bool FreeTradeF()
  {
   return(AccountInfoDouble(ACCOUNT_MARGIN_LEVEL)>2.0*MathMax(MinMargin,400.0));
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   Print("Init");
   Exposure();
   LastMessageTime=TimeCurrent();
   LastWorkTime=TimeCurrent()-NoWorkTime;
   BestBallance=AccountInfoDouble(ACCOUNT_BALANCE)-AccountInfoDouble(ACCOUNT_PROFIT);
   CloseOne=(0.9*AccountInfoDouble(ACCOUNT_BALANCE)>AccountInfoDouble(ACCOUNT_EQUITY));
   LotMin=MathMax(SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN),0.01);
   if(LotMin<0.1) LotMin=NormalizeDouble(LotMin*LotMX,2);
   if(Inverse)
      for(I=0;I<Nmax;I++)
         switch(A[I])
           {
            case PBuy : { A[I]=PSell; break; }
            default : A[I]=PBuy;
           }
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(!TerminalInfoInteger(TERMINAL_TRADE_ALLOWED)) { PrintNM(15,"=== TRADE_NOT_ALLOWED ==="); return; }
   if(TimeCurrent()-LastWorkTime<NoWorkTime) return;                  // don't trade for NoWorkTime == save CPU
   else
     {
      LastWorkTime=TimeCurrent();

      if(LastRiskDate>TimeCurrent()) LotAtSend=LotAtRisk;
      else { LotAtSend=LotMin; PrintNM(420,"!!! ==> No-risk mode <== !!!"); }

      // =============================== Trade start here ==========================================
      Exposure();
      Extreme();
      FreeBalance=AccountInfoDouble(ACCOUNT_BALANCE)-BestBallance;

      if(ManageLOSS && CloseOne)
        { Reason="Start_DrawDown"; CleanBad(); CloseOne=false; return; }

      // ========== Accepted loss ! ==========
      PositionSelect(MaxLosSym);
      if(ManageLOSS && (PositionGetDouble(POSITION_PROFIT)<0.0 && PositionGetDouble(POSITION_VOLUME)>LotMin))
        {
         Reason="Adjust_Price="+DoubleToString(PositionGetDouble(POSITION_PRICE_OPEN),
                                               (uchar)SymbolInfoInteger(MaxLosSym,SYMBOL_DIGITS));
         if(CleanBad()) { CloseOne=false; return; }
        }
      // ========== UN-Expected profit ! =========
      if(ManageTP && (MaxProPos>(-MaxLosPos) || AccountInfoDouble(ACCOUNT_PROFIT)>0.0))
        {
         PositionSelect(MaxProSym);
         if(PositionGetDouble(POSITION_VOLUME)>LotMin)
            switch(PositionGetInteger(POSITION_TYPE))
              {
               case POSITION_TYPE_BUY  : { ORDER_TP_BUY(); break; }
               case POSITION_TYPE_SELL : { ORDER_TP_SELL(); }
              } // end : switch
        }

      for(I=0;I<Nmax;I++) // WARNING : Work with Symbol[I] , not Position[I] !
        { // Reading some data for current symbol
         Val1=StringSubstr(S[I],0,3); Val2=StringSubstr(S[I],3,3);
         SDig=(uchar)SymbolInfoInteger(S[I],SYMBOL_DIGITS);
         SStop=SymbolInfoDouble(S[I],SYMBOL_POINT)*SymbolInfoInteger(S[I],SYMBOL_TRADE_STOPS_LEVEL);
         SymbolInfoTick(S[I],T);

         // WARNINIG : Position must be selected here !!! 
         if(PositionSelect(S[I]))
           {
            NLots=PositionGetDouble(POSITION_VOLUME);
            Take=PositionGetDouble(POSITION_TP);
            TP=KTP*Take;
            if( PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY ) SPos[I]=PBuy;
            if( PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL ) SPos[I]=PSell;
           }
         else
           {
            NLots=0.0;
            Take=0.0;
            TP=KTP*T.bid;
            SPos[I]=PNon;
           }
         TP=MathMax(TP,2.0*SStop);                                          // My limit
         TP=MathMax(TP,(T.ask-T.bid)+SymbolInfoDouble(S[I],SYMBOL_POINT));  // RULE III.7.4 !!! ( TakeProfit>Spread )
         TP=NormalizeDouble(TP,SDig);
         // From here below ( and insite called function ) , SPos[I] == "POSITION_TYPE"

         bool FreeTrade=FreeTradeF();
         switch(SPos[I])
           {
            case PNon  :
              { // New position BUY
               if(V[ValueToIndex(Val1)]<Limit_Exposure_Lot || FreeTrade)
                  if((-V[ValueToIndex(Val2)])<Limit_Exposure_Lot || FreeTrade)
                    { if(A[I]==PBuy) { BUY(); return; } }
               else PrintNM(300,"! ==> Maximum Exposure SELL "+Val2);
               else PrintNM(300,"! ==> Maximum Exposure BUY "+Val1);
               // New position SELL
               if(V[ValueToIndex(Val2)]<Limit_Exposure_Lot || FreeTrade)
                  if((-V[ValueToIndex(Val1)])<Limit_Exposure_Lot || FreeTrade)
                    { if(A[I]==PSell) { SELL(); return; } }
               else PrintNM(300,"! ==> Maximum Exposure SELL "+Val1);
               else PrintNM(300,"! ==> Maximum Exposure BUY "+Val2);
               break;
              } // end : case PNon
            case PBuy  :
              { // Part close
               if(T.bid>Take-(NLots/LotMin-1.0)*TP) // Minimal PriceToSend
               if(NLots>LotMin) { SELL(); return; } // Don't close LotMin ==> use TakeProfit
               if(AccountInfoDouble(ACCOUNT_MARGIN_LEVEL)<MinMargin && V[ValueToIndex(Val1)]-V[ValueToIndex(Val2)]<LotMin)
                 { PrintNM(360,"!!! ==> LOW FREE MARGIN + High exposure "); break; }
               if(AccountInfoDouble(ACCOUNT_MARGIN_LEVEL)<123.4)
                 { PrintNM(360,"!!! ==> LOW FREE MARGIN "); return; }
               // Increase position
               if(T.ask<=Take-(NLots/LotMin+1.0)*TP) // Maximal PriceToSend
                  if(NLots+LotMin<Limit_Position_Lot)
                     if(V[ValueToIndex(Val1)]-V[ValueToIndex(Val2)]<=LotAtSend || FreeTrade)
                        if(V[ValueToIndex(Val1)]<Limit_Exposure_Lot || FreeTrade)
                           if((-V[ValueToIndex(Val2)])<Limit_Exposure_Lot || FreeTrade)
                             { BUY(); return; }                                      // Add BUY
               else PrintNM(480,"! ==> Maximum Exposure SELL "+Val2);
               else PrintNM(420,"! ==> Maximum Exposure BUY "+Val1);
               else PrintNM(360,"! ==> Maximum LOT_at_RISK : BUY_"+S[I]);
               else PrintNM(300,"! ==> Maximum POSITION_LOT "+S[I]);
               break;
              } // end : case PBuy
            case PSell :
              { // Part close
               if(T.ask<Take+(NLots/LotMin-1.0)*TP) // Maximal PriceToSend
               if(NLots>LotMin) { BUY(); return; } // Don't close LotMin ==> use TakeProfit
               if(AccountInfoDouble(ACCOUNT_MARGIN_LEVEL)<MinMargin && V[ValueToIndex(Val2)]-V[ValueToIndex(Val1)]<LotMin)
                 { PrintNM(360,"!!! ==> LOW FREE MARGIN + High exposure "+S[I]); break; }
               if(AccountInfoDouble(ACCOUNT_MARGIN_LEVEL)<123.4)
                 { PrintNM(360,"!!! ==> LOW FREE MARGIN "); return; }
               // Increase position
               if(T.bid>Take+(NLots/LotMin+1.0)*TP) // Minimal PriceToSend
                  if(NLots+LotMin<Limit_Position_Lot)
                     if(V[ValueToIndex(Val2)]-V[ValueToIndex(Val1)]<=LotAtSend || FreeTrade)
                        if(V[ValueToIndex(Val2)]<Limit_Exposure_Lot || FreeTrade)
                           if((-V[ValueToIndex(Val1)])<Limit_Exposure_Lot || FreeTrade)
                             { SELL(); return; }                                     // Add SELL
               else PrintNM(480,"! ==> Maximum Exposure SELL "+Val1);
               else PrintNM(420,"! ==> Maximum Exposure BUY "+Val2);
               else PrintNM(360,"! ==> Maximum LOT_at_RISK : SELL_"+S[I]);
               else PrintNM(300,"! ==> Maximum POSITION_LOT "+S[I]);
               break;
              } // end : case PSell
           } // end : switch
        } // end : for
     }
  }
//+------------------------------------------------------------------+
//| OnTrade                                                          |
//+------------------------------------------------------------------+
void OnTrade()
  {
   Sleep(5555);
  }
//+------------------------------------------------------------------+
