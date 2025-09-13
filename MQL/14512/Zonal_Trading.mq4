//+------------------------------------------------------------------+
//|                                                    Zonal Trading |
//|                                                   Relaysim Comp. |
//|                                               relaysim@gmail.com |
//+------------------------------------------------------------------+
#property copyright    "RelaySim Comp."
#property link         "RelaySim@gmail.com"
#property version      "1.1"
#property description  "This expert use AO and AC indicators."
#property strict


extern double Lots=1; // Number of lots
extern bool AutoTrading=false; // Switch on automatic calculation of lots
extern double Risk=1; // Deposit's percent for trade
string com="Zonal Trading"; // Order's comment

extern int tp=5000; // Take profit value (pips)
double TP=tp*Point;

extern int MagNum=1214232141; // Magic Number
int Slip=10;

double spread,lots;

int ticket;
bool select,modify,close;

datetime t1,t2=0;

string Symb=Symbol();

int init() {return(0);}

int deinit() {return(0);}
//+------------------------------------------------------------------+
//| Order's control                                                  |
//+------------------------------------------------------------------+
bool order(string symb,int type,string c,int m)
  {
   int i;
   for(i=OrdersTotal()-1;i>=0;i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderComment()==c && OrderMagicNumber()==m && OrderSymbol()==symb && OrderType()==type) return(false);
         if(i==0) return(true);
        }
     }

   if(OrdersTotal()==0)return(true);
   else return(false);

  }

//+------------------------------------------------------------------+
//| Main function                                                    |
//+------------------------------------------------------------------+
int start()
  {
   int i;

   t1=Time[0];

   if(t1!=t2)
     {

      if(AutoTrading) lots=GetLots();
      else lots=Lots;

      bool buy  = iAC(NULL,0,1)>iAC(NULL,0,2) && iAO(NULL,0,1)>iAO(NULL,0,2) && (iAC(NULL,0,2)<iAC(NULL,0,3) || iAO(NULL,0,2)<iAO(NULL,0,3)) && iAO(NULL,0,1)>0 && iAC(NULL,0,1)>0; //// 
      bool sell = iAC(NULL,0,1)<iAC(NULL,0,2) && iAO(NULL,0,1)<iAO(NULL,0,2) && (iAC(NULL,0,2)>iAC(NULL,0,3) || iAO(NULL,0,2)>iAO(NULL,0,3)) && iAO(NULL,0,1)<0 && iAC(NULL,0,1)<0; //// 


      if(order(Symb,OP_BUY,com,MagNum) && order(Symb,OP_SELL,com,MagNum) && buy)
         ticket=OrderSend(Symbol(),OP_BUY,lots,Ask,Slip,0,Ask+TP,com,MagNum,0,Red);

      for(i=OrdersTotal()-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderComment()==com && OrderMagicNumber()==MagNum && OrderType()==OP_BUY)
              {
               if(iAC(NULL,0,1)<iAC(NULL,0,2) && iAO(NULL,0,1)<iAO(NULL,0,2))
                 {
                  close=OrderClose(OrderTicket(),OrderLots(),Bid,Slip,Blue);
                  break;
                 }
              }
           }
        }

      for(i=OrdersTotal()-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderComment()==com && OrderMagicNumber()==MagNum && OrderSymbol()==Symb && OrderType()==OP_BUY && OrderStopLoss()>iLow(NULL,0,iLowest(NULL,0,1,3,1)) && MathAbs(OrderStopLoss()-iLow(NULL,0,iLowest(NULL,0,1,3,1)))>spread)
              {
               modify=OrderModify(OrderTicket(),OrderOpenPrice(),iLow(NULL,0,iLowest(NULL,0,1,3,1)),OrderTakeProfit(),0,Red);
               break;
              }
           }
        }

      if(order(Symb,OP_BUY,com,MagNum) && order(Symb,OP_SELL,com,MagNum) && sell)
         ticket=OrderSend(Symbol(),OP_SELL,lots,Bid,Slip,0,Bid-TP,com,MagNum,0,Red);

      for(i=OrdersTotal()-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderComment()==com && OrderMagicNumber()==MagNum && OrderType()==OP_SELL)
              {
               if(iAC(NULL,0,1)>iAC(NULL,0,2) && iAO(NULL,0,1)>iAO(NULL,0,2))
                 {
                  close=OrderClose(OrderTicket(),OrderLots(),Ask,Slip,Blue);
                  break;
                 }
              }
           }
        }

      for(i=OrdersTotal()-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            if(OrderComment()==com && OrderMagicNumber()==MagNum && OrderSymbol()==Symb && OrderType()==OP_SELL && OrderStopLoss()<iHigh(NULL,0,iHighest(NULL,0,1,3,1)) && MathAbs(OrderStopLoss()-iHigh(NULL,0,iHighest(NULL,0,1,3,1)))>spread)
              {
               modify=OrderModify(OrderTicket(),OrderOpenPrice(),iHigh(NULL,0,iHighest(NULL,0,2,3,1))+spread,OrderTakeProfit(),0,Red);
               break;
              }
           }
        }

      t2=Time[0];

     }

   return(0);
  }
//+------------------------------------------------------------------+
//| Automatical lot's calculating.                                   |
//+------------------------------------------------------------------+
double GetLots()

  {
   double minlot = MarketInfo(Symbol(), MODE_MINLOT);
   double maxlot = MarketInfo(Symbol(), MODE_MAXLOT);
   double lotprice = MarketInfo(Symb,MODE_MARGINREQUIRED);
   double leverage = AccountLeverage();
   double lotsize = MarketInfo(Symbol(), MODE_LOTSIZE);
   double lotstep = MarketInfo(Symb,MODE_LOTSTEP);

   if(Risk>100) Risk=100;
   if(AutoTrading && Risk>0)
     {
      lots=MathFloor(AccountFreeMargin()*Risk/100/lotprice/lotstep)*lotstep;;
      if(lots < minlot) lots = minlot;
      if(lots > maxlot) lots = maxlot;
      if(AccountFreeMargin()<Ask*lots*lotsize/leverage) lots=minlot;
     }
   else lots=NormalizeDouble(Lots,2);

   return(lots);

  }
//+------------------------------------------------------------------+
