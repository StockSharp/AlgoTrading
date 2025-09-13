//+------------------------------------------------------------------+
//|                                             EA Hedge Average.mq4 |
//|                         Copyright 2016, MetaQuotes Software Corp |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, WidiPramana"
#property link      "https://www.mql5.com"
#property version   "1.00"

extern string  Name_EA        = "Hedge Average";
extern bool    Trade_buy      = true;
extern bool    Trade_sell     = true;
extern int     Start_Hour     = 6;
extern int     End_Hour       = 20;
extern bool    Tp_in_Money    = true;
extern double  TP_in_money    = 2;
extern int     TP             = 100;
extern int     SL             = 100;
extern int     Max_order      = 10;
extern double  Lots           = 0.1;
extern bool    TrailingStop_  = true;
extern int     TrailingStop   = 20;
extern int     Magic          = 76;
extern int     Period_1       = 4;
extern int     Period_2       = 4;

double slb,tpb,sls,tps,pt;
int ras,wt,wk,ticketb,tickets;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   if(Digits==3 || Digits==5) pt=10*Point;   else   pt=Point;
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {

   if(TrailingStop_)dtrailing();
   if(Tp_in_Money && TP_in_money<=money()) closeall();
//----
   double MA_1O =iMA(Symbol(),0,Period_1,0,MODE_SMA,PRICE_OPEN ,1);
   double MA_1C =iMA(Symbol(),0,Period_1,0,MODE_SMA,PRICE_CLOSE ,1);
   double MA_2O =iMA(Symbol(),0,Period_2,0,MODE_SMA,PRICE_OPEN ,2);
   double MA_2C =iMA(Symbol(),0,Period_2,0,MODE_SMA,PRICE_CLOSE ,2);

   int signal;
   if(MA_2O >MA_2C && MA_1O < MA_1C) signal=1;// open buy
   if(MA_2O <MA_2C && MA_1O > MA_1C) signal=2;// open sell
   if(Hour_trade()==1)
     {
      if(SL==0)slb=0;else slb=Ask-SL*pt;
      if(SL==0)sls=0;else sls=Bid+SL*pt;
      if(TP==0)tpb=0;else tpb=Ask+TP*pt;
      if(TP==0)tps=0;else tps=Bid-TP*pt;
      if(totalorder(0)<Max_order && Trade_buy && signal==1 && wt!=Time[0])
        {
         ticketb=OrderSend(Symbol(),OP_BUY,NR(Lots),Ask,3,slb,tpb,Name_EA,Magic,0,Blue);
         if(ticketb>0) wt=Time[0];
        }
      if(totalorder(1)<Max_order && Trade_sell && signal==2 && wk!=Time[0])
        {
         tickets=OrderSend(Symbol(),OP_SELL,NR(Lots),Bid,3,sls,tps,Name_EA,Magic,0,Red);
         if(tickets>0) wk=Time[0];
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int Hour_trade()
  {
   bool trade=false;
   if(Start_Hour>End_Hour)
     {
      if(Hour()>=Start_Hour || Hour()<End_Hour) trade=true;
     }
   else
      if(Hour()>=Start_Hour && Hour()<End_Hour) trade=true;

   return (trade);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int totalorder(int tipe)
  {
   int total=0;
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) continue;
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=Magic || OrderType()!=tipe) continue;
      total++;
     }

   return(total);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double NR(double thelot)
  {
   double maxlots=MarketInfo(Symbol(),MODE_MAXLOT),
   minilot=MarketInfo(Symbol(),MODE_MINLOT),
   lstep=MarketInfo(Symbol(),MODE_LOTSTEP);
   double lots=lstep*NormalizeDouble(thelot/lstep,0);
   lots=MathMax(MathMin(maxlots,lots),minilot);
   return (lots);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void dtrailing()
  {
   int i,r;double tpn;
   for(i=0; i<OrdersTotal(); i++)
     {
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) continue;
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=Magic) continue;
      tpn=OrderTakeProfit();
      if(OrderType()==OP_BUY)
        {
         if(Bid-OrderOpenPrice()>pt*TrailingStop)
           {
            if((OrderStopLoss()<Bid-pt*TrailingStop) || (OrderStopLoss()==0))
              {
               if(tpn) r=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-pt*TrailingStop,OrderTakeProfit(),0,Green);
              }
           }
        }
      if(OrderType()==OP_SELL)
        {
         if((OrderOpenPrice()-Ask)>(pt*TrailingStop))
           {
            if(OrderStopLoss()>(Ask+pt*TrailingStop) || (OrderStopLoss()==0))
              {
               if(tpn) r=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+pt*TrailingStop,OrderTakeProfit(),0,Red);
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeall()
  {
   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) continue;
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=Magic) continue;
      if(OrderType()>1) ras=OrderDelete(OrderTicket());
      else
        {
         if(OrderType()==0) ras=OrderClose(OrderTicket(),OrderLots(),Bid,3,CLR_NONE);
         else               ras=OrderClose(OrderTicket(),OrderLots(),Ask,3,CLR_NONE);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double money()
  {
   double dp=0;
   int i;
   for(i=0; i<OrdersTotal(); i++)
     {
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) continue;
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=Magic) continue;
      dp+=OrderProfit();
     }
   return(dp);
  }
//+------------------------------------------------------------------+
