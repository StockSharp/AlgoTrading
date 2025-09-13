//+------------------------------------------------------------------+
//|                                               ProfitTrailing.mq4 |
//|                                                            Moggy |
//|                                             moggylew@hotmail.com |
//+------------------------------------------------------------------+
#property copyright "Moggy"
#property link      "moggylew@hotmail.com"
#property version   "1.00"
#property strict

#define MAXORDERSCNT              100
#define TICKET_INVALID            -860228
#define ORDER_TYPE_INVALID        -1
#define PROFIT_SL_INVALID         -140523
#define PROFIT_TP_INVALID         140523


extern double minlots                     = 0.01;
extern double minlotsprofittrailingsl     = 2.20;
extern double minlotsprofittp             = 5.00;
extern int slippage                       = 1;


int arrOrderTicket[MAXORDERSCNT];//record all tickets
double arrOrderOpenPrice[MAXORDERSCNT];//record all tickets open price
int arrOrderType[MAXORDERSCNT];//record all tickets order type
double arrOrderProfit[MAXORDERSCNT];//record all order profits
double arrOrderProfitSL[MAXORDERSCNT];//record all order profits stoploss
double arrOrderProfitTP[MAXORDERSCNT];//record all order profits takeprofit

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   InitOrderArr();
//--- create timer
   if(!EventSetTimer(2))
   {
       printf("OnInit EventSetTimer error:" + (string)GetLastError());
       return INIT_FAILED;
   }
      
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();
      
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---

  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
//---
   UpdateOrderArr();
   CheckForCloseOrder();
  }
//+------------------------------------------------------------------+
//| Tester function                                                  |
//+------------------------------------------------------------------+
double OnTester()
  {
//---
   double ret=0.0;
//---

//---
   return(ret);
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
//---
   
  }
//+------------------------------------------------------------------+
void InitOrderArr()
{
    //clear arrs
    for(int idx = 0;idx < MAXORDERSCNT;idx++)
    {
        arrOrderTicket[idx] = TICKET_INVALID;
        arrOrderOpenPrice[idx] = 0;
        arrOrderType[idx] = ORDER_TYPE_INVALID;
        arrOrderProfit[idx] = 0;
        arrOrderProfitSL[idx] = PROFIT_SL_INVALID;
        arrOrderProfitTP[idx] = PROFIT_TP_INVALID;
    }
}
//+------------------------------------------------------------------+
//| update all current tickets and other array                       |
//| ***important:this should not be called inside some for clause*** |
//| ***because the OrderSelect func maybe confused***                |
//+------------------------------------------------------------------+
void UpdateOrderArr()
{
    //local arr to record latest order ticket
    int latestarrOrderTicket[MAXORDERSCNT];
    //init
    for(int latestinitidx = 0;latestinitidx < MAXORDERSCNT;latestinitidx++)
    {
        latestarrOrderTicket[latestinitidx] = TICKET_INVALID;
    }
    //fill the latestarrOrderTicket
    for(int latestidx = 0;latestidx < OrdersTotal();latestidx++)
    {
       if(OrderSelect(latestidx,SELECT_BY_POS,MODE_TRADES)==false) break;
       latestarrOrderTicket[latestidx] = OrderTicket();
    }
    
    //reset arr when order closed(exist in arrOrderTicket but not in latestarrOrderTicket)
    for(int resetidx = 0;resetidx < MAXORDERSCNT;resetidx++)
    {
        if(TICKET_INVALID != arrOrderTicket[resetidx])
        {
            bool shouldreset = true;
            for(int latestidx = 0;latestidx < MAXORDERSCNT;latestidx++)
            {
                if((arrOrderTicket[resetidx] == latestarrOrderTicket[latestidx]))//found,the order is still open
                {
                    shouldreset = false;
                    break;
                }
            }
            if(shouldreset)//not found,should reset
            {
                arrOrderTicket[resetidx] = TICKET_INVALID;
                arrOrderOpenPrice[resetidx] = 0;
                arrOrderType[resetidx] = ORDER_TYPE_INVALID;
                arrOrderProfit[resetidx] = 0;
                arrOrderProfitSL[resetidx] = PROFIT_SL_INVALID;
                arrOrderProfitTP[resetidx] = PROFIT_TP_INVALID;
            }
        }
    }

    //update exist order's arrOrderProfitSL,or insert if order newly opened
    for(int idx = OrdersTotal() - 1;idx >= 0;idx--)
    {
       if(OrderSelect(idx,SELECT_BY_POS,MODE_TRADES)==false) break;
       int orderticket = OrderTicket();
       bool ticketexist = false;
       int ticketfindidx = 0;
       for(ticketfindidx = 0; ticketfindidx < MAXORDERSCNT;ticketfindidx++)//find if orderticket exist in arr
       {
          if((TICKET_INVALID != arrOrderTicket[ticketfindidx]) && (orderticket == arrOrderTicket[ticketfindidx]))
          {
             ticketexist = true;
             break;
          }
       }
       if(!ticketexist)//not exist,newly add
       {
          for(int ticketinvalid = 0; ticketinvalid < MAXORDERSCNT;ticketinvalid++)//find an invalid position and add it
          {
              if(TICKET_INVALID == arrOrderTicket[ticketinvalid])//find the first invalid ticket and add it 
              {
                 arrOrderTicket[ticketinvalid] = orderticket;
                 arrOrderOpenPrice[ticketinvalid] = OrderOpenPrice();
                 arrOrderType[ticketinvalid] = OrderType();
                 arrOrderProfit[ticketinvalid] = OrderProfit();
                 if((OrderProfit() > 0) && (OrderProfit() > ((OrderLots()/minlots)*minlotsprofittrailingsl)))
                 {
                    arrOrderProfitSL[ticketinvalid] = OrderProfit() - ((OrderLots()/minlots)*minlotsprofittrailingsl);
                 }
                 arrOrderProfitTP[ticketinvalid] = (OrderLots()/minlots)*minlotsprofittp;
                 break;
              }
          }
       }else//exist,update the arrOrderProfitSL if necessary
       {
          //import:ticketfindidx is the find ticket index,update here
          if((OrderProfit() > 0)
          && (OrderProfit() > ((OrderLots()/minlots)*minlotsprofittrailingsl))
          && (PROFIT_SL_INVALID != arrOrderProfitSL[ticketfindidx])
          && (arrOrderProfitSL[ticketfindidx] < OrderProfit() - ((OrderLots()/minlots)*minlotsprofittrailingsl))
          )
          {
             arrOrderProfitSL[ticketfindidx] = OrderProfit() - ((OrderLots()/minlots)*minlotsprofittrailingsl);
          }
          arrOrderProfitTP[ticketfindidx] = (OrderLots()/minlots)*minlotsprofittp;//if exist,this line could be deleted,no side effect
       }
    }
    //Testing
    /*for(int testidx = 0;testidx < MAXORDERSCNT;testidx++)
    {
       if(TICKET_INVALID != arrOrderTicket[testidx])
       {
           printf("ticket:" + (string)arrOrderTicket[testidx] + " openprice:" + (string)arrOrderOpenPrice[testidx] + " sl:" + (string)arrOrderProfitSL[testidx] + " tp:" + (string)arrOrderProfitTP[testidx]);
       }
       if(PROFIT_SL_INVALID != arrOrderProfitSL[testidx])
       {
           if(OrderSelect(arrOrderTicket[testidx],SELECT_BY_TICKET,MODE_TRADES)==false) break;
           printf("testidx:%d symbol:%s lot:%f arrOrderProfitSL:%f arrOrderProfitTP:%f",testidx,OrderSymbol(),OrderLots(),arrOrderProfitSL[testidx],arrOrderProfitTP[testidx]);
       }
    }*/
}

void CheckForCloseOrder()
{
   int idx = 0;
   printf("ProfitTrailing CheckForCloseOrder");
   for(idx = 0;idx < MAXORDERSCNT;idx++)
   {
       if(TICKET_INVALID != arrOrderTicket[idx])
       {
           if(OrderSelect(arrOrderTicket[idx], SELECT_BY_TICKET, MODE_TRADES)==false) break;
           if(OrderType() == OP_BUY || OrderType() == OP_SELL)//filled order
           {
               if(((PROFIT_SL_INVALID != arrOrderProfitSL[idx]) && (OrderProfit() < arrOrderProfitSL[idx])) || (OrderProfit() > arrOrderProfitTP[idx]))
               {
                  double currsymbolbid = MarketInfo(OrderSymbol(),MODE_BID);
                  double currsymbolask = MarketInfo(OrderSymbol(),MODE_ASK);
                  if(OrderType() == OP_BUY)
                  {
                     if(!OrderClose(OrderTicket(), OrderLots(), currsymbolbid, slippage, CLR_NONE))
                     {
                         printf("Buy Order %d close failed:%d",OrderTicket(),GetLastError());
                     }
                  }else//sell order
                  {
                     if(!OrderClose(OrderTicket(), OrderLots(), currsymbolask, slippage, CLR_NONE))
                     {
                        printf("Sell Order %d close failed:%d",OrderTicket(),GetLastError());
                     }
                  }
                  return ;//close one for each timer
               }
           }
       }
   }
}
