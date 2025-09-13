//+------------------------------------------------------------------+
//|                             EA_PUB_FibonacciPotentialEntries.mq5 |
//|                                    Copyright 2020, Forex Jarvis. |
//|                                               info@fxweirdos.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, Forex Jarvis. info@fxweirdos.com"
#property link      "https://fxweirdos.com"
#property version   "1.00"

#include <Trade\Trade.mqh>

CTrade trade;

input double dP50Level = 1.08261; // Price on 50% Level
input double dP61Level = 1.07811; // Price on 61% Level
input double dP100Level = 1.06370; // Price on 100% Level
input double dTarget2 = 1.10178;  // Target
input double dRisk = 2; // RISK in %

double dVolTrade1;
double dVolTrade2;

bool bTrade1 = 0 ;
bool bTrade2 = 0;
bool bTrade1PartiallyClosed = 0;
bool bTrade2PartiallyClosed = 0;

int PositionFilled;
int LotDigits=0;

// INPUT TYPE PRICE ACTION
enum boolTypeMarket{
   A1 = 1,  // Bull
   A2 = 2,  // Bear
};
input boolTypeMarket bType = A1; 

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   
   if(SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN) == 0.001) LotDigits = 3;
   if(SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN) == 0.01)  LotDigits = 2;
   if(SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN) == 0.1)   LotDigits = 1;

   return(INIT_SUCCEEDED);
  }

double dLotSize(string sSymbol, double dPrice, double dSL, double dRiskAmount) {

   double dp=0;
   if (SymbolInfoInteger(sSymbol,SYMBOL_DIGITS)==1 || SymbolInfoInteger(sSymbol,SYMBOL_DIGITS)==3 || SymbolInfoInteger(sSymbol,SYMBOL_DIGITS)==5)
      dp=10;
   else 
      dp=1;   
   double pipPos   = SymbolInfoDouble(sSymbol,SYMBOL_POINT)*dp;
   double dNbPips  = NormalizeDouble(MathAbs((dPrice-dSL)/pipPos),1); 
   double PipValue = SymbolInfoDouble(sSymbol,SYMBOL_TRADE_TICK_VALUE)*pipPos/SymbolInfoDouble(sSymbol,SYMBOL_TRADE_TICK_SIZE);

   double dAmountRisk = AccountInfoDouble(ACCOUNT_BALANCE)*dRiskAmount/100;

   return NormalizeDouble(dAmountRisk/(dNbPips*PipValue), 2);

}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {

   double dAsk = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits);
   double dBid = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);
   double dSpread = dAsk-dBid;

   dVolTrade1 = dLotSize(_Symbol, dP50Level, dP61Level-3*dSpread, 0.7);
   dVolTrade2 = dLotSize(_Symbol, dP61Level, (dP61Level+dP100Level)/2+(3*dSpread), dRisk-0.7);

   if (bType==1 && bTrade1==0 && bTrade2==0) {
      trade.BuyLimit(NormalizeDouble(dLotSize(_Symbol, dP50Level, dP61Level-3*dSpread, 0.7),LotDigits),dP50Level, _Symbol, dP61Level-3*dSpread, dTarget2,0,0,"FIB - The 50% Trade");
      bTrade1 = 1;
      trade.BuyLimit(NormalizeDouble(dLotSize(_Symbol, dP61Level, (dP61Level+dP100Level)/2-(3*dSpread), dRisk-0.7),LotDigits),dP61Level, _Symbol, (dP61Level+dP100Level)/2-(3*dSpread), dTarget2,0,0,"FIB - The 61% Trade");
      bTrade2 = 1;
   } else if (bType==2 && bTrade1==0 && bTrade2==0) {
      trade.SellLimit(NormalizeDouble(dLotSize(_Symbol, dP50Level, dP61Level+3*dSpread, 0.7),LotDigits),dP50Level, _Symbol, dP61Level+3*dSpread, dTarget2,0,0,"FIB - The 50% Trade");
      bTrade1 = 1;
      trade.SellLimit(NormalizeDouble(dLotSize(_Symbol, dP61Level, (dP61Level+dP100Level)/2+(3*dSpread), dRisk-0.7),LotDigits),dP61Level, _Symbol, (dP61Level+dP100Level)/2+(3*dSpread), dTarget2,0,0,"FIB - The 61% Trade");
      bTrade2 = 1;   
   }
   
   if (bTrade1==1 && bTrade2==1)
      if (dAsk>dTarget2) {
      
         // TOTAL NUMBER OF OPEN POSITIONS
         PositionFilled = PositionsTotal();
         
         if (PositionFilled>0 && bTrade1PartiallyClosed==0) {
         
            for (int i=0 ; i < PositionsTotal() ; i++) {
            
            // GET THE TICKET OF i OPEN POSITION
            ulong PositionTicket = PositionGetTicket(i);
   
      	   if (PositionSelectByTicket(PositionTicket)) {
      
               double price   = PositionGetDouble(POSITION_PRICE_OPEN);
               double sl      = PositionGetDouble(POSITION_SL);
               double tp      = PositionGetDouble(POSITION_TP);  
               double vol     = PositionGetDouble(POSITION_VOLUME);
               string symbol  = PositionGetString(POSITION_SYMBOL);      

               if (symbol==_Symbol && price==dP50Level) {
                  trade.PositionClosePartial(PositionTicket,NormalizeDouble(dVolTrade1/2,LotDigits));
                  trade.PositionModify(PositionTicket,dP50Level,dTarget2);
                  bTrade1PartiallyClosed=1;
               }
               if (symbol==_Symbol && price==dP61Level) {
                  trade.PositionClosePartial(PositionTicket,NormalizeDouble(dVolTrade2/2,LotDigits));
                  trade.PositionModify(PositionTicket,dP61Level,dTarget2);
                  bTrade2PartiallyClosed=1;
               }
            }
         }
      }
   }
}