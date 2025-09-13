//+------------------------------------------------------------------+
//|                                _HPCS_IntFourth_MT4_EA_V01_WE.mq4 |
//|                        Copyright 2021, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
#property script_show_inputs
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

input int igi_stoploss = 10;
input int igi_takeprofit = 10;
input int igi_lots = 01;
input int  igi_magicNum = 2233;

int li_ticket=-1;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   int factor = 1;
   if(Digits == 5 || Digits ==3)
     { factor = 10; }

   double ld_takeprofit = Ask + igi_takeprofit*Point()*factor;
   double ld_stopLoss = Ask - igi_stoploss*Point()*factor;

   if(ld_stopLoss > (Bid - MarketInfo(_Symbol,MODE_STOPLEVEL)*Point()))
     {
      ld_stopLoss = Bid - MarketInfo(_Symbol,MODE_STOPLEVEL)*Point();
     }
   li_ticket = OrderSend(Symbol(),OP_BUY,igi_lots,Ask,10,ld_stopLoss,ld_takeprofit,NULL,igi_magicNum);

   if(li_ticket < 0)
     {
      Print("Order Send fail! ",GetLastError());
     }
   if(OrderSelect(li_ticket,SELECT_BY_TICKET))
     {
      OrderPrint();

      if(!OrderModify(OrderTicket(),OrderOpenPrice(),ld_stopLoss-(10*Point()*factor),ld_takeprofit,0))
        {
         Print("Order Not Modifiy Wth Error! ",GetLastError());
        }
      else
      {
         Print("Order Modified");
      }
     }
   //EventSetTimer(30);

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
EventKillTimer();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTimer()
  {
   /*if(OrderSelect(li_ticket,SELECT_BY_TICKET))
      if(OrderCloseTime() == 0)
         if(!OrderClose(li_ticket,igi_lots,OrderClosePrice(),10))
            Print("Order Not Closed Wth Error! ",GetLastError());
   */
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   if(OrderSelect(li_ticket,SELECT_BY_TICKET))
      if(TimeCurrent() >= (OrderOpenTime()+ 30))
         if(OrderCloseTime() == 0)
            if(!OrderClose(li_ticket,igi_lots,OrderClosePrice(),10))
               Print("Order Not Closed Wth Error! ",GetLastError());
            else
            {
               Print("Order Closed after 30 Seconds");
            }

  }
//+------------------------------------------------------------------+
