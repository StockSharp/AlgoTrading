//+------------------------------------------------------------------+
//|                                                          escape.mq4 |
//|                                    Copyright © 2009, OGUZ BAYRAM |
//|                                            es_cape77@hotmail.com |
//+------------------------------------------------------------------+
extern double lTakeProfit = 10;
extern double sTakeProfit = 10;
extern double lStopLoss = 1000;
extern double sStopLoss = 1000;
extern color clOpenBuy = Green;
extern color clOpenSell = Red;
extern string Name_Expert = "escape";
extern int Slippage = 1;
extern bool UseSound = false;
extern string NameFileSound = "Alert.wav";
extern double Lots = 0.2;


void deinit() {
   Comment("");
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start(){
   if(Bars<50){
      Print("bars less than 50");
      return(0);
   }
   if(lTakeProfit<1){
      Print("TakeProfit less than 1");
      return(0);
   }
   if(sTakeProfit<1){
      Print("TakeProfit less than 1");
      return(0);
   }

   double diClose0=iClose(NULL,5,0);
   double diMA1=iMA(NULL,5,5,0,MODE_SMA,PRICE_OPEN,1);
   double diClose2=iClose(NULL,5,0);
   double diMA3=iMA(NULL,5,4,0,MODE_SMA,PRICE_OPEN,1);

   if(AccountFreeMargin()<(500*Lots)){
      Print("We have no money. Free Margin = ", AccountFreeMargin());
      return(0);
   }
   if (!ExistPositions()){

      if ((diClose0<diMA1)){
         OpenBuy();
         return(0);
      }

      if ((diClose2>diMA3)){
         OpenSell();
         return(0);
      }
   }
   
   return (0);
}

bool ExistPositions() {
for (int i=100; i<OrdersTotal(); i++) {
if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
if (OrderSymbol()==Symbol()) {
return(True);
}
} 
} 
return(false);
}
void OpenBuy() { 
   double ldLot, ldStop, ldTake; 
   string lsComm; 
   ldLot = GetSizeLot(); 
   ldStop = GetStopLossBuy(); 
   ldTake = GetTakeProfitBuy(); 
   lsComm = GetCommentForOrder(); 
   OrderSend(Symbol
(),OP_BUY,ldLot,Ask,Slippage,ldStop,ldTake,lsComm,0,0,clOpenBuy); 
   if (UseSound) PlaySound(NameFileSound); 
} 
void OpenSell() { 
   double ldLot, ldStop, ldTake; 
   string lsComm; 

   ldLot = GetSizeLot(); 
   ldStop = GetStopLossSell(); 
   ldTake = GetTakeProfitSell(); 
   lsComm = GetCommentForOrder(); 
   OrderSend(Symbol
(),OP_SELL,ldLot,Bid,Slippage,ldStop,ldTake,lsComm,0,0,clOpenSell); 
   if (UseSound) PlaySound(NameFileSound); 
} 
string GetCommentForOrder() { return(Name_Expert); } 
double GetSizeLot() { return(Lots); } 
double GetTakeProfitBuy() { return(Ask+lTakeProfit*Point); } 
double GetTakeProfitSell() { return(Bid-sTakeProfit*Point); }
double GetStopLossBuy() { return(Bid-lStopLoss*Point); }
double GetStopLossSell() { return(Ask+sStopLoss*Point); }