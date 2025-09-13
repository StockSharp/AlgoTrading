//+------------------------------------------------------------------+
//|                                                  CustomTrade.mqh |
//|                                Copyright © 2022, MetaQuotes Ltd. |
//|                                            https://www.mql5.com/ |
//|                                                                  |
//| Simulate trade operations, market and pending orders             |
//|                                                                  |
//| status: experimental                                             |
//+------------------------------------------------------------------+

#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/Defines.mqh>
#include <MQL5Book/MarginProfitMeter.mqh>

#define HistorySelect CustomTrade::MT5HistorySelect
#define HistorySelectByPosition CustomTrade::MT5HistorySelectByPosition
#define PositionGetInteger CustomTrade::MT5PositionGetInteger
#define PositionGetDouble CustomTrade::MT5PositionGetDouble
#define PositionGetString CustomTrade::MT5PositionGetString
#define PositionSelect CustomTrade::MT5PositionSelect
#define PositionSelectByTicket CustomTrade::MT5PositionSelectByTicket
#define PositionsTotal CustomTrade::MT5PositionsTotal
#define OrdersTotal CustomTrade::MT5OrdersTotal
#define PositionGetSymbol CustomTrade::MT5PositionGetSymbol
#define PositionGetTicket CustomTrade::MT5PositionGetTicket
#define HistoryDealsTotal CustomTrade::MT5HistoryDealsTotal
#define HistoryOrdersTotal CustomTrade::MT5HistoryOrdersTotal
#define HistoryDealGetTicket CustomTrade::MT5HistoryDealGetTicket
#define HistoryOrderGetTicket CustomTrade::MT5HistoryOrderGetTicket
#define HistoryDealGetInteger CustomTrade::MT5HistoryDealGetInteger
#define HistoryDealGetDouble CustomTrade::MT5HistoryDealGetDouble
#define HistoryDealGetString CustomTrade::MT5HistoryDealGetString
#define HistoryOrderGetDouble CustomTrade::MT5HistoryOrderGetDouble
#define HistoryOrderGetInteger CustomTrade::MT5HistoryOrderGetInteger
#define HistoryOrderGetString CustomTrade::MT5HistoryOrderGetString
#define OrderSend CustomTrade::MT5OrderSend
#define OrderSelect CustomTrade::MT5OrderSelect
#define HistoryOrderSelect CustomTrade::MT5HistoryOrderSelect
#define HistoryDealSelect CustomTrade::MT5HistoryDealSelect
// don't use standard TimeCurrent in virtual back tests!
#define TimeCurrent CustomTrade::MT5TimeCurrent
#define DAY_LONG (60 * 60 * 24)

#include "TradeBaseMonitor.mqh"
//+------------------------------------------------------------------+
//| Implementation limitations                                       |
//| - no rejects or deviations                                       |
//| - no partial execution                                           |
//| - no support for all trade actions                               |
//| - no fees etc                                                    |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| CustomTrade namespace                                            |
//+------------------------------------------------------------------+
namespace CustomTrade
{

//+------------------------------------------------------------------+
//| CustomOrder                                                      |
//+------------------------------------------------------------------+
class CustomOrder: public MonitorInterface<ENUM_ORDER_PROPERTY_INTEGER,ENUM_ORDER_PROPERTY_DOUBLE,ENUM_ORDER_PROPERTY_STRING>::TradeState
{
   static long ticket;
   static long done;
public:
   CustomOrder(const ENUM_ORDER_TYPE type, const double volume, const string symbol)
   {
      _set(ORDER_TYPE, type);
      _set(ORDER_TICKET, ++ticket);
      _set(ORDER_TIME_SETUP, SymbolInfoInteger(symbol, SYMBOL_TIME));
      _set(ORDER_TIME_SETUP_MSC, SymbolInfoInteger(symbol, SYMBOL_TIME_MSC));
      if(type <= ORDER_TYPE_SELL)
      {
         // TODO: emulate deferred execution
         setDone(ORDER_STATE_FILLED);
      }
      else
      {
         _set(ORDER_STATE, ORDER_STATE_PLACED);
      }
      
      _set(ORDER_VOLUME_INITIAL, volume);
      _set(ORDER_VOLUME_CURRENT, volume);
      
      _set(ORDER_SYMBOL, symbol); // support multi-symbol
   }
   
   void setDone(const ENUM_ORDER_STATE state)
   {
      const string symbol = _get<string>(ORDER_SYMBOL);
      _set(ORDER_TIME_DONE, SymbolInfoInteger(symbol, SYMBOL_TIME));
      _set(ORDER_TIME_DONE_MSC, SymbolInfoInteger(symbol, SYMBOL_TIME_MSC));
      _set(ORDER_STATE, state);
      ++done;
   }
   
   bool isActive() const
   {
      return _get<long>(ORDER_TIME_DONE) == 0;
   }
   
   static long getDoneCount()
   {
      return done;
   }
};

static long CustomOrder::ticket = 0;
static long CustomOrder::done = 0;

//+------------------------------------------------------------------+
//| CustomDeal                                                       |
//+------------------------------------------------------------------+
class CustomDeal: public MonitorInterface<ENUM_DEAL_PROPERTY_INTEGER,ENUM_DEAL_PROPERTY_DOUBLE,ENUM_DEAL_PROPERTY_STRING>::TradeState
{
   static long ticket;
public:
   const CustomOrder *order;
   CustomDeal(const CustomOrder *o, const ENUM_DEAL_ENTRY entry): order(o)
   {
      const string symbol = order._get<string>(ORDER_SYMBOL);
      _set(DEAL_TICKET, ++ticket);
      _set(DEAL_ORDER, order._get<long>(ORDER_TICKET));
      _set(DEAL_TIME, SymbolInfoInteger(symbol, SYMBOL_TIME));
      _set(DEAL_TIME_MSC, SymbolInfoInteger(symbol, SYMBOL_TIME_MSC));
      _set(DEAL_TYPE, order._get<long>(ORDER_TYPE) % 2);
      _set(DEAL_ENTRY, entry);
      _set(DEAL_POSITION_ID, order._get<long>(ORDER_POSITION_ID));
      
      _set(DEAL_VOLUME, order._get<double>(ORDER_VOLUME_CURRENT));
      _set(DEAL_PRICE, order._get<double>(ORDER_PRICE_CURRENT));
      
      _set(DEAL_SYMBOL, order._get<string>(ORDER_SYMBOL));
   }
};

static long CustomDeal::ticket = 0;

//+------------------------------------------------------------------+
//| CustomPosition                                                   |
//+------------------------------------------------------------------+
class CustomPosition: public MonitorInterface<ENUM_POSITION_PROPERTY_INTEGER,ENUM_POSITION_PROPERTY_DOUBLE,ENUM_POSITION_PROPERTY_STRING>::TradeState
{
   static long ticket;
public:
   CustomPosition(CustomDeal *deal)
   {
      const string symbol = deal._get<string>(DEAL_SYMBOL);
      _set(POSITION_TICKET, ++ticket);
      _set(POSITION_IDENTIFIER, ticket);
      _set(POSITION_TIME, SymbolInfoInteger(symbol, SYMBOL_TIME));
      _set(POSITION_TIME_MSC, SymbolInfoInteger(symbol, SYMBOL_TIME_MSC));
      _set(POSITION_TYPE, deal._get<long>(DEAL_TYPE));
      _set(POSITION_VOLUME, deal._get<double>(DEAL_VOLUME));
      _set(POSITION_PRICE_OPEN, deal._get<double>(DEAL_PRICE));
      _set(POSITION_PRICE_CURRENT, deal._get<double>(DEAL_PRICE));
      _set(POSITION_PROFIT, deal._get<double>(DEAL_PROFIT));
      _set(POSITION_SYMBOL, deal._get<string>(DEAL_SYMBOL));
      _set(POSITION_SL, deal._get<double>(DEAL_SL));
      _set(POSITION_TP, deal._get<double>(DEAL_TP));
      deal._set(DEAL_POSITION_ID, ticket);
   }
   
   ~CustomPosition()
   {
      DisplayTrades();
   }
};

static long CustomPosition::ticket = 0;

//+------------------------------------------------------------------+
//| Global arrays of orders, deals, positions                        |
//+------------------------------------------------------------------+
AutoPtr<CustomOrder> orders[];
CustomOrder *selectedOrders[];
CustomOrder *selectedOrder = NULL;

AutoPtr<CustomDeal> deals[];
CustomDeal *selectedDeals[];
CustomDeal *selectedDeal = NULL;

AutoPtr<CustomPosition> positions[];
CustomPosition *selectedPosition = NULL;

//+------------------------------------------------------------------+
//| Simulated replacement functions for built-in trade functions     |
//| All functions have "MT5" prefix in their names                   |
//+------------------------------------------------------------------+

bool MT5HistorySelect(const datetime from_date, const datetime to_date)
{
   CheckOrders();
   
   ArrayResize(selectedOrders, 0);
   ArrayResize(selectedDeals, 0);
  
   for(int i = 0; i < ArraySize(orders); i++)
   {
      CustomOrder *ptr = orders[i][];
      if(!ptr.isActive())
      {
         if(ptr._get<long>(ORDER_TIME_SETUP) >= from_date
         || ptr._get<long>(ORDER_TIME_DONE) <= to_date)
         {
            PUSH(selectedOrders, ptr);
         }
      }
   }

   for(int i = 0; i < ArraySize(deals); i++)
   {
      CustomDeal *ptr = deals[i][];
      if(ptr._get<long>(DEAL_TIME) >= from_date
      || ptr._get<long>(DEAL_TIME) <= to_date)
      {
         PUSH(selectedDeals, ptr);
      }
   }
   return true;
}

bool MT5HistorySelectByPosition(long id)
{
   CheckOrders();
   
   ArrayResize(selectedOrders, 0);
   ArrayResize(selectedDeals, 0);
  
   for(int i = 0; i < ArraySize(orders); i++)
   {
      CustomOrder *ptr = orders[i][];
      if(!ptr.isActive())
      {
         if(ptr._get<long>(ORDER_POSITION_ID) == id)
         {
            PUSH(selectedOrders, ptr);
         }
      }
   }

   for(int i = 0; i < ArraySize(deals); i++)
   {
      CustomDeal *ptr = deals[i][];
      if(ptr._get<long>(DEAL_POSITION_ID) == id)
      {
         PUSH(selectedDeals, ptr);
      }
   }
   return true;
}

bool MT5OrderSend(const MqlTradeRequest &req, MqlTradeResult &result)
{
   MqlTradeRequest request = req;
   CustomOrder *order = NULL;
   CustomDeal *deal = NULL;
   CustomPosition *position = NULL;
   bool increasing = false;
   static uint reqid = 0;
   result.request_id = (uint)++reqid;
   result.retcode = 0;
   ResetLastError();

   if(request.action == TRADE_ACTION_DEAL || request.action == TRADE_ACTION_PENDING)
   {
      if(request.price == 0)
      {
         if(request.type == ORDER_TYPE_BUY)
         {
            ((MqlTradeRequest)request).price = SymbolInfoDouble(request.symbol, SYMBOL_ASK);
         }
         else
         if(request.type == ORDER_TYPE_SELL)
         {
            ((MqlTradeRequest)request).price = SymbolInfoDouble(request.symbol, SYMBOL_BID);
         }
      }
      
      if(request.action == TRADE_ACTION_DEAL)
      {
         if(AccountInfoInteger(ACCOUNT_MARGIN_MODE) != ACCOUNT_MARGIN_MODE_RETAIL_HEDGING
         && request.position == 0
         && MT5PositionSelect(request.symbol))
         {
            request.position = selectedPosition._get<long>(POSITION_TICKET);
         }
      }
    
      if(request.position > 0) // exit market or reverse or adding volume
      {
         int index = FindPositionIndex(request.position);
         if(index == -1)
         {
            SetUserError(TRADE_RETCODE_POSITION_CLOSED);
            result.retcode = TRADE_RETCODE_POSITION_CLOSED;
            return false;
         }
         CustomPosition *p = positions[index][];

         if(request.type == p._get<long>(POSITION_TYPE)) // adding volume (only netting)
         {
            if(AccountInfoInteger(ACCOUNT_MARGIN_MODE) == ACCOUNT_MARGIN_MODE_RETAIL_HEDGING)
            {
               SetUserError(TRADE_RETCODE_HEDGE_PROHIBITED);
               result.retcode = TRADE_RETCODE_HEDGE_PROHIBITED;
               return false;
            }
            increasing = true;
            selectedPosition = p;
         }
         else // exit market or reversing
         {
            const double v = p._get<double>(POSITION_VOLUME);
            deal = PositionClose(p, request.volume, request.price);
            if(!deal)
            {
               result.retcode = _LastError - ERR_USER_ERROR_FIRST;
            }
            else
            {
               if(TU::Equal(v, request.volume))
               {
                  Print("Close position: ", request.position);
                  ArrayRemove(positions, index, 1); // remove 'p' from 'positions'
               }
               order = (CustomOrder *)deal.order;
               result.retcode = TRADE_RETCODE_DONE;
               result.deal = deal._get<long>(DEAL_TICKET);
               result.order = order._get<long>(ORDER_TICKET);
               result.volume = request.volume;
               result.price = request.price;
            }
         }
      }
      
      if(request.position == 0 // enter market or set pending order
      || increasing)           // adding volume for netting
      {
         // TODO: check request.price
         order = new CustomOrder(request.type, request.volume, request.symbol);
         order._set(ORDER_PRICE_OPEN, request.price);
         order._set(ORDER_PRICE_CURRENT, request.price);
         order._set(ORDER_SL, request.sl);
         order._set(ORDER_TP, request.tp);
         order._set(ORDER_TIME_EXPIRATION, request.expiration);
         order._set(ORDER_TYPE_TIME, request.expiration ? ORDER_TIME_SPECIFIED : ORDER_TIME_GTC);

         result.retcode = TRADE_RETCODE_DONE;
         result.order = order._get<long>(ORDER_TICKET);
         result.ask = SymbolInfoDouble(request.symbol, SYMBOL_ASK);
         result.bid = SymbolInfoDouble(request.symbol, SYMBOL_BID);
         result.volume = request.volume;
         result.price = request.price;

         if(request.action == TRADE_ACTION_DEAL)
         {
            deal = new CustomDeal(order, DEAL_ENTRY_IN);
            deal._set(DEAL_SL, order._get<double>(ORDER_SL));
            deal._set(DEAL_TP, order._get<double>(ORDER_TP));
            
            if(!increasing)
            {
               position = new CustomPosition(deal);
               order._set(ORDER_POSITION_ID, position._get<long>(POSITION_IDENTIFIER));
            }
            else
            {
               Increase(selectedPosition, request.volume, request.price);
               selectedPosition._set(POSITION_SL, order._get<double>(ORDER_SL));
               selectedPosition._set(POSITION_TP, order._get<double>(ORDER_TP));
               order._set(ORDER_POSITION_ID, selectedPosition._get<long>(POSITION_IDENTIFIER));
            }
            deal._set(DEAL_POSITION_ID, order._get<long>(ORDER_POSITION_ID));
            result.deal = deal._get<long>(DEAL_TICKET);
         }
      }
   }
   /*
   else if(request.action == TRADE_ACTION_SLTP)
   {
      // TODO: change opened position
   }
   else if(request.action == TRADE_ACTION_MODIFY)
   {
      // TODO: change pending order
   }
   else if(request.action == TRADE_ACTION_REMOVE)
   {
      // TODO: delete pending order
   }
   else if(request.action == TRADE_ACTION_CLOSE_BY)
   {
      // TODO: close 2 positions
   }
   */

   if(order == NULL) return false;
   
   PUSH(orders, order);

   if(deal != NULL)
   {
      PUSH(deals, deal);
   }

   if(position != NULL)
   {
      PUSH(positions, position);
   }

   return true;
}

long MT5PositionGetInteger(ENUM_POSITION_PROPERTY_INTEGER property_id)
{
  long temp;
  if(MT5PositionGetInteger(property_id, temp))
  {
    return temp;
  }
  return 0;
}

bool MT5PositionGetInteger(ENUM_POSITION_PROPERTY_INTEGER property_id, long &long_var)
{
  if(CheckPointer(selectedPosition) == POINTER_INVALID) return false;
  long_var = selectedPosition._get<long>(property_id);
  return true;
}

double MT5PositionGetDouble(ENUM_POSITION_PROPERTY_DOUBLE property_id)
{
  double temp;
  if(MT5PositionGetDouble(property_id, temp))
  {
    return temp;
  }
  return 0;
}

bool MT5PositionGetDouble(ENUM_POSITION_PROPERTY_DOUBLE property_id, double &double_var)
{
  if(CheckPointer(selectedPosition) == POINTER_INVALID) return false;
  double_var = selectedPosition._get<double>(property_id);
  return true;
}

string MT5PositionGetString(ENUM_POSITION_PROPERTY_STRING property_id)
{
  string temp;
  if(MT5PositionGetString(property_id, temp))
  {
    return temp;
  }
  return "";
}

bool MT5PositionGetString(ENUM_POSITION_PROPERTY_STRING property_id, string &string_var)
{
  if(CheckPointer(selectedPosition) == POINTER_INVALID) return false;
  string_var = selectedPosition._get<string>(property_id);
  return true;
}

bool MT5PositionSelect(string symbol)
{
   selectedPosition = NULL;
   for(int i = 0; i < ArraySize(positions); i++)
   {
      CustomPosition *p = positions[i][];
      if(p._get<string>(POSITION_SYMBOL) == symbol)
      {
         selectedPosition = p;
         UpdatePosition(p);
         return true;
      }
   }
   return false;
}

bool MT5PositionSelectByTicket(ulong ticket)
{
   selectedPosition = NULL;
   for(int i = 0; i < ArraySize(positions); i++)
   {
      CustomPosition *p = positions[i][];
      if(p._get<long>(POSITION_TICKET) == ticket)
      {
         selectedPosition = p;
         UpdatePosition(p);
         return true;
      }
   }
   return false;
}

int MT5PositionsTotal()
{
   CheckOrders();
   return ArraySize(positions);
}

int MT5OrdersTotal()
{
   CheckOrders();
   return ArraySize(orders) - (int)CustomOrder::getDoneCount();
}

string MT5PositionGetSymbol(int index)
{
   selectedPosition = NULL;
   if(index < 0 || index >= ArraySize(positions)) return NULL;
   
   CustomPosition *p = positions[index][];
   selectedPosition = p;
   UpdatePosition(p);
   return p._get<string>(POSITION_SYMBOL);
}

ulong MT5PositionGetTicket(int index)
{
   selectedPosition = NULL;
   if(index < 0 || index >= ArraySize(positions)) return 0;

   CustomPosition *p = positions[index][];
   selectedPosition = p;
   UpdatePosition(p);
   return p._get<long>(POSITION_TICKET);
}

int MT5HistoryDealsTotal()
{
   return ArraySize(selectedDeals);
}

int MT5HistoryOrdersTotal()
{
   return ArraySize(selectedOrders);
}

ulong MT5HistoryDealGetTicket(int index)
{
   if(index < 0 || index >= ArraySize(selectedDeals)) return 0;
   selectedDeal = selectedDeals[index];
   return selectedDeal._get<long>(DEAL_TICKET);
}

ulong MT5HistoryOrderGetTicket(int index)
{
   if(index < 0 || index >= ArraySize(selectedOrders)) return 0;
   selectedOrder = selectedOrders[index];
   return selectedOrder._get<long>(ORDER_TICKET);
}

long MT5HistoryDealGetInteger(ulong ticket, ENUM_DEAL_PROPERTY_INTEGER property_id)
{
  long temp;
  if(MT5HistoryDealGetInteger(ticket, property_id, temp))
  {
    return temp;
  }
  return 0;
}

bool MT5HistoryDealGetInteger(ulong ticket, ENUM_DEAL_PROPERTY_INTEGER property_id, long &long_var)
{
   if(CheckPointer(selectedDeal) != POINTER_INVALID
   && selectedDeal._get<long>(DEAL_TICKET) == ticket)
   {
      selectedDeal._get(property_id, long_var);
      return true;
   }
   return false;
}

double MT5HistoryDealGetDouble(ulong ticket, ENUM_DEAL_PROPERTY_DOUBLE property_id)
{
  double temp;
  if(MT5HistoryDealGetDouble(ticket, property_id, temp))
  {
    return temp;
  }
  return 0;
}

bool MT5HistoryDealGetDouble(ulong ticket, ENUM_DEAL_PROPERTY_DOUBLE property_id, double &double_var)
{
   if(CheckPointer(selectedDeal) != POINTER_INVALID
   && selectedDeal._get<long>(DEAL_TICKET) == ticket)
   {
      selectedDeal._get(property_id, double_var);
      return true;
   }
   return false;
}

string MT5HistoryDealGetString(ulong ticket, ENUM_DEAL_PROPERTY_STRING property_id)
{
  string temp;
  if(MT5HistoryDealGetString(ticket, property_id, temp))
  {
    return temp;
  }
  return "";
}

bool MT5HistoryDealGetString(ulong ticket, ENUM_DEAL_PROPERTY_STRING property_id, string &string_var)
{
   if(CheckPointer(selectedDeal) != POINTER_INVALID
   && selectedDeal._get<long>(DEAL_TICKET) == ticket)
   {
      selectedDeal._get(property_id, string_var);
      return true;
   }
   return false;
}

double MT5HistoryOrderGetDouble(ulong ticket, ENUM_ORDER_PROPERTY_DOUBLE property_id)
{
  double temp;
  if(MT5HistoryOrderGetDouble(ticket, property_id, temp))
  {
    return temp;
  }
  return 0;
}

bool MT5HistoryOrderGetDouble(ulong ticket, ENUM_ORDER_PROPERTY_DOUBLE property_id, double &double_var)
{
   if(CheckPointer(selectedOrder) != POINTER_INVALID
   && selectedOrder._get<long>(ORDER_TICKET) == ticket)
   {
      if(property_id == ORDER_PRICE_CURRENT)
      {
         double_var = SymbolInfoDouble(selectedOrder._get<string>(ORDER_SYMBOL),
            (selectedOrder._get<long>(ORDER_TYPE) % 2) == 0 ? SYMBOL_BID : SYMBOL_ASK);
      }
      else
      {
         selectedOrder._get(property_id, double_var);
      }
      return true;
   }
   return false;
}

long MT5HistoryOrderGetInteger(ulong ticket, ENUM_ORDER_PROPERTY_INTEGER property_id)
{
  long temp = 0;
  if(MT5HistoryOrderGetInteger(ticket, property_id, temp))
  {
    return temp;
  }
  return 0;
}

bool MT5HistoryOrderGetInteger(ulong ticket, ENUM_ORDER_PROPERTY_INTEGER property_id, long &long_var)
{
   if(CheckPointer(selectedOrder) != POINTER_INVALID
   && selectedOrder._get<long>(ORDER_TICKET) == ticket)
   {
      selectedOrder._get(property_id, long_var);
      return true;
   }
   return false;
}

string MT5HistoryOrderGetString(ulong ticket, ENUM_ORDER_PROPERTY_STRING property_id)
{
  string temp;
  if(MT5HistoryOrderGetString(ticket, property_id, temp))
  {
    return temp;
  }
  return "";
}

bool MT5HistoryOrderGetString(ulong ticket, ENUM_ORDER_PROPERTY_STRING property_id, string &string_var)
{
   if(CheckPointer(selectedOrder) != POINTER_INVALID
   && selectedOrder._get<long>(ORDER_TICKET) == ticket)
   {
      selectedOrder._get(property_id, string_var);
      return true;
   }
   return false;
}

bool MT5OrderSelect(ulong ticket)
{
   for(int i = 0; i < ArraySize(orders); ++i)
   {
      CustomOrder *o = orders[i][];
      if(o.isActive() && o._get<long>(ORDER_TICKET) == ticket)
      {
         selectedOrder = o;
         return true;
      }
   }
   return false;
}

bool MT5HistoryOrderSelect(ulong ticket)
{
   ArrayResize(selectedOrders, 0);
   for(int i = 0; i < ArraySize(orders); ++i)
   {
      CustomOrder *o = orders[i][];
      if(!o.isActive() && o._get<long>(ORDER_TICKET) == ticket)
      {
         PUSH(selectedOrders, o);
         selectedOrder = o;
         return true;
      }
   }
   return false;
}

bool MT5HistoryDealSelect(ulong ticket)
{
   ArrayResize(selectedDeals, 0);
   for(int i = 0; i < ArraySize(deals); ++i)
   {
      CustomDeal *d = deals[i][];
      if(d._get<long>(DEAL_TICKET) == ticket)
      {
         PUSH(selectedDeals, d);
         selectedDeal = d;
         return true;
      }
   }
   return false;
}

datetime MT5TimeCurrent()
{
   return (datetime)SymbolInfoInteger(_Symbol, SYMBOL_TIME);
}

//+------------------------------------------------------------------+
//| Main custom functions for updating and exposing trade state      |
//+------------------------------------------------------------------+

void CheckPositions()
{
   for(int i = ArraySize(positions) - 1; i >= 0; --i)
   {
      UpdatePosition(positions[i][]);
   }
}

void UpdatePosition(CustomPosition *position)
{
   if(CheckPointer(position) == POINTER_INVALID) return;
   
   const ENUM_POSITION_TYPE type = (ENUM_POSITION_TYPE)position._get<long>(POSITION_TYPE);
   const string symbol = position._get<string>(POSITION_SYMBOL);
   const double price = SymbolInfoDouble(symbol,
      type == POSITION_TYPE_BUY ? SYMBOL_BID : SYMBOL_ASK);
   position._set(POSITION_PRICE_CURRENT, price);
      
   double profit = (price - position._get<double>(POSITION_PRICE_OPEN))
      / SymbolInfoDouble(symbol, SYMBOL_POINT)
      * position._get<double>(POSITION_VOLUME) * MPM::PointValue(symbol);
   if(type == POSITION_TYPE_SELL)
   {
      profit *= -1;
   }
   
   position._set(POSITION_PROFIT, profit);

   const double swap = SymbolInfoDouble(symbol,
      type == POSITION_TYPE_BUY ? SYMBOL_SWAP_LONG : SYMBOL_SWAP_SHORT);

   ENUM_SYMBOL_INFO_DOUBLE swaps[7] = {SYMBOL_SWAP_SUNDAY, SYMBOL_SWAP_MONDAY, SYMBOL_SWAP_TUESDAY, SYMBOL_SWAP_WEDNESDAY, SYMBOL_SWAP_THURSDAY, SYMBOL_SWAP_FRIDAY, SYMBOL_SWAP_SATURDAY};
   double total = 0;
   datetime start = (datetime)((position._get<long>(POSITION_TIME) / DAY_LONG + 1) * DAY_LONG);
   for( ; start < SymbolInfoInteger(symbol, SYMBOL_TIME); start += DAY_LONG)
   {
      MqlDateTime day;
      TimeToStruct(start, day);
      total += SymbolInfoDouble(symbol, swaps[day.day_of_week]) * swap;
   }
   // TODO: utilize SYMBOL_SWAP_MODE, for example,
   // if SYMBOL_SWAP_MODE_POINTS used, MPM::PointValue(symbol) * swap * lot
   // NB: this calculation may be tricky because position
   // may change volume during lifetime
   position._set(POSITION_SWAP, total);
   
   CustomDeal *deal = NULL;

   if(type == POSITION_TYPE_BUY)
   {
      if(price <= position._get<double>(POSITION_SL))
      {
         deal = PositionClose(position, position._get<double>(POSITION_VOLUME), price, "[sl]");
      }
      else if(position._get<double>(POSITION_TP) > 0.0 && price >= position._get<double>(POSITION_TP))
      {
         deal = PositionClose(position, position._get<double>(POSITION_VOLUME), price, "[tp]");
      }
   }
   else
   {
      if(position._get<double>(POSITION_SL) != 0.0 && price >= position._get<double>(POSITION_SL))
      {
         deal = PositionClose(position, position._get<double>(POSITION_VOLUME), price, "[sl]");
      }
      else if(price <= position._get<double>(POSITION_TP))
      {
         deal = PositionClose(position, position._get<double>(POSITION_VOLUME), price, "[tp]");
      }
   }
   if(deal)
   {
      if(deal.order)
      {
         PUSH(orders, deal.order);
      }
      PUSH(deals, deal);
      
      int index = FindPositionIndex(position._get<long>(POSITION_TICKET));
      Print("SL/TP position: ", position._get<long>(POSITION_TICKET));
      if(index != -1)
      {
         ArrayRemove(positions, index, 1);
      }
   }
}

CustomDeal *PositionClose(CustomPosition *position, const double volume, const double price,
   const string comment = NULL, CustomOrder *order = NULL, CustomDeal *deal = NULL)
{
   // TODO: check price
   if(volume > position._get<double>(POSITION_VOLUME)) // this is turn around (netting only)
   {
      if(AccountInfoInteger(ACCOUNT_MARGIN_MODE) == ACCOUNT_MARGIN_MODE_RETAIL_HEDGING)
      {
         SetUserError(TRADE_RETCODE_INVALID_CLOSE_VOLUME);
         return NULL;
      }
   }

   ENUM_ORDER_TYPE type = (ENUM_ORDER_TYPE)!position._get<long>(POSITION_TYPE);
   const string symbol = position._get<string>(POSITION_SYMBOL);

   if(!order) order = new CustomOrder(type, volume, symbol);
   order._set(ORDER_POSITION_ID, position._get<long>(POSITION_IDENTIFIER));
   order._set(ORDER_PRICE_OPEN, price);
   order._set(ORDER_PRICE_CURRENT, price);

   double remain = position._get<double>(POSITION_VOLUME) - volume;
   double actual = fmin(volume, position._get<double>(POSITION_VOLUME));
   remain = NormalizeDouble(remain, -(int)MathLog10(SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP)));
   
   double profit = (price - position._get<double>(POSITION_PRICE_OPEN)) / SymbolInfoDouble(symbol, SYMBOL_POINT)
      * actual * MPM::PointValue(symbol);
   
   if(position._get<long>(POSITION_TYPE) == POSITION_TYPE_SELL)
   {
      profit *= -1;
   }

   position._set(POSITION_PRICE_CURRENT, price);
   if(remain > 0) // partial close
   {
      Print("partial close ", remain);
      position._set(POSITION_VOLUME, remain);
      position._set(POSITION_PROFIT, profit * remain / volume);
   }
   else if(remain < 0) // reversal
   {
      Print("reversal ", remain);
      position._set(POSITION_VOLUME, -remain);
      position._set(POSITION_TICKET, order._get<long>(ORDER_TICKET));
      position._set(POSITION_PROFIT, 0);
      position._set(POSITION_TYPE, (ENUM_POSITION_TYPE)type);
      position._set(POSITION_PRICE_OPEN, price);
   }
   else // complete close
   {
      position._set(POSITION_PROFIT, profit);
   }
   
   if(!deal) deal = new CustomDeal(order, DEAL_ENTRY_OUT);
   deal._set(DEAL_PROFIT, profit);
   deal._set(DEAL_SWAP, position._get<double>(POSITION_SWAP));
   deal._set(DEAL_COMMENT, comment);
   
   deal._set(DEAL_SL, position._get<double>(POSITION_SL));
   deal._set(DEAL_TP, position._get<double>(POSITION_TP));

   position._set(POSITION_COMMENT, comment);
   position._set(POSITION_SWAP, 0);
   
   return deal;
}

void Increase(CustomPosition *position, const double volume, const double price)
{
   double average = (position._get<double>(POSITION_PRICE_OPEN) * position._get<double>(POSITION_VOLUME)
      + price * volume) / (position._get<double>(POSITION_VOLUME) + volume);
   
   position._set(POSITION_PRICE_OPEN, average);
   position._set(POSITION_VOLUME, position._get<double>(POSITION_VOLUME) + volume);
}

int FindPositionIndex(ulong ticket)
{
   for(int i = 0; i < ArraySize(positions); i++)
   {
      if(positions[i][]._get<long>(POSITION_TICKET) == ticket)
      {
         return i;
      }
   }
   return -1;
}

void MakeDeal(CustomOrder *order)
{
   CustomDeal *deal = new CustomDeal(order, DEAL_ENTRY_IN);
   deal._set(DEAL_SL, order._get<double>(ORDER_SL));
   deal._set(DEAL_TP, order._get<double>(ORDER_TP));
   
   CustomPosition *position = NULL;
   if(AccountInfoInteger(ACCOUNT_MARGIN_MODE) == ACCOUNT_MARGIN_MODE_RETAIL_HEDGING
     || !MT5PositionSelect(order._get<string>(ORDER_SYMBOL)))
   {
      position = new CustomPosition(deal);
      PUSH(positions, position);
      order._set(ORDER_POSITION_ID, position._get<long>(POSITION_IDENTIFIER));
      deal._set(DEAL_POSITION_ID, position._get<long>(POSITION_IDENTIFIER));
   }
   else // netting and selected
   {
      position = selectedPosition;
      order._set(ORDER_POSITION_ID, position._get<long>(POSITION_IDENTIFIER));
      deal._set(DEAL_POSITION_ID, position._get<long>(POSITION_IDENTIFIER));
      if(order._get<long>(ORDER_TYPE) % 2 == position._get<long>(POSITION_TYPE))
      {
         // added volume
         Increase(position, order._get<double>(ORDER_VOLUME_INITIAL), order._get<double>(ORDER_PRICE_OPEN));
      }
      else // closure, partial closure or reversal
      {
         deal._set(DEAL_ENTRY, DEAL_ENTRY_OUT);
         const bool complete = TU::Equal(position._get<double>(POSITION_VOLUME), order._get<double>(ORDER_VOLUME_INITIAL));
         PositionClose(position, order._get<double>(ORDER_VOLUME_INITIAL), position._get<double>(POSITION_PRICE_CURRENT),
            NULL, order, deal);
         if(complete)
         {
            int index = FindPositionIndex(position._get<long>(POSITION_TICKET));
            if(index != -1)
            {
               ArrayRemove(positions, index, 1);
            }
         }
      }
   }
   order.setDone(ORDER_STATE_FILLED);
   PUSH(deals, deal);
}

void CheckOrders()
{
   for(int i = 0; i < ArraySize(orders); ++i)
   {
      CustomOrder *o = orders[i][];
      if(o.isActive())
      {
         const string symbol = o._get<string>(ORDER_SYMBOL);
         const datetime expiration = (datetime)o._get<long>(ORDER_TIME_EXPIRATION);
         if(expiration && expiration > SymbolInfoInteger(symbol, SYMBOL_TIME))
         {
            o.setDone(ORDER_STATE_EXPIRED);
         }
         else
         switch((ENUM_ORDER_TYPE)o._get<long>(ORDER_TYPE))
         {
         case ORDER_TYPE_BUY_LIMIT:
            if(SymbolInfoDouble(symbol, SYMBOL_ASK) <= o._get<double>(ORDER_PRICE_OPEN))
            {
               Print("Buy limit triggered: ", o._get<long>(ORDER_TICKET));
               o._set(ORDER_COMMENT, "[buy limit]");
               MakeDeal(o);
            }
            break;
         case ORDER_TYPE_SELL_LIMIT:
            if(SymbolInfoDouble(symbol, SYMBOL_BID) >= o._get<double>(ORDER_PRICE_OPEN))
            {
               Print("Sell limit triggered: ", o._get<long>(ORDER_TICKET));
               o._set(ORDER_COMMENT, "[sell limit]");
               MakeDeal(o);
            }
            break;
         case ORDER_TYPE_BUY_STOP:
            if(SymbolInfoDouble(symbol, SYMBOL_ASK) >= o._get<double>(ORDER_PRICE_OPEN))
            {
               Print("Buy stop triggered: ", o._get<long>(ORDER_TICKET));
               o._set(ORDER_COMMENT, "[buy stop]");
               MakeDeal(o);
            }
            break;
         case ORDER_TYPE_SELL_STOP:
            if(SymbolInfoDouble(symbol, SYMBOL_BID) <= o._get<double>(ORDER_PRICE_OPEN))
            {
               Print("Sell stop triggered: ", o._get<long>(ORDER_TICKET));
               o._set(ORDER_COMMENT, "[sell stop]");
               MakeDeal(o);
            }
            break;
         }
      }
   }
}

void DisplayTrades()
{
   static const color colors[] = {clrBlue, clrRed};
   for(int i = 0; i < ArraySize(positions); ++i)
   {
      const long ticket = positions[i][]._get<long>(POSITION_TICKET);
      const string name = "pos" + (string)ticket;
      ObjectCreate(0, name, OBJ_ARROWED_LINE, 0,
         positions[i][]._get<long>(POSITION_TIME), positions[i][]._get<double>(POSITION_PRICE_OPEN));
      ObjectSetInteger(0, name, OBJPROP_TIME, 1, SymbolInfoInteger(_Symbol, SYMBOL_TIME));
      ObjectSetDouble(0, name, OBJPROP_PRICE, 1, positions[i][]._get<double>(POSITION_PRICE_CURRENT));
      
      ObjectSetInteger(0, name, OBJPROP_STYLE, STYLE_SOLID);
      ObjectSetInteger(0, name, OBJPROP_COLOR, colors[(int)positions[i][]._get<long>(POSITION_TYPE)]);
      ObjectSetString(0, name, OBJPROP_TEXT, StringFormat("#%lld %.2f",
         ticket, positions[i][]._get<double>(POSITION_PROFIT)));
         
      if(positions[i][]._get<double>(POSITION_SL))
      {
         const string sl = "sl" + (string)ticket;
         ObjectCreate(0, sl, OBJ_HLINE, 0,
            0, positions[i][]._get<double>(POSITION_SL));
         ObjectSetInteger(0, sl, OBJPROP_ZORDER, ticket);
         ObjectSetInteger(0, sl, OBJPROP_COLOR, clrRed);
         ObjectSetInteger(0, sl, OBJPROP_STYLE, STYLE_DOT);
         ObjectSetString(0, sl, OBJPROP_TEXT, StringFormat("#%lld SL", ticket));
      }

      if(positions[i][]._get<double>(POSITION_TP))
      {
         const string tp = "tp" + (string)ticket;
         ObjectCreate(0, tp, OBJ_HLINE, 0,
            0, positions[i][]._get<double>(POSITION_TP));
         ObjectSetInteger(0, tp, OBJPROP_ZORDER, ticket);
         ObjectSetInteger(0, tp, OBJPROP_COLOR, clrBlue);
         ObjectSetInteger(0, tp, OBJPROP_STYLE, STYLE_DOT);
         ObjectSetString(0, tp, OBJPROP_TEXT, StringFormat("#%lld TP", ticket));
      }
   }
   
   for(int i = ObjectsTotal(0, 0, OBJ_HLINE) - 1; i >= 0; --i)
   {
      const string name = ObjectName(0, i, 0, OBJ_HLINE);
      const long ticket = ObjectGetInteger(0, name, OBJPROP_ZORDER);
      if(ticket != 0 && FindPositionIndex(ticket) == -1)
      {
         ObjectDelete(0, name);
         ObjectSetInteger(0, "pos" + (string)ticket, OBJPROP_STYLE, STYLE_DOT);
      }
   }

   for(int i = 0; i < ArraySize(orders); ++i)
   {
      const string name = "pen" + (string)orders[i][]._get<long>(ORDER_TICKET);
      if(orders[i][].isActive())
      {
         ObjectCreate(0, name,
            OBJ_HLINE, 0,
            0, orders[i][]._get<double>(ORDER_PRICE_OPEN));
         
         ObjectSetInteger(0, name, OBJPROP_COLOR, colors[(int)orders[i][]._get<long>(ORDER_TYPE) % 2]);
         ObjectSetInteger(0, name, OBJPROP_STYLE, STYLE_DASHDOT);
         ObjectSetString(0, name, OBJPROP_TEXT, StringFormat("#%lld",
            orders[i][]._get<long>(ORDER_TICKET)));
      }
      else
      {
         ObjectDelete(0, name);
      }
   }
}

string ReportTradeState()
{
   CheckPositions();
   CheckOrders();
   
   static const string types[] = {"BUY ", "SELL"};
   string result = "Positions:" + (string)ArraySize(positions) + "\n";
   double total = 0;
   for(int i = 0; i < ArraySize(positions); ++i)
   {
      const string symbol = positions[i][]._get<string>(POSITION_SYMBOL);
      int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
      int dots = (int)fmax(0, -log10(SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP)));
      result += StringFormat("#%lld %s %s %.*f %.*f -> %.*f = %.2f\n",
         positions[i][]._get<long>(POSITION_TICKET),
         TimeToString(positions[i][]._get<long>(POSITION_TIME), TIME_DATE | TIME_SECONDS),
         types[(int)positions[i][]._get<long>(POSITION_TYPE)],
         dots, positions[i][]._get<double>(POSITION_VOLUME),
         digits, positions[i][]._get<double>(POSITION_PRICE_OPEN),
         digits, positions[i][]._get<double>(POSITION_PRICE_CURRENT),
         positions[i][]._get<double>(POSITION_PROFIT));
      total += positions[i][]._get<double>(POSITION_PROFIT);
   }
   result += "Total: " + StringFormat("%.2f\n", total);
   result += "Orders:" + (string)(ArraySize(orders) - CustomOrder::getDoneCount()) + "\n";
   for(int i = 0; i < ArraySize(orders); ++i)
   {
      if(orders[i][].isActive())
      {
         const string symbol = orders[i][]._get<string>(ORDER_SYMBOL);
         int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
         int dots = (int)fmax(0, -log10(SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP)));
         result += StringFormat("#%lld %s %s %.*f %.*f\n",
            orders[i][]._get<long>(ORDER_TICKET),
            EnumToString((ENUM_ORDER_TYPE)orders[i][]._get<long>(ORDER_TYPE)),
            TimeToString(orders[i][]._get<long>(ORDER_TIME_SETUP), TIME_DATE | TIME_SECONDS),
            dots, orders[i][]._get<double>(ORDER_VOLUME_INITIAL),
            digits, orders[i][]._get<double>(ORDER_PRICE_OPEN));
      }
   }
   return result;
}

#define STRNULL(S) (S == NULL ? "" : S)

void PrintTradeHistory()
{
   Print("History Orders:");
   for(int i = 0; i < ArraySize(orders); ++i)
   {
      if(!orders[i][].isActive())
      {
         PrintFormat("(%lld) #%lld %s %s -> %s L=%g @ %g %s",
            orders[i][]._get<long>(ORDER_POSITION_ID),
            orders[i][]._get<long>(ORDER_TICKET),
            EnumToString((ENUM_ORDER_TYPE)orders[i][]._get<long>(ORDER_TYPE)),
            TimeToString(orders[i][]._get<long>(ORDER_TIME_SETUP), TIME_DATE | TIME_SECONDS),
            TimeToString(orders[i][]._get<long>(ORDER_TIME_DONE), TIME_DATE | TIME_SECONDS),
            orders[i][]._get<double>(ORDER_VOLUME_INITIAL),
            orders[i][]._get<double>(ORDER_PRICE_OPEN),
            STRNULL(orders[i][]._get<string>(ORDER_COMMENT)));
      }
   }
   double total = 0;
   int trades = 0;
   Print("Deals:");
   for(int i = 0; i < ArraySize(deals); ++i)
   {
      PrintFormat("(%lld) #%lld [#%lld] %s %s %s L=%g @ %g = %.2f %s",
         deals[i][]._get<long>(DEAL_POSITION_ID),
         deals[i][]._get<long>(DEAL_TICKET), deals[i][]._get<long>(DEAL_ORDER),
         EnumToString((ENUM_DEAL_TYPE)deals[i][]._get<long>(DEAL_TYPE)),
         EnumToString((ENUM_DEAL_ENTRY)deals[i][]._get<long>(DEAL_ENTRY)),
         TimeToString(deals[i][]._get<long>(DEAL_TIME), TIME_DATE | TIME_SECONDS),
         deals[i][]._get<double>(DEAL_VOLUME),
         deals[i][]._get<double>(DEAL_PRICE),
         deals[i][]._get<double>(DEAL_PROFIT),
         STRNULL(deals[i][]._get<string>(DEAL_COMMENT)));
         
      if(deals[i][]._get<long>(DEAL_ENTRY) != DEAL_ENTRY_IN)
      {
         ++trades;
      }
      total += deals[i][]._get<double>(DEAL_PROFIT); /* TODO: + deals[i][]._get<double>(DEAL_SWAP) + etc */
   }
   PrintFormat("Total: %.2f, Trades: %d", total, trades);
}

} // end of the namespace CustomTrade
//+------------------------------------------------------------------+
