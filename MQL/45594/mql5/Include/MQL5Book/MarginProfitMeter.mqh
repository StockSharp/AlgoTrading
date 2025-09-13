//+------------------------------------------------------------------+
//|                                            MarginProfitMeter.mqh |
//|                               Copyright (c) 2018-2022, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//| A set of functions to calculate margin, potential profit/loss,   |
//| point value and coversion rates.                                 |
//+------------------------------------------------------------------+

namespace MPM
{
   // Analogue of built-in OrderCalcMargin which is not allowed in indicators
   bool OrderCalcMargin(const ENUM_ORDER_TYPE action, const string symbol,
      double volume, double price, double &margin)
   {
      double marginInit, marginMain;
      MqlTick tick;
     
      // check given parameters
      if((action != ORDER_TYPE_BUY && action != ORDER_TYPE_SELL)
         || volume < 0 || price < 0) return false;
     
      // request all properties used in the formulae
      if(!SymbolInfoTick(symbol, tick)) return false;
      if(!SymbolInfoMarginRate(symbol, action, marginInit, marginMain)) return false;
      const double contract = SymbolInfoDouble(symbol, SYMBOL_TRADE_CONTRACT_SIZE);
      long leverage = AccountInfoInteger(ACCOUNT_LEVERAGE);
      if(volume == 0) volume = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
      if(price == 0) price = action == ORDER_TYPE_BUY ? tick.ask : tick.bid;
      
      if(margin == DBL_MAX) marginInit = marginMain;
      margin = 0;
      
      const ENUM_SYMBOL_CALC_MODE m =
         (ENUM_SYMBOL_CALC_MODE)SymbolInfoInteger(symbol, SYMBOL_TRADE_CALC_MODE);
     
      switch(m)
      {
      case SYMBOL_CALC_MODE_FOREX_NO_LEVERAGE:
         leverage = 1;
         
      case SYMBOL_CALC_MODE_FOREX:
         margin = volume * contract / leverage * marginInit;
         break;
   
      case SYMBOL_CALC_MODE_CFD:      
         margin = volume * contract * price * marginInit;
         break;
         
      case SYMBOL_CALC_MODE_CFDINDEX:
         margin = volume * contract * price * SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE)
            / SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE) * marginInit;
         break;
         
      case SYMBOL_CALC_MODE_CFDLEVERAGE:
         margin = volume * contract * price / leverage * marginInit;
         break;
         
      case SYMBOL_CALC_MODE_EXCH_STOCKS:
      case SYMBOL_CALC_MODE_EXCH_STOCKS_MOEX:
         if(price == 0) price = tick.last;
         margin = volume * contract * price * marginInit;
         break;
   
      case SYMBOL_CALC_MODE_FUTURES:
      case SYMBOL_CALC_MODE_EXCH_FUTURES:
      case SYMBOL_CALC_MODE_EXCH_FUTURES_FORTS:
         margin = volume * SymbolInfoDouble(symbol, SYMBOL_MARGIN_INITIAL) * marginInit; 
         break;
      default:
         PrintFormat("Unsupported symbol %s trade mode: %s", symbol, EnumToString(m));
      }
     
      string account = AccountInfoString(ACCOUNT_CURRENCY);
      string current = SymbolInfoString(symbol, SYMBOL_CURRENCY_MARGIN);
      if(current != account)
      {
         if(!Convert(current, account, action == ORDER_TYPE_SELL, margin)) return false;
      }
     
      return true;
   }
   
   // Search available symbols for a one built of the 'current' and 'account' currencies
   int FindExchangeRate(const string current, const string account, string &result)
   {
      for(int i = 0; i < SymbolsTotal(true); i++)
      {
         const string symbol = SymbolName(i, true);
         const ENUM_SYMBOL_CALC_MODE m =
            (ENUM_SYMBOL_CALC_MODE)SymbolInfoInteger(symbol, SYMBOL_TRADE_CALC_MODE);
         if(m == SYMBOL_CALC_MODE_FOREX || m == SYMBOL_CALC_MODE_FOREX_NO_LEVERAGE)
         {
            string base = SymbolInfoString(symbol, SYMBOL_CURRENCY_BASE);
            string profit = SymbolInfoString(symbol, SYMBOL_CURRENCY_PROFIT);
            if(base == current && profit == account)
            {
               result = symbol;
               return +1;
            }
            else
            if(base == account && profit == current)
            {
               result = symbol;
               return -1;
            }
         }
      }
      return 0;
   }
   
   // Estimate a rate of specified symbol at a given moment in past
   double GetHistoricPrice(const string symbol, const datetime moment, const bool ask)
   {
      const int offset = iBarShift(symbol, _Period, moment);
      // NB: iClose can hold Last price instead of Bid for exchange symbols
      //     there is no fast way to handle this, only tick history analysis
      return iClose(symbol, _Period, offset) +
         (ask ? iSpread(symbol, _Period, offset) * SymbolInfoDouble(symbol, SYMBOL_POINT) : 0);
   }
   
   // Convert amount of 'current' money into 'account' money
   bool Convert(const string current, const string account,
      const bool ask, double &margin, const datetime moment = 0)
   {
      string rate;
      int dir = FindExchangeRate(current, account, rate);
      if(dir == +1)
      {
         margin *= moment == 0 ?
            SymbolInfoDouble(rate, ask ? SYMBOL_BID : SYMBOL_ASK) :
            GetHistoricPrice(rate, moment, ask);
      }
      else if(dir == -1)
      {
         margin /= moment == 0 ?
            SymbolInfoDouble(rate, ask ? SYMBOL_ASK : SYMBOL_BID) :
            GetHistoricPrice(rate, moment, ask);
      }
      else
      {
         static bool once = false;
         if(!once)
         {
            Print("Can't convert ", current, " -> ", account);
            once = true;
         }
      }
      return true;
   }
   
   // Return point value (in account currency) of specific symbol
   double PointValue(const string symbol, const bool ask = false, const datetime moment = 0)
   {
      const double point = SymbolInfoDouble(symbol, SYMBOL_POINT);
      const double contract = SymbolInfoDouble(symbol, SYMBOL_TRADE_CONTRACT_SIZE);
      const ENUM_SYMBOL_CALC_MODE m =
         (ENUM_SYMBOL_CALC_MODE)SymbolInfoInteger(symbol, SYMBOL_TRADE_CALC_MODE);
      double result = 0;
   
      switch(m)
      {
      case SYMBOL_CALC_MODE_FOREX_NO_LEVERAGE:
      case SYMBOL_CALC_MODE_FOREX:
      case SYMBOL_CALC_MODE_CFD:
      case SYMBOL_CALC_MODE_CFDINDEX:
      case SYMBOL_CALC_MODE_CFDLEVERAGE:
      case SYMBOL_CALC_MODE_EXCH_STOCKS:
      case SYMBOL_CALC_MODE_EXCH_STOCKS_MOEX:
         result = point * contract;
         break;
   
      case SYMBOL_CALC_MODE_FUTURES:
      case SYMBOL_CALC_MODE_EXCH_FUTURES:
      case SYMBOL_CALC_MODE_EXCH_FUTURES_FORTS:
         result = point * SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE)
            / SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE);
         break;
      default:
         PrintFormat("Unsupported symbol %s trade mode: %s", symbol, EnumToString(m));
      }
      
      string account = AccountInfoString(ACCOUNT_CURRENCY);
      string current = SymbolInfoString(symbol, SYMBOL_CURRENCY_PROFIT);
   
      if(current != account)
      {
         if(!Convert(current, account, ask, result, moment)) return 0;
      }
     
      return result;
   }
};
//+------------------------------------------------------------------+
