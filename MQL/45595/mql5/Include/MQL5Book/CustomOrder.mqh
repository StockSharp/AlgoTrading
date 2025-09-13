//+------------------------------------------------------------------+
//|                                                  CustomOrder.mqh |
//|                               Copyright (c) 2020-2022, Marketeer |
//|                           https://www.mql5.com/ru/articles/8226/ |
//+------------------------------------------------------------------+
#include <MQL5Book/TradeUtils.mqh>

//+------------------------------------------------------------------+
//| Replace custom symbol with work symbol (and vice versa)          |
//| in built-in trade structs and functions                          |
//+------------------------------------------------------------------+
class CustomOrder
{
private:
   static string workSymbol;
    
   static void replaceRequest(MqlTradeRequest &request)
   {
      if(request.symbol == _Symbol && workSymbol != NULL)
      {
         request.symbol = workSymbol;
         if(MQLInfoInteger(MQL_TESTER)
            && (request.type == ORDER_TYPE_BUY
            || request.type == ORDER_TYPE_SELL))
         {
            if(TU::Equal(request.price, SymbolInfoDouble(_Symbol, SYMBOL_ASK)))
               request.price = SymbolInfoDouble(workSymbol, SYMBOL_ASK);
            if(TU::Equal(request.price, SymbolInfoDouble(_Symbol, SYMBOL_BID)))
               request.price = SymbolInfoDouble(workSymbol, SYMBOL_BID);
         }
      }
   }
    
public:
   static void setReplacementSymbol(const string replacementSymbol)
   {
      workSymbol = replacementSymbol;
   }
    
   static bool OrderSend(MqlTradeRequest &request, MqlTradeResult &result)
   {
      replaceRequest(request);
      return ::OrderSend(request, result);
   }
    
   static bool OrderSendAsync(MqlTradeRequest &request, MqlTradeResult &result)
   {
      replaceRequest(request);
      return ::OrderSendAsync(request, result);
   }

   static bool OrderCheck(MqlTradeRequest &request, MqlTradeCheckResult &result)
   {
      replaceRequest(request);
      return ::OrderCheck(request, result);
   }
    
   static bool OrderCalcMargin(ENUM_ORDER_TYPE action, string symbol, double volume, double price, double &margin)
   {
      if(symbol == _Symbol && workSymbol != NULL)
      {
        symbol = workSymbol;
      }
      return ::OrderCalcMargin(action, symbol, volume, price, margin);
   }
    
   static bool OrderCalcProfit(ENUM_ORDER_TYPE action, string symbol, double volume, double price_open, double price_close, double &profit)
   {
      if(symbol == _Symbol && workSymbol != NULL)
      {
        symbol = workSymbol;
      }
      return ::OrderCalcProfit(action, symbol, volume, price_open, price_close, profit);
   }
    
   static string PositionGetString(ENUM_POSITION_PROPERTY_STRING property_id)
   {
      const string result = ::PositionGetString(property_id);
      if(property_id == POSITION_SYMBOL && result == workSymbol) return _Symbol;
      return result;
   }
    
   static bool PositionGetString(ENUM_POSITION_PROPERTY_STRING property_id, string &var)
   {
      const bool result = ::PositionGetString(property_id, var);
      if(property_id == POSITION_SYMBOL && var == workSymbol) var = _Symbol;
      return result;
   }
    
   static string OrderGetString(ENUM_ORDER_PROPERTY_STRING property_id)
   {
      const string result = ::OrderGetString(property_id);
      if(property_id == ORDER_SYMBOL && result == workSymbol) return _Symbol;
      return result;
   }
    
   static bool OrderGetString(ENUM_ORDER_PROPERTY_STRING property_id, string &var)
   {
      const bool result = ::OrderGetString(property_id, var);
      if(property_id == ORDER_SYMBOL && var == workSymbol) var = _Symbol;
      return result;
   }
    
   static string HistoryOrderGetString(ulong ticket_number, ENUM_ORDER_PROPERTY_STRING property_id)
   {
      const string result = ::HistoryOrderGetString(ticket_number, property_id);
      if(property_id == ORDER_SYMBOL && result == workSymbol) return _Symbol;
      return result;
   }
    
   static bool HistoryOrderGetString(ulong ticket_number, ENUM_ORDER_PROPERTY_STRING property_id, string &var)
   {
      const bool result = ::HistoryOrderGetString(ticket_number, property_id, var);
      if(property_id == ORDER_SYMBOL && var == workSymbol) var = _Symbol;
      return result;
   }
    
   static string HistoryDealGetString(ulong ticket_number, ENUM_DEAL_PROPERTY_STRING property_id)
   {
      const string result = ::HistoryDealGetString(ticket_number, property_id);
      if(property_id == DEAL_SYMBOL && result == workSymbol) return _Symbol;
      return result;
   }
    
   static bool HistoryDealGetString(ulong ticket_number, ENUM_DEAL_PROPERTY_STRING property_id, string &var)
   {
      const bool result = ::HistoryDealGetString(ticket_number, property_id, var);
      if(property_id == DEAL_SYMBOL && var == workSymbol) var = _Symbol;
      return result;
   }
    
   static bool PositionSelect(string symbol)
   {
      if(symbol == _Symbol && workSymbol != NULL) return ::PositionSelect(workSymbol);
      return ::PositionSelect(symbol);
   }
    
   static string PositionGetSymbol(int index)
   {
      const string result = ::PositionGetSymbol(index);
      if(result == workSymbol) return _Symbol;
      return result;
   }
};

static string CustomOrder::workSymbol = NULL;

//+------------------------------------------------------------------+
//| Global functions for MQL5 API substitution                       |
//+------------------------------------------------------------------+
bool CustomOrderSend(const MqlTradeRequest &request, MqlTradeResult &result)
{
   return CustomOrder::OrderSend((MqlTradeRequest)request, result);
}

bool CustomOrderSendAsync(const MqlTradeRequest &request, MqlTradeResult &result)
{
   return CustomOrder::OrderSendAsync((MqlTradeRequest)request, result);
}

bool CustomOrderCheck(const MqlTradeRequest &request, MqlTradeCheckResult &result)
{
   return CustomOrder::OrderCheck((MqlTradeRequest)request, result);
}

bool CustomOrderCalcMargin(ENUM_ORDER_TYPE action, string symbol, double volume, double price, double &margin)
{
   return CustomOrder::OrderCalcMargin(action, symbol, volume, price, margin);
}

bool CustomOrderCalcProfit(ENUM_ORDER_TYPE action, string symbol, double volume, double price_open, double price_close, double &profit)
{
   return CustomOrder::OrderCalcProfit(action, symbol, volume, price_open, price_close, profit);
}

bool CustomPositionSelect(string symbol)
{
   return CustomOrder::PositionSelect(symbol);
}

string CustomPositionGetSymbol(int index)
{
   return CustomOrder::PositionGetSymbol(index);
}

string CustomPositionGetString(ENUM_POSITION_PROPERTY_STRING property_id)
{
  return CustomOrder::PositionGetString(property_id);
}

bool CustomPositionGetString(ENUM_POSITION_PROPERTY_STRING property_id, string &var)
{
   return CustomOrder::PositionGetString(property_id, var);
}

string CustomOrderGetString(ENUM_ORDER_PROPERTY_STRING property_id)
{
  return CustomOrder::OrderGetString(property_id);
}

bool CustomOrderGetString(ENUM_ORDER_PROPERTY_STRING property_id, string &var)
{
   return CustomOrder::OrderGetString(property_id, var);
}

string CustomHistoryOrderGetString(ulong ticket_number, ENUM_ORDER_PROPERTY_STRING property_id)
{
   return CustomOrder::HistoryOrderGetString(ticket_number, property_id);
}

bool CustomHistoryOrderGetString(ulong ticket_number, ENUM_ORDER_PROPERTY_STRING property_id, string &var)
{
   return CustomOrder::HistoryOrderGetString(ticket_number, property_id, var);
}

string CustomHistoryDealGetString(ulong ticket_number, ENUM_DEAL_PROPERTY_STRING property_id)
{
   return CustomOrder::HistoryDealGetString(ticket_number, property_id);
}

bool CustomHistoryDealGetString(ulong ticket_number, ENUM_DEAL_PROPERTY_STRING property_id, string &var)
{
   return CustomOrder::HistoryDealGetString(ticket_number, property_id, var);
}

//+------------------------------------------------------------------+
//| Substitution macros                                              |
//+------------------------------------------------------------------+
#define OrderSend CustomOrderSend
#define OrderSendAsync CustomOrderSendAsync
#define OrderCheck CustomOrderCheck
#define OrderCalcMargin CustomOrderCalcMargin
#define OrderCalcProfit CustomOrderCalcProfit
#define PositionSelect CustomPositionSelect
#define PositionGetSymbol CustomPositionGetSymbol
#define PositionGetString CustomPositionGetString
#define OrderGetString CustomOrderGetString
#define HistoryOrderGetString CustomHistoryOrderGetString
#define HistoryDealGetString CustomHistoryDealGetString

//+------------------------------------------------------------------+
