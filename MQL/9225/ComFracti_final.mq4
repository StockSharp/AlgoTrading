//+---------------------------------------------------------------------------------+
//|                                 ComFracti.mq4                                   |
//|                                                                                 |
//| If You make too much money with this EA - some gift or donations accepted [:-)  |
//+---------------------------------------------------------------------------------+
#property copyright " mich99@o2.pl "
#property link      " "

//---- input parameters 
extern bool         I_understand_how_it_works= false;
extern int          mn = 19390901;
extern double       tp = 400; //  for 4 digits brokers cut of one zero.
extern double       sl = 800;

extern bool         UseTrailingStop= false;
extern bool         ProfitTrailing = true;
extern double       TrailingStop=300;
extern double       lots = 0.1;
extern bool         MM = false;
extern bool         AccountMicro = false;
extern double       Risk= 0.05;
extern double       multilot=0;

extern string  
         Basic_Signal = "";
         
extern bool         useFractals = true;      
extern bool         useRsi = true;
extern double       rsiTF= 60;
extern bool         useStoch = false;

extern bool         CloseOnOppositeSignal = false;

extern string  
         o = " First allow optymisation for BUY, next for SELL";
extern bool         BUY = true;
extern int          sh1b = 3; 
extern int          sh2b = 3;
extern int          rsi1levelb = 3;
extern int          Stoch_perb = 5;
extern int          Stochlevelb = 20;

extern bool         SELL = true; 
extern int          sh1s = 3;
extern int          sh2s = 3;
extern int          rsi1levels = 3;
extern int          Stoch_pers = 5;
extern int          Stochlevels = 20;






extern string  
         Filters = "If You realy need it";
         
extern bool         MA_Filtr=false;
extern double       Ma_per=26;        
         
extern bool         PSAR_Filtr=false;
extern double       PsarStep=0.02; 


extern bool         Chanel_Filtr=false;
extern double         hbars=45;         
extern double         k=0.1;

extern bool         HLperceptron_Filtr=false;
extern int          v1 = 55; // 0  to  100
extern int          v2 = 55;
extern int          v3 = 55;
extern int          v4 = 55;


static int          prevtime = 0;

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
      
      if(OrderType()<=OP_SELL && UseTrailingStop)
          {
             
              TrailingPositions();

          }
          
           if(OrderType() == OP_BUY && psv()  < 0 && CloseOnOppositeSignal )
          {
             
              OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_BID),30,GreenYellow);

          }
          if(OrderType() == OP_SELL && psv()  > 0 && CloseOnOppositeSignal)
          {
             
              OrderClose(OrderTicket(),OrderLots(),MarketInfo(Symbol(),MODE_BID),30,GreenYellow);

          }
         return(0);
      } 
   }
   
 
   
   int ticket = -1;
   
   RefreshRates();
   
    if ( !I_understand_how_it_works ) Alert("Someone,who not understand the code turn on expert!"+Symbol());
 
   if ( BUY && psv()  > 0  && (!MA_Filtr  || Ma()>0) && (!PSAR_Filtr  ||Psar()>0) && (!Chanel_Filtr  || Chan()>0) && (!HLperceptron_Filtr  || xperc()>0)  ) {
      ticket = OrderSend(Symbol(), OP_BUY, LotSize(), Ask, 30, Ask - sl * Point, Ask + tp * Point, WindowExpertName()+"  BUY", mn, 0, Blue);
       if (ticket < 0) {
         again();      
      }
   } if (SELL && psv() < 0 && (!MA_Filtr  || Ma()<0) && (!PSAR_Filtr || Psar()<0) && (!Chanel_Filtr  || Chan()<0) && (!HLperceptron_Filtr  || xperc()<0) ) {
      ticket = OrderSend(Symbol(), OP_SELL, LotSize(), Bid, 30, Bid + sl * Point, Bid - tp * Point, WindowExpertName()+"  SELL", mn, 0, Red);
     
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
   
    
     
     return (0); 
   }    
  

double psv()   {


 
 double m7=iRSI(NULL, rsiTF, 3, PRICE_OPEN, 0);
 double m6b=iStochastic(NULL, 0, Stoch_perb, 3, 3, MODE_SMA, 1 , MODE_MAIN, 0);
 double m6s=iStochastic(NULL, 0, Stoch_pers, 3, 3, MODE_SMA, 1 , MODE_MAIN, 0);
 
 
   if ((!useFractals  ||( Crof(0 , sh1b)>0  && Crof(60 , sh2b)>0))  && (!useRsi  || m7<50-rsi1levelb ) && (!useStoch  || m6b<50-Stochlevelb) ) return(1); 
    
   if ((!useFractals  ||( Crof(0 , sh1s)<0  && Crof(60 , sh2s)<0)) && (!useRsi  || m7>50+rsi1levels ) && (!useStoch  || m6s>50+Stochlevels) ) return(-1);  
       
      
   return(0);
  
}



 double Chan()
    
    
   {
     
    double max=iHigh(Symbol(),0,iHighest(Symbol(),0,MODE_HIGH,hbars,1));
    double min=iLow(Symbol(),0,iLowest(Symbol(),0,MODE_LOW,hbars,1));
    double pivot=(iClose(Symbol(),0,1+1)+iClose(Symbol(),0,1+2)+iClose(Symbol(),0,1+3))/3;
    double A=(Close[1]-((max + min + pivot)/3));
    double b=(((max + min + pivot)/3));
    double f=(((max - min )));

     double g=max-k*f;
     double s=min+k*f;
 
    if (  Low[1] > s ) return (1); 
    if ( High[1] < g ) return (-1); 
   
     return (0); //elsewhere
   }   
   
    

 double Ma()
    
    
   {
     
    double ma6=iMA(NULL,60,Ma_per,0,MODE_EMA,PRICE_CLOSE,1)-iMA(NULL,60,Ma_per,0,MODE_EMA,PRICE_CLOSE,2);
    
    if (  ma6 > 0 ) return (1); 
    if (  ma6 < 0 ) return (-1); 
   
     return (0); //elsewhere
   }    

double Psar()
    
    
   {
     
     double  s1 = iSAR(NULL, 60, PsarStep, 0.2, 0);
     
    if (  s1 < Open[0] ) return (1); 
    if (  s1 > Open[0]) return (-1); 
   
     return (0); //elsewhere
   }    


     double xperc()
  {
   double w1 = v1 - 50;
   double w2 = v2 - 50;
   double w3 = v3 - 50;
   double w4 = v4 - 50;
 
  
   double a1 = High[1] - High[7];
   double a2 = High[4] - High[11];
   double a3 = Low[1] - Low[7];
   double a4 = Low[4] - Low[11];
    return(w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4 );
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
   if(!MM) lot=lots;
   if(lot<0.1) lot=0.1;
   if(AccountMicro==true) lot=lot/10;
   return(lot);
  }
  
  
void TrailingPositions() {
  double pp;
  int TrailingStep = 1;
  
  pp = MarketInfo(OrderSymbol(), MODE_POINT);
  if (OrderType()==OP_BUY) 
  {
    
    if (!ProfitTrailing || (Bid-OrderOpenPrice())>TrailingStop*pp) 
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
  
    if (!ProfitTrailing || OrderOpenPrice()-Ask>TrailingStop*pp) 
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