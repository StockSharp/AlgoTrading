//+---------------------------------------------------------------------------------+
//|                                 ComFracti.mq4                                   |
//|                                                                                 |
//| If You make too much money with this EA - some gift or donations accepted [:-)  |
//+---------------------------------------------------------------------------------+
#property copyright " mich99@o2.pl "
#property link      " "

//---- input parameters 
extern int          mn = 818;
extern double       tp = 2000; // ( for 5 digits brokers )
extern double       sl = 1000;
extern double       TrailingStop=300;
extern double       lots = 0.1;
extern bool         MM = false;
extern double       Risk= 0.005;
extern double       multilot=0;
extern bool         closeby = false;
static int          prevtime = 0;


extern int          sh2 = 3; 
extern int          sh3 = 3;
extern int          sh4 = 3;
extern int          sh5 = 3;

extern int          per_rsi=3; // rsi1440 period 

extern double       sars1=0.02; // if sar1 = sar2,that sar filter is with out mining.
extern double       sars2=0.03;

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
      
      if(OrderType()<=OP_SELL && TrailingStop>0)
          {
             
              TrailingPositions();

          }
          
           if(OrderType() == OP_BUY && psv()  < 0 && closeby )
          {
             
              OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_BID),30,GreenYellow);

          }
          if(OrderType() == OP_SELL && psv()  > 0 && closeby)
          {
             
              OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_BID),30,GreenYellow);

          }
         return(0);
      } 
   }
   
 
   
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


 double Crof(int t , int s)
    
    
   {
     
   double frup = iFractals(NULL, t, MODE_UPPER, s);
   double frdw = iFractals(NULL, t, MODE_LOWER, s);

  
    
    if ( (frup==0 ) && frdw!=0 ) return (1); 
    if ( (frdw==0 ) && frup!=0 ) return (-1); 
   
    
     
     return (0); //elsewhere
   }    


 

 

      

double psv()   {

    double  s1 = iSAR(NULL, 0, sars1, 0.2, 0);
    double  s2 = iSAR(NULL, 0, sars2, 0.2, 0);
 
    double m6=iRSI(NULL, 1440, per_rsi, PRICE_OPEN, 0);

   if ( Crof(0 , sh2)>0  && Crof(60 , sh3)>0  && m6<50  && s1>=s2 )  return(1);
    
   if ( Crof(0 , sh4)<0  && Crof(60 , sh5)<0  && m6>50  && s1<=s2 ) return(-1);
      
      
   return(0);
  
}

//+--------------------------- getLots ----------------------------------+
   
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
   if(lot<0.01) lot=MarketInfo(Symbol(), MODE_MINLOT);
   if(!MM) lot=lots;
   return(lot);
  }
  
  
void TrailingPositions() {
  double pp;
  int TrailingStep = 1;
  bool ProfitTrailing = true;
  pp = Point;
  if (OrderType()==OP_BUY) 
  {
    
    if (!ProfitTrailing || (Bid-OrderOpenPrice())>TrailingStop*pp*2) 
    {
      if (OrderStopLoss()<Bid-(TrailingStop+TrailingStep-1)*pp) 
      {
        ModifyStopLoss(Bid-TrailingStop*pp);
        return;
      }
    }
  }
  if (OrderType()==OP_SELL) 
  {
  
    if (!ProfitTrailing || OrderOpenPrice()-Ask>TrailingStop*pp*2) 
    {
      if (OrderStopLoss()>Ask+(TrailingStop+TrailingStep-1)*pp || OrderStopLoss()==0) 
      {
        ModifyStopLoss(Ask+TrailingStop*pp);
        return;
      }
    }
  }
}
//--------------------------------------------------------------------------------------// 
void ModifyStopLoss(double ldStopLoss) {
  bool fm;

  fm=OrderModify(OrderTicket(),OrderOpenPrice(),ldStopLoss,OrderTakeProfit(),0,Yellow);
  
}
//-------------------------



void again() {
   prevtime = Time[1];
   Sleep(10000);
}