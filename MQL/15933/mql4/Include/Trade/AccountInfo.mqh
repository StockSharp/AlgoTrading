//+------------------------------------------------------------------+
//|                                                  AccountInfo.mqh |
//|                   Copyright 2009-2013, MetaQuotes Software Corp. |
//|                                              http://www.mql4.com |
//+------------------------------------------------------------------+
#include <Object.mqh>
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_ACCOUNT_TRADE_MODE
  {
   ACCOUNT_TRADE_MODE_DEMO,
   ACCOUNT_TRADE_MODE_CONTEST,
   ACCOUNT_TRADE_MODE_REAL
  };
enum ENUM_ACCOUNT_STOPOUT_MODE
  {
   ACCOUNT_STOPOUT_MODE_PERCENT,
   ACCOUNT_STOPOUT_MODE_MONEY
  };
//+------------------------------------------------------------------+
//| Class CAccountInfo.                                              |
//| Appointment: Class for access to account info.                   |
//|              Derives from class CObject.                         |
//+------------------------------------------------------------------+
class CAccountInfo : public CObject
  {
public:
                     CAccountInfo(void);
                    ~CAccountInfo(void);
//--- AccountFreeMarginMode ???
   //--- fast access methods to the integer account propertyes
   long              Login(void) const { return(AccountNumber()); }
   ENUM_ACCOUNT_TRADE_MODE TradeMode(void) const;
   string            TradeModeDescription(void) const;
   long              Leverage(void) const { return(AccountLeverage()); }
   ENUM_ACCOUNT_STOPOUT_MODE MarginMode(void) const { return((ENUM_ACCOUNT_STOPOUT_MODE)AccountStopoutMode()); }
   string            MarginModeDescription(void) const;
   bool              TradeAllowed(void) const { return(IsTradeAllowed()); }
   bool              TradeExpert(void) const { return(IsExpertEnabled()); }
   int               LimitOrders(void) const;
   //--- fast access methods to the double account propertyes
   double            Balance(void) const { return(AccountBalance()); }
   double            Credit(void) const { return(AccountCredit()); }
   double            Profit(void) const { return(AccountProfit()); }
   double            Equity(void) const { return(AccountEquity()); }
   double            Margin(void) const { return(AccountMargin()); }
   double            FreeMargin(void) const { return(AccountFreeMargin()); }
   double            MarginLevel(void) const;
   double            MarginCall(void) const { return(AccountStopoutLevel()); }  // ???
   double            MarginStopOut(void) const { return(AccountStopoutLevel()); }  // ???
   //--- fast access methods to the string account propertyes
   string            Name(void) const { return(AccountName()); }
   string            Server(void) const { return(AccountServer()); }
   string            Currency(void) const { return(AccountCurrency()); }
   string            Company(void) const { return(AccountCompany()); }
   //--- checks
   double            OrderProfitCheck(const string symbol,const ENUM_ORDER_TYPE trade_operation,
                                      const double volume,const double price_open,const double price_close) const;
   double            MarginCheck(const string symbol,const ENUM_ORDER_TYPE trade_operation,
                                 const double volume,const double price) const;
//--- AccountFreeMarginCheck
   double            FreeMarginCheck(const string symbol,const ENUM_ORDER_TYPE trade_operation,
                                     const double volume,const double price) const;
   double            MaxLotCheck(const string symbol,const ENUM_ORDER_TYPE trade_operation,
                                 const double price,const double percent=100) const;
  };
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CAccountInfo::CAccountInfo(void)
  {
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CAccountInfo::~CAccountInfo(void)
  {
  }
//+------------------------------------------------------------------+
//| Get the property value "ACCOUNT_TRADE_MODE"                      |
//+------------------------------------------------------------------+
ENUM_ACCOUNT_TRADE_MODE CAccountInfo::TradeMode(void) const
  {
   if(IsDemo())
      return(ACCOUNT_TRADE_MODE_DEMO);
//--- если не демо, значит реал
   return(ACCOUNT_TRADE_MODE_REAL);
  }
//+------------------------------------------------------------------+
//| Get the property value "ACCOUNT_TRADE_MODE" as string            |
//+------------------------------------------------------------------+
string CAccountInfo::TradeModeDescription(void) const
  {
   string str;
//---
   switch(TradeMode())
     {
      case ACCOUNT_TRADE_MODE_DEMO   : str="Demo trading account";    break;
      case ACCOUNT_TRADE_MODE_CONTEST: str="Contest trading account"; break;
      case ACCOUNT_TRADE_MODE_REAL   : str="Real trading account";    break;
      default                        : str="Unknown trade account";
     }
//---
   return(str);
  }
//+------------------------------------------------------------------+
//| Get the property value "ACCOUNT_MARGIN_SO_MODE" as string        |
//+------------------------------------------------------------------+
string CAccountInfo::MarginModeDescription(void) const
  {
   string str;
//---
   switch(MarginMode())
     {
      case ACCOUNT_STOPOUT_MODE_PERCENT: str="Level is specified in percentage"; break;
      case ACCOUNT_STOPOUT_MODE_MONEY  : str="Level is specified in money";      break;
      default                          : str="Unknown margin mode";
     }
//---
   return(str);
  }
//+------------------------------------------------------------------+
//| Get the property value "ACCOUNT_LIMIT_ORDERS"                    |
//+------------------------------------------------------------------+
int CAccountInfo::LimitOrders(void) const
  {
//--- в 4 не поддерживается
   return(INT_MAX);
  }
//+------------------------------------------------------------------+
//| Get the property value "ACCOUNT_MARGIN_LEVEL"                    |
//+------------------------------------------------------------------+
double CAccountInfo::MarginLevel(void) const
  {
   double balance=Balance();
//---
   return((balance==0) ? 0: Margin()/balance);
  }
//+------------------------------------------------------------------+
//| Access functions OrderCalcProfit(...).                           |
//| INPUT:  name            - symbol name,                           |
//|         trade_operation - trade operation,                       |
//|         volume          - volume of the opening position,        |
//|         price_open      - price of the opening position,         |
//|         price_close     - price of the closing position.         |
//+------------------------------------------------------------------+
double CAccountInfo::OrderProfitCheck(const string symbol,const ENUM_ORDER_TYPE trade_operation,
                                      const double volume,const double price_open,const double price_close) const
  {
   double profit=EMPTY_VALUE;
//---
//   if(!OrderCalcProfit(trade_operation,symbol,volume,price_open,price_close,profit))
//      return(EMPTY_VALUE);
//---
   return(profit);
  }
//+------------------------------------------------------------------+
//| Access functions OrderCalcMargin(...).                           |
//| INPUT:  name            - symbol name,                           |
//|         trade_operation - trade operation,                       |
//|         volume          - volume of the opening position,        |
//|         price           - price of the opening position.         |
//+------------------------------------------------------------------+
double CAccountInfo::MarginCheck(const string symbol,const ENUM_ORDER_TYPE trade_operation,
                                 const double volume,const double price) const
  {
   return(FreeMargin()-AccountFreeMarginCheck(symbol,trade_operation,volume));
  }
//+------------------------------------------------------------------+
//| Access functions OrderCalcMargin(...).                           |
//| INPUT:  name            - symbol name,                           |
//|         trade_operation - trade operation,                       |
//|         volume          - volume of the opening position,        |
//|         price           - price of the opening position.         |
//+------------------------------------------------------------------+
double CAccountInfo::FreeMarginCheck(const string symbol,const ENUM_ORDER_TYPE trade_operation,
                                     const double volume,const double price) const
  {
   return(AccountFreeMarginCheck(symbol,trade_operation,volume));
  }
//+------------------------------------------------------------------+
//| Access functions OrderCalcMargin(...).                           |
//| INPUT:  name            - symbol name,                           |
//|         trade_operation - trade operation,                       |
//|         price           - price of the opening position,         |
//|         percent         - percent of available margin [1-100%].  |
//+------------------------------------------------------------------+
double CAccountInfo::MaxLotCheck(const string symbol,const ENUM_ORDER_TYPE trade_operation,
                                 const double price,const double percent) const
  {
   double margin=0.0;
//--- checks
   if(symbol=="" || price<=0.0 || percent<1 || percent>100)
     {
      Print("CAccountInfo::MaxLotCheck invalid parameters");
      return(0.0);
     }
//--- calculate margin requirements for 1 lot
   if(!MarginCheck(symbol,trade_operation,1.0,price) || margin<0.0)
     {
      Print("CAccountInfo::MaxLotCheck margin calculation failed");
      return(0.0);
     }
//---
   if(margin==0.0) // for pending orders
      return(MarketInfo(symbol,MODE_MAXLOT));
//--- calculate maximum volume
   double volume=NormalizeDouble(FreeMargin()*percent/100.0/margin,2);
//--- normalize and check limits
   double stepvol=MarketInfo(symbol,MODE_LOTSTEP);
   if(stepvol>0.0)
      volume=stepvol*MathFloor(volume/stepvol);
//---
   double minvol=MarketInfo(symbol,MODE_MINLOT);
   if(volume<minvol)
      volume=0.0;
//---
   double maxvol=MarketInfo(symbol,MODE_MAXLOT);
   if(volume>maxvol)
      volume=maxvol;
//--- return volume
   return(volume);
  }
//+------------------------------------------------------------------+
