//+------------------------------------------------------------------+
//|                                      Get_Last_Nth_Close_Trade.mq4 |
//|                                  Copyright 2024, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "This EA will scan all the closed trades and then print the nth trade from the end"
#property copyright "https://tradingbotmaker.com/"
#property description  "Email - support@tradingbotmaker.com "
#property description  "Telegram - @pops1990 "
#property version "1.0"
#property version   "1.00"
#property strict

// Define a global struct to store the trade details
struct OrderDetails
  {
   int               ticket;
   string            symbol;
   double            lots;
   double            openPrice;
   double            closePrice;
   double            stopLoss;
   double            takeProfit;
   double            profit;
   string            comment;
   int               type;
   datetime          orderOpenTime;
   datetime          orderCloseTime;

  } orderHistoryDetails;


input bool ENABLE_MAGIC_NUMBER= false; // Enable Magic Number
input bool ENABLE_SYMBOL_FILTER=false; // Enable Symbol filter
int MAGICNUMBER=1234; // Magic Number
input int TRADE_INDEX=2; // Trade Index
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
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
   RefreshRates();
   int totalOrders = TotalClosedOrders();
   if(TRADE_INDEX>=0 && TRADE_INDEX<totalOrders && totalOrders>0)
      GetLastNthClosedTradeDetails(TRADE_INDEX);
  }

//+------------------------------------------------------------------+
//|  Get Total Orders                                                       |
//+------------------------------------------------------------------+
int TotalClosedOrders()
  {
   int count=0;
   for(int i=0; i<OrdersHistoryTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY) && ((ENABLE_SYMBOL_FILTER && OrderSymbol()==Symbol()) || !ENABLE_SYMBOL_FILTER) && ((ENABLE_MAGIC_NUMBER && OrderMagicNumber()==MAGICNUMBER) || !ENABLE_MAGIC_NUMBER) && OrderType()!=6 && OrderType()!=7)
         count++;

     }
   return count;
  }

//+------------------------------------------------------------------+
//|        Sort the struct by order open time                        |
//+------------------------------------------------------------------+
void SortByCloseTime(OrderDetails &array[], int array_size)
  {
   for(int i = 0; i < array_size - 1; i++)
     {
      for(int j = i + 1; j < array_size; j++)
        {
         if(array[i].orderCloseTime < array[j].orderCloseTime)
           {
            // Swap elements if the age of person at index i is greater than person at index j
            OrderDetails temp = array[i];
            array[i] = array[j];
            array[j] = temp;
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|        Get the details of the last open trade                    |
//+------------------------------------------------------------------+
void GetLastNthClosedTradeDetails(int index)
  {
// Get the total number of open trades
   int totalTrades = TotalClosedOrders();
   if(totalTrades==0)
     {
      orderHistoryDetails.ticket = 0;
      orderHistoryDetails.symbol = "";
      orderHistoryDetails.lots = 0;
      orderHistoryDetails.openPrice = 0;
      orderHistoryDetails.stopLoss = 0;
      orderHistoryDetails.takeProfit = 0;
      orderHistoryDetails.profit = 0;
      orderHistoryDetails.comment = "";
      orderHistoryDetails.type =0;
      orderHistoryDetails.orderOpenTime= 0;
      orderHistoryDetails.orderCloseTime=0;
      return;
     }
   OrderDetails trades[];
   ArrayResize(trades,totalTrades);
   int count=0;
   for(int i = 0; i <OrdersHistoryTotal(); i++)
     {
      // Get the trade information for the current trade
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY) && ((ENABLE_SYMBOL_FILTER && OrderSymbol()==Symbol()) || !ENABLE_SYMBOL_FILTER) && ((ENABLE_MAGIC_NUMBER && OrderMagicNumber()==MAGICNUMBER) || !ENABLE_MAGIC_NUMBER) && OrderType()!=6 && OrderType()!=7)
        {
         trades[count].ticket = OrderTicket();
         trades[count].symbol = OrderSymbol();
         trades[count].lots = OrderLots();
         trades[count].openPrice = OrderOpenPrice();
         trades[count].stopLoss = OrderStopLoss();
         trades[count].takeProfit = OrderTakeProfit();
         trades[count].profit = OrderProfit();
         trades[count].comment = OrderComment();
         trades[count].type =OrderType();
         trades[count].orderOpenTime= OrderOpenTime();
         trades[count].orderCloseTime=OrderCloseTime();
         count++;
        }

     }
   SortByCloseTime(trades,totalTrades);
   orderHistoryDetails.ticket=trades[index].ticket;
   orderHistoryDetails.symbol=trades[index].symbol;
   orderHistoryDetails.lots=trades[index].lots;
   orderHistoryDetails.openPrice=trades[index].openPrice;
   orderHistoryDetails.stopLoss=trades[index].stopLoss;
   orderHistoryDetails.takeProfit=trades[index].takeProfit;
   orderHistoryDetails.profit=trades[index].profit;
   orderHistoryDetails.comment=trades[index].comment;
   orderHistoryDetails.type=trades[index].type;
   orderHistoryDetails.orderOpenTime=trades[index].orderOpenTime;
   orderHistoryDetails.orderCloseTime=trades[index].orderCloseTime;

   string lastTradeInfo = "";
   lastTradeInfo+=("ticket"+(string)orderHistoryDetails.ticket)+"\n";
   lastTradeInfo+=("symbol"+orderHistoryDetails.symbol)+"\n";
   lastTradeInfo+=("lots"+(string)orderHistoryDetails.lots)+"\n";
   lastTradeInfo+=("openPrice"+(string)orderHistoryDetails.openPrice)+"\n";
   lastTradeInfo+=("closePrice"+(string)orderHistoryDetails.closePrice)+"\n";
   lastTradeInfo+=("stopLoss"+(string)orderHistoryDetails.stopLoss)+"\n";
   lastTradeInfo+=("takeProfit"+(string)orderHistoryDetails.takeProfit)+"\n";
   lastTradeInfo+=("comment"+orderHistoryDetails.comment)+"\n";
   lastTradeInfo+=("type"+(string)orderHistoryDetails.type)+"\n";
   lastTradeInfo+=("orderOpenTime"+(string)orderHistoryDetails.orderOpenTime)+"\n";
   lastTradeInfo+=("orderCloseTime"+(string)orderHistoryDetails.orderCloseTime);
   Comment(lastTradeInfo);
  }
//+------------------------------------------------------------------+
