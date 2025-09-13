#property copyright "Copyright 2023, Ctyptos_V1."
#property description "Crypto robot for ETH/USD pair. It uses large & tp/sl ratio and trailings for catching big profits. Usefull for conservative investments."
#property link      "https://t.me/CryptoRobotSecrets"
#property version   "1.00"
#property strict
double vbid,vask,bb,bbl,bbh,m,sprd;
string comment="cryptos.v1";
extern string sad1;// ----INPUT-------
extern int globaltrend=0; //1 FORSELL 2 FOR BUY 0 AUTO
extern string sad;// ----PROPERTIES-------
extern int candles=60; //search low/high candles count
extern int tpratio=30; //takeprofit ratio
extern bool autohl=true;
int orimagi=33232;
extern int risk=250; //risk $ per trade 250 for 10000$ depo or 25$ for 1000$ depo
extern string sad2;// ----EVENTS-------
extern bool pushBuy=false,pushSell=false;

extern bool skipSells=false,skipBuys=false;
double minVal=0.01; //min Value
double maxVal=100; //max Value
const int minRange=100; //min Range
const int loop=1000000,hoop=0; //constants for search hh/ll
double lll=loop,hhh=hoop;
bool trade=true; //trading always allow
int valueIndex=1; //crypto and forex constant
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(AccountBalance()<risk*3)
     {
      Print("It's reccomended no less than ",risk*3, " ballance! Or change Risk parameter to less risks");   //check ballance
      trade=false;
     }
   maxVal=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   minVal=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   //minVal=0.01;
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {



//check the pair
   if(Symbol()=="ETHUSD")
     {
      valueIndex=100;
      //minVal=0.1;
     }   //trade=true; else {Print("This EA is only for ETH/USD pair.");return;}
     else tpratio=3;

   if(trade)
     {
      //---
      vbid    = MarketInfo(Symbol(),MODE_BID); //bid
      vask    = MarketInfo(Symbol(),MODE_ASK); //ask
      sprd = MarketInfo(Symbol(),MODE_SPREAD); //spread

      int bandssize=200; //bands range
      bb = NormalizeDouble(iBands(NULL,0,bandssize,2,1,PRICE_CLOSE,MODE_MAIN,0),5);
      bbh = NormalizeDouble(iBands(NULL,0,bandssize,2,1,PRICE_CLOSE,MODE_UPPER,0),5);
      bbl = NormalizeDouble(iBands(NULL,0,bandssize,2,1,PRICE_CLOSE,MODE_LOWER,0),5);

      m=NormalizeDouble(iMA(NULL,0,55,1,MODE_LWMA,PRICE_MEDIAN,0),5); //wma 55

      if(bb==0 || m==0 )
         return; //do not trade if no indicators
      //trend search
      if(vbid>=bbh)
        {
         globaltrend=1;
         lll=loop;
         autohl=false;
        }
      if(vbid<=bbl)
        {
         globaltrend=2;
         hhh=hoop;
         autohl=false;
        }

      //sell signal
      if(vbid<m && globaltrend==1 && getSells()==0 && !skipSells)
        {
         pushSell();
         globaltrend=0;
         hhh=hoop;
        }
      //buy signal
      if(vbid>m && globaltrend==2 && getBuys()==0 && !skipBuys)
        {
         pushBuy();
         globaltrend=0;
         lll=loop;
        }
      //manual triggers
      if(pushBuy && getBuys()==0)
         pushBuy();
      if(pushSell && getSells()==0)
         pushSell();

      //trailing
      if(vbid<bbl && getBuys()>0)
         closeBuys();
      if(vbid>bbh && getSells()>0)
         closeSells();

      //auto high/low search
      if(vbid<m)
        {
         if(vbid<lll)
            lll=vbid;
        }
      if(vbid>m)
        {
         if(vbid>hhh)
            hhh=vbid;
        }
      //comments
      // Comment("TREND: ",globaltrend," HH: ",getHH()," LL: ",getLL()," BV: ",DoubleToStr(getBuyVal(),2)," SV: ",DoubleToStr(getSellVal(),2)," Candels: ",candles, " AH: ",autohl);
      Comment(AccountEquity());
     }

  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| get Highest High                                                                  |
//+------------------------------------------------------------------+
double getHH()
  {
   double hh=hoop;
   for(int i=0; i<=candles; i++)
     {
      if(High[i]>hh)
         hh=High[i];
     }
   if(autohl)
      return hh;
   else
      return hhh;

  }
//+------------------------------------------------------------------+
//| get Lowest Low                                                                  |
//+------------------------------------------------------------------+
double getLL()
  {
   double ll=loop;
   for(int i=0; i<=candles; i++)
     {
      if(Low[i]<ll)
         ll=Low[i];
     }
   if(autohl)
      return ll;
   else
      return lll;

  }


//+------------------------------------------------------------------+
//| prepare signal for Buy                                                                    |
//+------------------------------------------------------------------+
double getBuyRange()
  {
   double r=0;
   r=(m-getLL())/Point*tpratio;
   if(r<minRange)
      r=minRange;
   return r;


  }

//+------------------------------------------------------------------+
//| prepare signal for Sell                                                                  |
//+------------------------------------------------------------------+
double getSellRange()
  {
   double r=0;
   r=(getHH()-m)/Point*tpratio;
   if(r<minRange)
      r=minRange;
   return r;
  }



//+------------------------------------------------------------------+
//|  get current volume for Buy                                                                    |
//+------------------------------------------------------------------+
double getBuyVal()
  {

   double v=0;
   v=NormalizeDouble(risk/getBuyRange()*valueIndex,2);
   if(v<=minVal)
      v=minVal;

   if(v>=maxVal)
      v=maxVal;
   return v;
  }

//+------------------------------------------------------------------+
//| get current volume for Sell                                                                  |
//+------------------------------------------------------------------+
double getSellVal()
  {
   double v=0;
   v=NormalizeDouble(risk/getSellRange()*valueIndex,2);
   if(v<=minVal)
      v=minVal;
   if(v>=maxVal)
      v=maxVal;
   return v;
  }

//+------------------------------------------------------------------+
//| send new Buy                                                                      |
//+------------------------------------------------------------------+
void pushBuy()
  {
   double tp=vask+(getBuyRange()+sprd)*Point;
   
   openBuy(comment,getBuyVal(),getLL()-sprd*Point,tp);
  }
//+------------------------------------------------------------------+
//|  send new sell                                                                 |
//+------------------------------------------------------------------+
void pushSell()
  {
   double tp=vbid-(getSellRange()+sprd)*Point;
   openSell(comment,getSellVal(),getHH()+sprd*Point,tp);
  }


//+------------------------------------------------------------------+
//| send new raw Sell                                                                 |
//+------------------------------------------------------------------+
void openSell(string t, double v, double sl,double tp)
  {



   int ticket=OrderSend(Symbol(),OP_SELL,v,vbid,3,sl,tp,t,orimagi,0,clrRed);
   if(ticket>0)
     {

      if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
        {



         Print(t," Sell order Opened : ",OrderOpenPrice());
        }
     }
   else
      Print("Error opening Sell order : ",GetLastError());
   return;

  }

//+------------------------------------------------------------------+
//| send new raw Buy                                                                 |
//+------------------------------------------------------------------+
void openBuy(string t,double v, double sl,double tp)
  {



   int ticket=OrderSend(Symbol(),OP_BUY,v,vask,3,sl,tp,t,orimagi,0,clrGreen);
   if(ticket>0)
     {


      if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
        {

         Print(t," Buy order Opened : ",OrderOpenPrice());
        }
     }
   else
      Print("Error opening Buy order : ",GetLastError());
   return;

  }

//+------------------------------------------------------------------+
//|  get number of open Sell orders
//+------------------------------------------------------------------+
int getSells()
  {
   int r=0;
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      int s=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(s && OrderType()==OP_SELL  && OrderMagicNumber()==orimagi)
         r++;

     }
   return r;

  }


//+------------------------------------------------------------------+
//|  get number of open Buy orders
//+------------------------------------------------------------------+
int getBuys()
  {
   int r=0;
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      int s=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(s && OrderType()==OP_BUY && OrderMagicNumber()==orimagi)
         r++;

     }
   return r;


  }

//+------------------------------------------------------------------+
//| Close Sell Orders
//+------------------------------------------------------------------+
int closeSells()
  {
   int r=0;


   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      int s=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(s && OrderType()==OP_SELL  && OrderMagicNumber()==orimagi)
        {
         r=OrderClose(OrderTicket(),OrderLots(),vask,3,clrAqua);
        }


     }

   return r;

  }


//+------------------------------------------------------------------+
//| Close Buy Orders
//+------------------------------------------------------------------+
int closeBuys()
  {
   int r=0;
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      int s=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(s && OrderType()==OP_BUY  && OrderMagicNumber()==orimagi)
        {
         r=OrderClose(OrderTicket(),OrderLots(),vbid,3,clrAqua);
        }


     }


   return r;

  } 