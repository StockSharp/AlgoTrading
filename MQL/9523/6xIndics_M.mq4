//+------------------------------------------------------------------+
//|                             Indics_Shaker.mq4                    | 
//|                     Gift or donations accepted [:-)              |
//+------------------------------------------------------------------+

#property copyright "mich99@o2.pl"
#property link      ""




extern bool SELL = true; 
extern bool BUY = true;
extern bool Closeby = false;  

extern double   k = 1;
extern double   u = 2;
extern double   t = 3;
extern double   e = 4;
extern double   r = 3;
extern double   o = 4;

extern double   sh1 = 10; 
extern double   sh2 = 10;
extern double   sh3 = 10; 
extern double   zz = 1; 



extern int tp = 300;//for 4 digits = 30
extern int sl = 300;
extern bool         Trailing = false; 
extern int tsl = 300;

extern bool         ProfitTrailing = false; 
extern int LockPips = 300;
extern double lots = 0.1;
extern bool Martingale = false;

extern int MagicNumber = 911;
static int prevtime = 0;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   if(Time[0] == prevtime) 
       return(0);
   prevtime = Time[0];
   
//----
   if(IsTradeAllowed()) 
     {
       RefreshRates();
       
     } 
   else 
     {
       prevtime = Time[1];
       return(0);
     }
   int ticket = -1;

   int total = OrdersTotal();   
//----


   for(int i = 0; i < total; i++) 
     {
       OrderSelect(i, SELECT_BY_POS, MODE_TRADES); 
       
       if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber) 
         {
           int prevticket = OrderTicket();
           
           if(OrderType() == OP_BUY) 
             {
               
               if(Bid > OrderOpenPrice() )
                 {               
                   if(OPEN_POS()<0 && Closeby )  
                     { 
                       ticket = OrderClose(OrderTicket(), OrderLots(), Bid, 30, MediumSeaGreen);
                       
                       Sleep(10000);
                    
                       if(ticket < 0) 
                           prevtime = Time[1];
                         
                     } 
                   if(  Trailing && OrderStopLoss()<Bid-tsl * Point && (!ProfitTrailing || Bid > OrderOpenPrice()+(tsl+LockPips)*Point))
                     { // trailing stop
                       if(!OrderModify(OrderTicket(), OrderOpenPrice(), Bid - tsl * Point, 
                          OrderTakeProfit(), 0, Blue)) 
                         {
                           Sleep(30000);
                           prevtime = Time[1];
                         }
                     }
                 }  
               
             } 
           else 
             {
                
               if(Ask < OrderOpenPrice() ) 
                 {
                   if(OPEN_POS()>0 && Closeby )  
                     { 
                       ticket = OrderClose(OrderTicket(), OrderLots(), Ask, 30, MediumSeaGreen);
                       
                       Sleep(10000);
                       //----
                       if(ticket < 0) 
                           prevtime = Time[1];
                          
                     } 
                   if(Trailing && OrderStopLoss()>Ask +tsl*Point && (!ProfitTrailing || Ask < OrderOpenPrice()-((tsl+LockPips)*Point))) 
                     { 
                       if(!OrderModify(OrderTicket(), OrderOpenPrice(), Ask + tsl * Point, 
                          OrderTakeProfit(), 0, Blue)) 
                         {
                           Sleep(30000);
                           prevtime = Time[1];
                         }
                     }
                 }  
             }
          
           return(0);
         }
     }

   if(BUY  &&OPEN_POS()>0 ) 
     {
       ticket = OrderSend(Symbol(), OP_BUY, LotSize() , Ask, 30, Ask - sl * Point,Ask + tp * Point, WindowExpertName(), 
                          MagicNumber, 0, Blue); 
       
       if(ticket < 0) 
         {
           Sleep(30000);
           prevtime = Time[1];
         }
     } 
   if(SELL &&OPEN_POS()<0 ) 
     { 
       ticket = OrderSend(Symbol(), OP_SELL, LotSize() , Bid, 30, Bid + sl * Point, Bid - tp * Point, WindowExpertName(), 
                          MagicNumber, 0, Red); 
       if(ticket < 0) 
         {
           Sleep(30000);
           prevtime = Time[1];
         }
     }

   return(0);
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
double OPEN_POS()
{  
  
   
    
    double  klon1 = iAC(Symbol(),0,1);
    double  klon2 = iAC(Symbol(),0,10);
    double  klon3 = iAC(Symbol(),0,20);
    double  klon4 = iAO(Symbol(),0,0)-iAO(Symbol(),0,sh1);
    double  klon5 = iAC(Symbol(),0,0)-iAC(Symbol(),0,sh2);
    double  klon6 = iAC(Symbol(),0,0)-iAC(Symbol(),0,sh3);
    
    
    double  a =  0;
    double  b =  0;
    double  c =  0;
    double  d =  0;
    double  f =  0;
    double  g =  0;
    
    
    if(k==0)a=klon1;
    if(k==1)a=klon2;
    if(k==2)a=klon3;
    if(k==3)a=klon4;
    if(k==4)a=klon5;
    if(k==5)a=klon6;
    
    if(u==0)b=klon1;
    if(u==1)b=klon2;
    if(u==2)b=klon3;
    if(u==3)b=klon4;
    if(u==4)b=klon5;
    if(u==5)b=klon6;
    
    if(t==0)c=klon1;
    if(t==1)c=klon2;
    if(t==2)c=klon3;
    if(t==3)c=klon4;
    if(t==4)c=klon5;
    if(t==5)c=klon6;
    
    if(e==0)d=klon1;
    if(e==1)d=klon2;
    if(e==2)d=klon3;
    if(e==3)d=klon4;
    if(e==4)d=klon5;
    if(e==5)d=klon6;
    
    
    if(r==0)f=klon1;
    if(r==1)f=klon2;
    if(r==2)f=klon3;
    if(r==3)f=klon4;
    if(r==4)f=klon5;
    if(r==5)f=klon6;
    
    if(o==0)g=klon1;
    if(o==1)g=klon2;
    if(o==2)g=klon3;
    if(o==3)g=klon4;
    if(o==4)g=klon5;
    if(o==5)g=klon6;
    
    double  stoch70 =iStochastic(NULL, 0, 5, 5, 5, 0, 0, MODE_MAIN, 1);
   
   if( a > 0  && b >  0.0001*zz  && c  > 0.0002*zz && d  <  0   && f  <  0.0001*zz && g  <  0.0002*zz  && stoch70<15  ) { return(1);}  // a > xb/100  &&   c  >  yb/100
     
  
   if( a < 0  && b <  0.0001*zz  && c  < 0.0002*zz && d  >  0   && f  >  0.0001*zz && g  >  0.0002*zz  && stoch70>85 ) { return(-1);}  //b > xs/100  &&   d  >  ys/100  
   
   
   
   return(0);
}



  
  
  double LotSize() {

   if (IsOptimization()|| !Martingale ) {
      return(lots);
   }
  
  double losses = 0;
   double minlot = MarketInfo(Symbol(), MODE_MINLOT);
   int round = MathAbs(MathLog(minlot) / MathLog(10.0)) + 0.5;
   double result = lots;
   int total = OrdersHistoryTotal();
   double spread = MarketInfo(Symbol(), MODE_SPREAD);
   double k = (tp + sl) / (tp );
   for (int i = 0; i < total; i++) {
      OrderSelect(i, SELECT_BY_POS, MODE_HISTORY);
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber) {
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
   double maxlot = MarketInfo(Symbol(), MODE_MAXLOT);
   if (result > maxlot) {
      result = maxlot;
   }
   if (result < minlot) {
      MagicNumber = MagicNumber + 1;
   }
   RefreshRates();
   return(result);
}
 
  
  