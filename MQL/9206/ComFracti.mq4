//+------------------------------------------------------------------+
//|                                                  Grizli.mq4      |
//+------------------------------------------------------------------+
#property copyright ""
#property link      ""

//---- input parameters
extern double       tp1 = 700;
extern double       sl1 = 2500;
extern int          Expimin = 5555;
extern double       lots = 0.1;
extern double       Risk= 0.05;
extern double       multilot=1;
extern bool         closeby = false;
extern int          mn = 88;
static int          prevtime = 0;


extern int          sh2 = 3;
extern int          sh3 = 3;
extern int          sh4 = 3;
extern int          sh5 = 3;
extern int          levelb = 3;
extern int          levels = 3;

static double       sl = 10;
static double       tp = 10;

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   if (Time[0] == prevtime) return(0);
   prevtime = Time[0];
   
   if (! IsTradeAllowed()) {
      again();
      return(0);
   }
//----
   int total = OrdersTotal();
   for (int i = 0; i < total; i++) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == mn) {
      
      if(TimeCurrent()-OrderOpenTime()>=Expimin*60)
          {
             
              OrderClose(OrderTicket(),LotSize(),MarketInfo(Symbol(),MODE_BID),30,GreenYellow);

          }
          
           if(OrderType() == OP_BUY && psv()  < 0 && closeby )
          {
             
              OrderClose(OrderTicket(),LotSize(),MarketInfo(Symbol(),MODE_BID),30,GreenYellow);

          }
          if(OrderType() == OP_SELL && psv()  > 0 && closeby)
          {
             
              OrderClose(OrderTicket(),LotSize(),MarketInfo(Symbol(),MODE_BID),30,GreenYellow);

          }
         return(0);
      } 
   }
   
   sl = sl1;
   tp = tp1;
   
   int ticket = -1;
   
   RefreshRates();
   
   if (psv()  > 0) {
      ticket = OrderSend(Symbol(), OP_BUY, LotSize(), Ask, 30, Bid - sl * Point, Bid + tp * Point, WindowExpertName(), mn, 0, Blue); 
      if (ticket < 0) {
         again();      
      }
   } if (psv()  < 0) {
      ticket = OrderSend(Symbol(), OP_SELL, LotSize(), Bid, 30, Ask + sl * Point, Ask - tp * Point, WindowExpertName(), mn, 0, Red); 
      if (ticket < 0) {
         again();
      }
   }
//-- Exit --
   return(0);
}
//+--------------------------- getLots ----------------------------------+
      
 double LotSize2(){
   double lotMM = MathCeil(AccountFreeMargin() * Risk / 1000)/10;  
   if (lotMM < 0.1) lotMM = lots;
   if (lotMM > 1.0) lotMM = MathCeil(lotMM);
   if  (lotMM > 50) lotMM = 50;
   return (lotMM);  }
   
  double LotSize()
  {
  double DecreaseFactor=multilot;
  
   double lot=lots;
   int    orders=HistoryTotal();     // history orders total
   int    losses=0;                  // number of losses orders without a break
//---- select lot size
   lot=MathCeil(AccountFreeMargin() * Risk / 1000)/10;
//---- calcuulate number of losses orders without a break
   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>0) lot=NormalizeDouble(lot-(-1)*lot*DecreaseFactor,1);
     }
//---- return lot size
   if(lot<0.1) lot=0.1;
   return(lot);
  }

 double Crof(int t , int s)
    
    
   {
     
   double frup = iFractals(NULL, t, MODE_UPPER, s);
   double frdw = iFractals(NULL, t, MODE_LOWER, s);
  // double frup2 = iFractals(NULL, t, MODE_UPPER, s2);
   //double frdw2 = iFractals(NULL, t, MODE_LOWER, s2);
  
    
    if ( (frup==0 ) && frdw!=0 ) return (1); //up trend|| High[1] > frup
    if ( (frdw==0 ) && frup!=0 ) return (-1); //down trend|| Low[1]  < frdw
   
    
     
     return (0); //elsewhere
   }    


 

 

      

double psv()   {

 
 double m6=iRSI(NULL, 1440, 3, PRICE_OPEN, 0);

   if ( Crof(0 , sh2)>0  && Crof(60 , sh3)>0  && m6<50-levelb ) return(1);
    
   if ( Crof(0 , sh4)<0  && Crof(60 , sh5)<0  && m6>50+levels ) return(-1);
      
      
   return(0);
  
}





void again() {
   prevtime = Time[1];
   Sleep(30000);
}