//+------------------------------------------------------------------+
//|                                               "Casino.mq4"   |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "mich99@o2.pl"



extern bool           BUY = true;
extern bool           SELL = true;
extern  double       Bet = 400;

extern int    x1 = 97;    
extern int    x2 = 77;





extern double       lots = 0.1;
extern bool MM = false;
extern  double maxlot = 4;
extern double Risk = 0.01; 


extern int          mn = 5431937;
extern int maxOrdersPerPair = 999;
static int          prevtime = 0;



//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
  HideTestIndicators(TRUE);
  
   if (Time[0] == prevtime) return(0);
   prevtime = Time[0];
   
   if (! IsTradeAllowed()) {
      again();
      return(0);
   }

   int total = OrdersTotal();
   for (int i = 0; i < total; i++) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
   if (OrderSymbol() == Symbol() && OrderMagicNumber() == mn) {
      
      
         return(0);
      } 
   }
   
   int ticket = -1;
   
   if (BUY && orderscntb()<maxOrdersPerPair &&  ACD() > 0 ) {//MACD > 0
      ticket = OrderSend(Symbol(), OP_BUY, LotSize(), Ask, 1, Ask - Bet * Point, Ask + Bet * Point, WindowExpertName(), mn, 0, Blue); 
      if (ticket < 0) {
         again();      
      }
   } 
   
   if (SELL && orderscnts()< maxOrdersPerPair &&  ACD() < 0 ) {//MACD < 0 
      ticket = OrderSend(Symbol(), OP_SELL, LotSize(), Bid, 1, Bid + Bet * Point, Bid - Bet * Point, WindowExpertName(), mn, 0, Red); 
      if (ticket < 0) {
         again();
      }
   }
   
   return(0);
}


//+------------------------------------------------------------------+
//| calculate perciptrons value                                      |
//+------------------------------------------------------------------+

///////////////////////////////////////////////////////////////////////
  

///////////////////////////////////////////////////////////////////////






 
double  ACD() {
   
   
   double Hd=iHigh(NULL,1440,1)+x1*Point;
   double Ld=iLow(NULL,1440,1)-x2*Point;
  
   
   if( Open[1]<Hd  && Open[0]>Hd   ) {
      return(-1);
   }
   if( Open[1]>Ld  && Open[0]<Ld   ) {
      return(1);
   }
   return(0);
}

//+------------------------------------------------------------------+

 


void again() {
   prevtime = Time[1];
   Sleep(30000);
}


 int orderscntb(){
  int cntb=0;
   for(int i =0;i<OrdersTotal();i++){
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)){
         if(OrderSymbol()==Symbol() && OrderType()==OP_BUY){
            cntb++;
         }
      }
   }
   return(cntb);
}
  
 int orderscnts(){
  int cnts=0;
   for(int i =0;i<OrdersTotal();i++){
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)){
         if(OrderSymbol()==Symbol() && OrderType()==OP_SELL){
            cnts++;
         }
      }
   }
   return(cnts);
} 


   
 
  
 double LotSize() {

   if (IsOptimization()|| !MM ) {
      return(lots);
   }
  
  double losses = 0;
   double minlot = MarketInfo(Symbol(), MODE_MINLOT);
   int round = MathAbs(MathLog(minlot) / MathLog(10.0)) + 0.5;
   double result = lots;
   int total = OrdersHistoryTotal();
   double spread = MarketInfo(Symbol(), MODE_SPREAD);
   double k = (Bet *2) / (Bet - spread);
   for (int i = 0; i < total; i++) {
      OrderSelect(i, SELECT_BY_POS, MODE_HISTORY);
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == mn) {
         if (OrderProfit() > 0) {
            result = lots;
            losses = 0;
         } else {
            result = result * k;
            losses++;
         }
      }
   }
   result = NormalizeDouble(result, round);
  
   if (result > maxlot) {
      result = maxlot;
   }
   if (result < minlot) {
      mn = mn + 1;
   }
   RefreshRates();
   return(result);
}
 