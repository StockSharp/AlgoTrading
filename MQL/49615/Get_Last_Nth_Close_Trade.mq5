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
#property strict



#include <Trade\SymbolInfo.mqh>
#include <Trade\PositionInfo.mqh>

CSymbolInfo       m_symbol;                     // symbol info object

// Define a global struct to store the trade details
struct OrderHistoryDetails
  {
   ulong             ticket;
   ulong             position_ticket;
   string            symbol;
   double            lots;
   double            openPrice;
   double            closePrice;
   double            stopLoss;
   double            takeProfit;
   double            profit;
   string            comment;
   long               type;
   string            typeDescription;
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
   m_symbol.Name(Symbol());
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
int totalClosedOrder=0;
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   m_symbol.RefreshRates();
   HistorySelect(0,TimeCurrent());
   totalClosedOrder =HistoryDealsTotal();
   int totalFilteredClosedOrder = TotalClosedOrders();
   if(TRADE_INDEX>=0 && TRADE_INDEX<totalFilteredClosedOrder && totalFilteredClosedOrder>0)
      GetLastNthClosedTradeDetails(TRADE_INDEX);
  }

//+------------------------------------------------------------------+
//|  Get Total Orders                                                       |
//+------------------------------------------------------------------+
int TotalClosedOrders()
  {
   int count=0;
   HistorySelect(0,TimeCurrent());
   int deals=HistoryDealsTotal();
//--- now process each trade
   for(int i=0;i<deals;i++)
     {
      ulong ticket = HistoryDealGetTicket(i);
      ulong deal_ticket = HistoryDealGetInteger(ticket,DEAL_TICKET);
      long entry = HistoryDealGetInteger(ticket,DEAL_ENTRY);
      string deal_symbol= HistoryDealGetString(ticket,DEAL_SYMBOL);
      long deal_magic =HistoryDealGetInteger(ticket,DEAL_MAGIC);
      long deal_type = HistoryDealGetInteger(ticket,DEAL_TYPE);
      if(entry==DEAL_ENTRY_OUT && ((ENABLE_SYMBOL_FILTER && deal_symbol==Symbol()) || !ENABLE_SYMBOL_FILTER) && ((ENABLE_MAGIC_NUMBER && deal_magic==MAGICNUMBER) || !ENABLE_MAGIC_NUMBER) && (deal_type==DEAL_TYPE_BUY || deal_type==DEAL_TYPE_SELL))
         count++;
     }
   return count;
  }

//+------------------------------------------------------------------+
//|        Sort the struct by order open time                        |
//+------------------------------------------------------------------+
void SortByCloseTime(OrderHistoryDetails &array[], int array_size)
  {
   for(int i = 0; i < array_size - 1; i++)
     {
      for(int j = i + 1; j < array_size; j++)
        {
         if(array[i].orderCloseTime < array[j].orderCloseTime)
           {
            // Swap elements if the age of person at index i is greater than person at index j
            OrderHistoryDetails temp = array[i];
            array[i] = array[j];
            array[j] = temp;
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|        Get the details of the last closed trade                    |
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
   OrderHistoryDetails trades[];
   ArrayResize(trades,totalTrades);
   int count=0;
   for(int i = 0; i <HistoryDealsTotal(); i++)
     {
      ulong ticket = HistoryDealGetTicket(i);
      long entry = HistoryDealGetInteger(ticket,DEAL_ENTRY);
      string deal_symbol= HistoryDealGetString(ticket,DEAL_SYMBOL);
      long deal_magic =HistoryDealGetInteger(ticket,DEAL_MAGIC);
      long deal_type = HistoryDealGetInteger(ticket,DEAL_TYPE);
      // Get the trade information for the current trade
      if(entry==DEAL_ENTRY_OUT && ((ENABLE_SYMBOL_FILTER && deal_symbol==Symbol()) || !ENABLE_SYMBOL_FILTER) && ((ENABLE_MAGIC_NUMBER && deal_magic==MAGICNUMBER) || !ENABLE_MAGIC_NUMBER) && (deal_type==DEAL_TYPE_BUY || deal_type==DEAL_TYPE_SELL))
        {
         trades[count].ticket = ticket;
         trades[count].symbol = deal_symbol;
         trades[count].position_ticket = HistoryDealGetInteger(ticket,DEAL_POSITION_ID);
         trades[count].lots = HistoryDealGetDouble(ticket,DEAL_VOLUME);
         trades[count].stopLoss = HistoryDealGetDouble(ticket,DEAL_SL) ;
         trades[count].takeProfit = HistoryDealGetDouble(ticket,DEAL_TP) ;
         trades[count].profit = HistoryDealGetDouble(ticket,DEAL_PROFIT) ;
         trades[count].comment = HistoryDealGetString(ticket,DEAL_COMMENT) ;
         trades[count].type =HistoryDealGetInteger(ticket,DEAL_TYPE) ;
         trades[count].typeDescription= trades[count].type==DEAL_TYPE_BUY ? "buy":"sell";
         trades[count].closePrice= HistoryDealGetDouble(ticket,DEAL_PRICE) ;
         trades[count].orderCloseTime= (datetime)HistoryDealGetInteger(ticket,DEAL_TIME);
         count++;
         if(count>=ArraySize(trades))
            break;
        }

     }
   for(int i = 0; i <HistoryDealsTotal(); i++)
     {
      ulong ticket = HistoryDealGetTicket(i);
      long entry = HistoryDealGetInteger(ticket,DEAL_ENTRY);
      string deal_symbol= HistoryDealGetString(ticket,DEAL_SYMBOL);
      long deal_magic =HistoryDealGetInteger(ticket,DEAL_MAGIC);
      long deal_type = HistoryDealGetInteger(ticket,DEAL_TYPE);
      ulong position_ticket = HistoryDealGetInteger(ticket,DEAL_POSITION_ID);
      // Get the trade information for the current trade
      if(entry==DEAL_ENTRY_IN  && ((ENABLE_SYMBOL_FILTER && deal_symbol==Symbol()) || !ENABLE_SYMBOL_FILTER) && ((ENABLE_MAGIC_NUMBER && deal_magic==MAGICNUMBER) || !ENABLE_MAGIC_NUMBER) && (deal_type==DEAL_TYPE_BUY || deal_type==DEAL_TYPE_SELL))
        {
         for(int j=0;j<ArraySize(trades);j++)
           {
            if(trades[j].position_ticket ==position_ticket)
              {
               trades[j].openPrice= HistoryDealGetDouble(ticket,DEAL_PRICE) ;
               trades[j].orderOpenTime= (datetime)HistoryDealGetInteger(ticket,DEAL_TIME);
              }
           }
        }
     }
   SortByCloseTime(trades,totalTrades);
   orderHistoryDetails.ticket=trades[index].ticket;
   orderHistoryDetails.symbol=trades[index].symbol;
   orderHistoryDetails.lots=trades[index].lots;
   orderHistoryDetails.openPrice=trades[index].openPrice;
   orderHistoryDetails.closePrice=trades[index].closePrice;
   orderHistoryDetails.stopLoss=trades[index].stopLoss;
   orderHistoryDetails.takeProfit=trades[index].takeProfit;
   orderHistoryDetails.profit=trades[index].profit;
   orderHistoryDetails.comment=trades[index].comment;
   orderHistoryDetails.type=trades[index].type;
   orderHistoryDetails.typeDescription=trades[index].typeDescription;
   orderHistoryDetails.orderOpenTime=trades[index].orderOpenTime;
   orderHistoryDetails.orderCloseTime=trades[index].orderCloseTime;

   string lastTradeInfo = "";
   lastTradeInfo+=("ticket "+(string)orderHistoryDetails.ticket)+"\n";
   lastTradeInfo+=("symbol "+orderHistoryDetails.symbol)+"\n";
   lastTradeInfo+=("lots "+(string)orderHistoryDetails.lots)+"\n";
   lastTradeInfo+=("openPrice "+(string)orderHistoryDetails.openPrice)+"\n";
   lastTradeInfo+=("closePrice "+(string)orderHistoryDetails.closePrice)+"\n";
   lastTradeInfo+=("stopLoss "+(string)orderHistoryDetails.stopLoss)+"\n";
   lastTradeInfo+=("takeProfit "+(string)orderHistoryDetails.takeProfit)+"\n";
   lastTradeInfo+=("comment "+orderHistoryDetails.comment)+"\n";
   lastTradeInfo+=("type "+(string)orderHistoryDetails.type)+"\n";
   lastTradeInfo+=("typeDescription "+orderHistoryDetails.typeDescription)+"\n";
   lastTradeInfo+=("orderOpenTime "+(string)orderHistoryDetails.orderOpenTime)+"\n";
   lastTradeInfo+=("orderCloseTime "+(string)orderHistoryDetails.orderCloseTime)+"\n";
   Comment(lastTradeInfo);
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
