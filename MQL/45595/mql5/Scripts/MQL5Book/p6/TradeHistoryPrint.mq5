//+------------------------------------------------------------------+
//|                                            TradeHistoryPrint.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Print out positions/deal/orders from trade history               |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/OrderFilter.mqh>
#include <MQL5Book/DealFilter.mqh>

//+------------------------------------------------------------------+
//| History selection method                                         |
//+------------------------------------------------------------------+
enum SELECTOR_TYPE
{
   TOTAL,    // Whole history
   POSITION, // Position ID
};

input SELECTOR_TYPE Type = TOTAL;
input ulong PositionID = 0; // Position ID

//+------------------------------------------------------------------+
//| Custom tuple for pretty-printing deal properties                 |
//| First field must have name '_1' to be compatible with Filters    |
//+------------------------------------------------------------------+
struct DealTuple
{
   datetime _1;   // deal time
   ulong deal;
   ulong order;
   string type;
   string in_out;
   double volume;
   double price;
   double profit;

   static int size() { return 8; }; // number of properties
   static const int fields[]; // identifiers of requested deal properties

   template<typename M> // M should be MonitorInterface<>
   void assign(M &m)
   {
      static const int DEAL_TYPE_ = StringLen("DEAL_TYPE_");
      static const int DEAL_ENTRY_ = StringLen("DEAL_ENTRY_");
      static const ulong L = 0; // default type declarator (dummy)

      _1 = (datetime)m.get(fields[0], L);
      deal = m.get(fields[1], deal);
      order = m.get(fields[2], order);
      const ENUM_DEAL_TYPE t = (ENUM_DEAL_TYPE)m.get(fields[3], L);
      type = StringSubstr(EnumToString(t), DEAL_TYPE_);
      const ENUM_DEAL_ENTRY e = (ENUM_DEAL_ENTRY)m.get(fields[4], L);
      in_out = StringSubstr(EnumToString(e), DEAL_ENTRY_);
      volume = m.get(fields[5], volume);
      price = m.get(fields[6], price);
      profit = m.get(fields[7], profit);
   }
};

static const int DealTuple::fields[] =
{
   DEAL_TIME, DEAL_TICKET, DEAL_ORDER, DEAL_TYPE,
   DEAL_ENTRY, DEAL_VOLUME, DEAL_PRICE, DEAL_PROFIT
};

//+------------------------------------------------------------------+
//| Custom tuple for pretty-printing order properties                |
//| First field must have name '_1' to be compatible with Filters    |
//+------------------------------------------------------------------+
struct OrderTuple
{
   ulong _1;       // ticket (and 'ulong' type declarator)
   datetime setup;
   datetime done;
   string type;
   double volume;
   double open;
   double current;
   double sl;
   double tp;
   string comment;

   static int size() { return 10; }; // number of properties
   static const int fields[]; // identifiers of requested order properties

   template<typename M> // M should be MonitorInterface<>
   void assign(M &m)
   {
      static const int ORDER_TYPE_ = StringLen("ORDER_TYPE_");

      _1 = m.get(fields[0], _1);
      setup = (datetime)m.get(fields[1], _1);
      done = (datetime)m.get(fields[2], _1);
      const ENUM_ORDER_TYPE t = (ENUM_ORDER_TYPE)m.get(fields[3], _1);
      type = StringSubstr(EnumToString(t), ORDER_TYPE_);
      volume = m.get(fields[4], volume);
      open = m.get(fields[5], open);
      current = m.get(fields[6], current);
      sl = m.get(fields[7], sl);
      tp = m.get(fields[8], tp);
      comment = m.get(fields[9], comment);
   }
};

static const int OrderTuple::fields[] =
{
   ORDER_TICKET, ORDER_TIME_SETUP, ORDER_TIME_DONE, ORDER_TYPE, ORDER_VOLUME_INITIAL,
   ORDER_PRICE_OPEN, ORDER_PRICE_CURRENT, ORDER_SL, ORDER_TP, ORDER_COMMENT
};

//+------------------------------------------------------------------+
//| Help function to remove duplicates from array                    |
//+------------------------------------------------------------------+
template<typename T>
int ArrayUnique(T &array[], const T empty = (T)NULL)
{
   const int n = ArraySize(array);
   for(int i = 1; i < n; ++i)
   {
      if(array[i] == array[i - 1])
      {
         array[i - 1] = empty;
      }
   }
   
   int i = 0, w = 0;
   while(i < n)
   {
      while(i < n && array[i] == empty)
      {
         i++;
      }
     
      while(i < n && array[i] != empty)
      {
         if(w < i)
         {
            array[w] = array[i];
         }
         i++;
         w++;
      }
   }
   return n - ArrayResize(array, w);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // filter objects for deals and orders
   DealFilter filter;
   HistoryOrderFilter subfilter;
   
   // select specific history
   if(PositionID == 0 || Type == TOTAL)
   {
      HistorySelect(0, LONG_MAX);
   }
   else if(Type == POSITION)
   {
      HistorySelectByPosition(PositionID);
   }
   
   // collect position IDs (all or a given single one)
   ulong positions[];
   if(PositionID == 0)
   {
      ulong tickets[];
      filter.let(DEAL_SYMBOL, _Symbol).select(DEAL_POSITION_ID, tickets, positions, true);
      ArrayUnique(positions);
   }
   else
   {
      PUSH(positions, PositionID);
   }

   const int n = ArraySize(positions);
   Print("Positions total: ", n);
   if(n == 0) return;
   
   // show all positions: deals and orders
   for(int i = 0; i < n; ++i)
   {
      DealTuple deals[];
      filter.let(DEAL_POSITION_ID, positions[i]).select(deals, true);
      const int m = ArraySize(deals);
      if(m == 0)
      {
         Print("Wrong position ID: ", positions[i]);
         break; // wrong position id specfied by user
      }
      double profit = 0; // TODO: consider commissons, swaps and fees, if needed
      for(int j = 0; j < m; ++j) profit += deals[j].profit;
      PrintFormat("Position: % 8d %16lld Profit:%f", i + 1, positions[i], (profit));
      ArrayPrint(deals);
      
      // NB: 'close-by' orders will be listed on both positions
      // only if 'Whole history' is selected, because otherwise
      // HistorySelectByPosition is used
      // which selects 'close-by' order only by one side

      Print("Order details:");
      OrderTuple orders[];
      subfilter.let(ORDER_POSITION_ID, positions[i], IS::OR_EQUAL)
         .let(ORDER_POSITION_BY_ID, positions[i], IS::OR_EQUAL)
         .select(orders);
      ArrayPrint(orders);
   }
}
//+------------------------------------------------------------------+
/*
   EXAMPLE 1 (default settings)

   Positions total: 3
   Position:        1       1253500309 Profit:238.150000
                      [_1]     [deal]    [order] [type] [in_out] [volume]  [price]  [profit]
   [0] 2022.02.04 17:34:57 1236049891 1253500309 "BUY"  "IN"      1.00000 76.23900   0.00000
   [1] 2022.02.14 16:28:41 1242295527 1259788704 "SELL" "OUT"     1.00000 76.42100 238.15000
   Order details:
             [_1]             [setup]              [done] [type] [volume]   [open] [current] [sl] [tp] [comment]
   [0] 1253500309 2022.02.04 17:34:57 2022.02.04 17:34:57 "BUY"   1.00000 76.23900  76.23900 0.00 0.00 ""       
   [1] 1259788704 2022.02.14 16:28:41 2022.02.14 16:28:41 "SELL"  1.00000 76.42100  76.42100 0.00 0.00 ""       
   Position:        2       1253526613 Profit:878.030000
                      [_1]     [deal]    [order] [type] [in_out] [volume]  [price]  [profit]
   [0] 2022.02.07 10:00:00 1236611994 1253526613 "BUY"  "IN"      1.00000 75.75000   0.00000
   [1] 2022.02.14 16:28:40 1242295517 1259788693 "SELL" "OUT"     1.00000 76.42100 878.03000
   Order details:
             [_1]             [setup]              [done]      [type] [volume]   [open] [current] [sl] [tp] [comment]
   [0] 1253526613 2022.02.04 17:55:18 2022.02.07 10:00:00 "BUY_LIMIT"  1.00000 75.75000  75.67000 0.00 0.00 ""       
   [1] 1259788693 2022.02.14 16:28:40 2022.02.14 16:28:40 "SELL"       1.00000 76.42100  76.42100 0.00 0.00 ""       
   Position:        3       1256280710 Profit:4449.040000
                      [_1]     [deal]    [order] [type] [in_out] [volume]  [price]   [profit]
   [0] 2022.02.09 13:17:52 1238797056 1256280710 "BUY"  "IN"      2.00000 74.72100    0.00000
   [1] 2022.02.14 16:28:39 1242295509 1259788685 "SELL" "OUT"     2.00000 76.42100 4449.04000
   Order details:
             [_1]             [setup]              [done] [type] [volume]   [open] [current] [sl] [tp] [comment]
   [0] 1256280710 2022.02.09 13:17:52 2022.02.09 13:17:52 "BUY"   2.00000 74.72100  74.72100 0.00 0.00 ""       
   [1] 1259788685 2022.02.14 16:28:39 2022.02.14 16:28:39 "SELL"  2.00000 76.42100  76.42100 0.00 0.00 ""       


   EXAMPLE 2 (PositionID=1276109280, Type=Whole history/Position)

   Positions total: 1
   Position:        1       1276109280 Profit:-0.040000
                      [_1]     [deal]    [order] [type] [in_out] [volume] [price] [profit]
   [0] 2022.03.07 12:20:53 1258725455 1276109280 "BUY"  "IN"      0.01000 1.08344  0.00000
   [1] 2022.03.07 12:20:58 1258725503 1276109328 "SELL" "OUT_BY"  0.01000 1.08340 -0.04000
   Order details:
             [_1]             [setup]              [done]     [type] [volume]  [open] [current] [sl] [tp]                    [comment]
   [0] 1276109280 2022.03.07 12:20:53 2022.03.07 12:20:53 "BUY"       0.01000 1.08344   1.08344 0.00 0.00 ""                          
   [1] 1276109328 2022.03.07 12:20:58 2022.03.07 12:20:58 "CLOSE_BY"  0.01000 1.08340   1.08340 0.00 0.00 "#1276109280 by #1276109283"

   
   EXAMPLE 3 (PositionID=1276109283, Type=Position) - note that exit order (1276109328) is missing

   Positions total: 1
   Position:        1       1276109283 Profit:0.000000
                      [_1]     [deal]    [order] [type] [in_out] [volume] [price] [profit]
   [0] 2022.03.07 12:20:53 1258725458 1276109283 "SELL" "IN"      0.01000 1.08340  0.00000
   [1] 2022.03.07 12:20:58 1258725504 1276109328 "BUY"  "OUT_BY"  0.01000 1.08344  0.00000
   Order details:
             [_1]             [setup]              [done] [type] [volume]  [open] [current] [sl] [tp] [comment]
   [0] 1276109283 2022.03.07 12:20:53 2022.03.07 12:20:53 "SELL"  0.01000 1.08340   1.08340 0.00 0.00 ""       


   EXAMPLE 4 (PositionID=1276109283, Type=Whole history) - note that exit order is present
   
   Positions total: 1
   Position:        1       1276109283 Profit:0.000000
                      [_1]     [deal]    [order] [type] [in_out] [volume] [price] [profit]
   [0] 2022.03.07 12:20:53 1258725458 1276109283 "SELL" "IN"      0.01000 1.08340  0.00000
   [1] 2022.03.07 12:20:58 1258725504 1276109328 "BUY"  "OUT_BY"  0.01000 1.08344  0.00000
   Order details:
             [_1]             [setup]              [done]     [type] [volume]  [open] [current] [sl] [tp]                    [comment]
   [0] 1276109283 2022.03.07 12:20:53 2022.03.07 12:20:53 "SELL"      0.01000 1.08340   1.08340 0.00 0.00 ""                          
   [1] 1276109328 2022.03.07 12:20:58 2022.03.07 12:20:58 "CLOSE_BY"  0.01000 1.08340   1.08340 0.00 0.00 "#1276109280 by #1276109283"
   

*/
//+------------------------------------------------------------------+
