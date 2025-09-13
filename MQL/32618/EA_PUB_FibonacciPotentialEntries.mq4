//+------------------------------------------------------------------+
//|                             EA_PUB_FibonacciPotentialEntries.mq4 |
//|                                    Copyright 2020, Forex Jarvis. |
//|                                               info@fxweirdos.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, Forex Jarvis. info@fxweirdos.com"
#property link      "https://fxweirdos.com"
#property version   "1.00"
#property strict

input double dP50Level = 1.08261; // Price on 50% Level
input double dP61Level = 1.07811; // Price on 61% Level
input double dP100Level = 1.06370; // Price on 100% Level
input double dTarget2 = 1.10178;  // Target

input double dRisk; // RISK in %

double dVolTrade1;
double dVolTrade2;

bool buy1, buy2;
bool sell1, sell2;
bool close1, close2;
bool modify1, modify2;

bool bTrade1 = 0 ;
bool bTrade2 = 0;
bool bTrade1PartiallyClosed = 0;
bool bTrade2PartiallyClosed = 0;

int PositionFilled;
int LotDigits=0;
double dp;

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

   if (MarketInfo(sSymbol,MODE_DIGITS)==1 || MarketInfo(sSymbol,MODE_DIGITS)==3 || MarketInfo(sSymbol,MODE_DIGITS)==5)
      dp=10;
   else 
      dp=1;  
   double pipPos = MarketInfo(sSymbol,MODE_POINT)*dp;

   double dNbPips  = NormalizeDouble(MathAbs((dPrice-dSL)/pipPos),1); 
   double PipValue = MarketInfo(sSymbol,MODE_TICKVALUE)*pipPos/MarketInfo(sSymbol,MODE_TICKSIZE);

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
      if (NormalizeDouble(dLotSize(_Symbol, dP50Level, dP61Level-3*dSpread, 0.7),LotDigits)>0 && dP61Level-3*dSpread>0)
         if(OrderSend(_Symbol, OP_BUYLIMIT,NormalizeDouble(dLotSize(_Symbol, dP50Level, dP61Level-3*dSpread, 0.7),LotDigits), dP50Level, 5, dP61Level-3*dSpread, dTarget2, "FIB - The 50% Trade",0,0,0))
            buy1=true;
      if (NormalizeDouble(dLotSize(_Symbol, dP61Level, (dP61Level+dP100Level)/2-(3*dSpread), dRisk-0.7),LotDigits)>0 && (dP61Level+dP100Level)/2-(3*dSpread)>0)
         if(OrderSend(_Symbol, OP_BUYLIMIT,NormalizeDouble(dLotSize(_Symbol, dP61Level, (dP61Level+dP100Level)/2-(3*dSpread), dRisk-0.7),LotDigits), dP61Level, 5, (dP61Level+dP100Level)/2-(3*dSpread), dTarget2, "2FIB - The 61% Trade",0,0,0))
            buy2=true;
      bTrade1 = 1; 
      bTrade2 = 1;
   } else if (bType==2 && bTrade1==0 && bTrade2==0) {
      if (NormalizeDouble(dLotSize(_Symbol, dP50Level, dP61Level+3*dSpread, 0.7),LotDigits)>0 && dP61Level+3*dSpread>0)
         if(OrderSend(_Symbol, OP_SELLLIMIT,NormalizeDouble(dLotSize(_Symbol, dP50Level, dP61Level+3*dSpread, 0.7),LotDigits), dP50Level, 5, dP61Level+3*dSpread, dTarget2, "FIB - The 50% Trade",0,0,0))
            sell1=true;
      if (NormalizeDouble(dLotSize(_Symbol, dP61Level, (dP61Level+dP100Level)/2+(3*dSpread), dRisk-0.7),LotDigits)>0 && (dP61Level+dP100Level)/2+(3*dSpread)>0)
         if(OrderSend(_Symbol, OP_SELLLIMIT,NormalizeDouble(dLotSize(_Symbol, dP61Level, (dP61Level+dP100Level)/2+(3*dSpread), dRisk-0.7),LotDigits), dP61Level, 5, (dP61Level+dP100Level)/2+(3*dSpread), dTarget2, "FIB - The 61% Trade",0,0,0))
            sell2=true;
      bTrade1 = 1;
      bTrade2 = 1;
   }

   if (bTrade1==1 && bTrade2==1)
      if (dAsk>dTarget2) {
         
      	// TOTAL NUMBER OF OPEN POSITIONS
      	PositionFilled = OrdersTotal();
         
         if (PositionFilled>0 && bTrade1PartiallyClosed==0) {
         
            for (int i=0 ; i < PositionFilled ; i++) {
            
            // GET THE TICKET OF i OPEN POSITION
            int PositionTicket =OrderTicket();

   			if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) {
      
               double price   = OrderOpenPrice();
               double sl      = OrderStopLoss();
               double tp      = OrderTakeProfit();
               double vol     = OrderLots();
               string symbol  = OrderSymbol();

               if (symbol==_Symbol && price==dP50Level) {                 
                  if(OrderClose(PositionTicket,NormalizeDouble(dVolTrade1/2,LotDigits),0,0,0))
                     close1=true;
                  if(OrderModify(PositionTicket,0,dP50Level,dTarget2,0,0))
                     modify1=true;
                  bTrade1PartiallyClosed=1;
                  
               }
               if (symbol==_Symbol && price==dP61Level) {
                  if(OrderClose(PositionTicket,NormalizeDouble(dVolTrade1/2,LotDigits),0,0,0))
                     close2=true;
                  if(OrderModify(PositionTicket,0,dP61Level,dTarget2,0,0))
                     modify2=true;
                  bTrade2PartiallyClosed=1;
               }
            }
         }
      }
   }
}