//+------------------------------------------------------------------+
//|                                      Get_Last_Nth_Open_Trade.mq4 |
//|                                  Copyright 2024, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "This EA will scan all the open trades and then print the nth trade from the end"
#property copyright "https://tradingbotmaker.com/"
#property description  "Email - support@tradingbotmaker.com "
#property description  "Telegram - @pops1990 "
#property version "1.0"
#property version   "1.00"
#property strict



#include <Trade\SymbolInfo.mqh>
#include <Trade\PositionInfo.mqh>

CSymbolInfo       m_symbol;                     // symbol info object
CPositionInfo     m_position;                   // trade position object


// Define a global struct to store the trade details
struct OrderDetails
  {
   ulong               ticket;
   string            symbol;
   double            lots;
   double            openPrice;
   double            stopLoss;
   double            takeProfit;
   double            profit;
   string            comment;
   int               type;
   string            typeDescription;
   datetime          orderOpenTime;
   datetime          orderCloseTime;

  } orderDetails;


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
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   m_symbol.RefreshRates();
   int totalOrders = TotalOrders();
   if(TRADE_INDEX>=0 && TRADE_INDEX<totalOrders && totalOrders>0)
      GetLastNthActiveTradeDetails(TRADE_INDEX);
  }

//+------------------------------------------------------------------+
//|  Get Total Orders                                                       |
//+------------------------------------------------------------------+
int TotalOrders()
  {
   int count=0;
   for(int i=0; i<PositionsTotal(); i++)
     {
      if(m_position.SelectByIndex(i) && ((ENABLE_SYMBOL_FILTER && m_position.Symbol()==Symbol()) || !ENABLE_SYMBOL_FILTER) && ((ENABLE_MAGIC_NUMBER && m_position.Magic()==MAGICNUMBER) || !ENABLE_MAGIC_NUMBER) && (m_position.TypeDescription()=="buy" ||m_position.TypeDescription()=="sell"))
         count++;

     }
   return count;
  }

//+------------------------------------------------------------------+
//|        Sort the struct by order open time                        |
//+------------------------------------------------------------------+
void SortByTicket(OrderDetails &array[], int array_size)
  {
   for(int i = 0; i < array_size - 1; i++)
     {
      for(int j = i + 1; j < array_size; j++)
        {
         if(array[i].ticket < array[j].ticket)
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
void GetLastNthActiveTradeDetails(int index)
  {
// Get the total number of open trades
   int totalTrades = TotalOrders();
   if(totalTrades==0)
     {
      orderDetails.ticket = 0;
      orderDetails.symbol = "";
      orderDetails.lots = 0;
      orderDetails.openPrice = 0;
      orderDetails.stopLoss = 0;
      orderDetails.takeProfit = 0;
      orderDetails.profit = 0;
      orderDetails.comment = "";
      orderDetails.type =0;
      orderDetails.orderOpenTime= 0;
      orderDetails.orderCloseTime=0;
      return;
     }
   OrderDetails trades[];
   ArrayResize(trades,totalTrades);
   int count=0;
   for(int i = 0; i <PositionsTotal(); i++)
     {
      // Get the trade information for the current trade
      if(m_position.SelectByIndex(i) && ((ENABLE_SYMBOL_FILTER && m_position.Symbol()==Symbol()) || !ENABLE_SYMBOL_FILTER) && ((ENABLE_MAGIC_NUMBER && m_position.Magic()==MAGICNUMBER) || !ENABLE_MAGIC_NUMBER))
        {
         trades[count].ticket = m_position.Ticket();
         trades[count].symbol = m_position.Symbol();
         trades[count].lots = m_position.Volume();
         trades[count].openPrice = m_position.PriceOpen();
         trades[count].stopLoss = m_position.StopLoss();
         trades[count].takeProfit = m_position.TakeProfit();
         trades[count].profit = m_position.Profit();
         trades[count].comment = m_position.Comment();
         trades[count].type = m_position.Type();
         trades[count].typeDescription = m_position.TypeDescription();
         trades[count].orderOpenTime= m_position.Time();
         count++;
        }

     }
   SortByTicket(trades,totalTrades);
   orderDetails.ticket=trades[index].ticket;
   orderDetails.symbol=trades[index].symbol;
   orderDetails.lots=trades[index].lots;
   orderDetails.openPrice=trades[index].openPrice;
   orderDetails.stopLoss=trades[index].stopLoss;
   orderDetails.takeProfit=trades[index].takeProfit;
   orderDetails.profit=trades[index].profit;
   orderDetails.comment=trades[index].comment;
   orderDetails.type=trades[index].type;
   orderDetails.typeDescription=trades[index].typeDescription;
   orderDetails.orderOpenTime=trades[index].orderOpenTime;

   string lastTradeInfo = "";
   lastTradeInfo+=("ticket "+(string)orderDetails.ticket)+"\n";
   lastTradeInfo+=("symbol "+orderDetails.symbol)+"\n";
   lastTradeInfo+=("lots "+(string)orderDetails.lots)+"\n";
   lastTradeInfo+=("openPrice "+(string)orderDetails.openPrice)+"\n";
   lastTradeInfo+=("stopLoss "+(string)orderDetails.stopLoss)+"\n";
   lastTradeInfo+=("takeProfit "+(string)orderDetails.takeProfit)+"\n";
   lastTradeInfo+=("comment "+orderDetails.comment)+"\n";
   lastTradeInfo+=("type "+(string)orderDetails.type)+"\n";
   lastTradeInfo+=("typeDescription "+orderDetails.typeDescription)+"\n";
   lastTradeInfo+=("orderOpenTime "+(string)orderDetails.orderOpenTime)+"\n";
   Comment(lastTradeInfo);
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
