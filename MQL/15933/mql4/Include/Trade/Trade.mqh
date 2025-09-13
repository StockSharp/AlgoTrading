//+------------------------------------------------------------------+
//|                                                        Trade.mqh |
//|                   Copyright 2009-2013, MetaQuotes Software Corp. |
//|                                              http://www.mql4.com |
//+------------------------------------------------------------------+
#include <Object.mqh>
#include "SymbolInfo.mqh"
#include "OrderInfo.mqh"
#include "HistoryOrderInfo.mqh"
#include "PositionInfo.mqh"
#include "DealInfo.mqh"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_TRADE_REQUEST_ACTIONS
  {
   TRADE_ACTION_DEAL,    // Установить торговый ордер на немедленное совершение сделки с указанными параметрами (поставить рыночный ордер)
   TRADE_ACTION_PENDING, // Установить торговый ордер на совершение сделки при указанных условиях (отложенный ордер)
   TRADE_ACTION_SLTP,    // Изменить значения Stop Loss и Take Profit у открытой позиции
   TRADE_ACTION_MODIFY,  // Изменить параметры ранее установленного торгового ордера
   TRADE_ACTION_REMOVE   // Удалить ранее отложенный торговый ордер
  };
enum ENUM_TRADE_RETCODE
  {
   TRADE_RETCODE_REQUOTE, // Реквота
   TRADE_RETCODE_REJECT, // Запрос отвергнут
   TRADE_RETCODE_CANCEL, // Запрос отменен трейдером
   TRADE_RETCODE_PLACED, // Ордер размещен
   TRADE_RETCODE_DONE, // Заявка выполнена
   TRADE_RETCODE_DONE_PARTIAL, // Заявка выполнена частично
   TRADE_RETCODE_ERROR, // Ошибка обработки запроса
   TRADE_RETCODE_TIMEOUT, // Запрос отменен по истечению времени
   TRADE_RETCODE_INVALID, // Неправильный запрос
   TRADE_RETCODE_INVALID_VOLUME, // Неправильный объем в запросе
   TRADE_RETCODE_INVALID_PRICE, // Неправильная цена в запросе
   TRADE_RETCODE_INVALID_STOPS, // Неправильные стопы в запросе
   TRADE_RETCODE_TRADE_DISABLED, // Торговля запрещена
   TRADE_RETCODE_MARKET_CLOSED, // Рынок закрыт
   TRADE_RETCODE_NO_MONEY, // Нет достаточных денежных средств для выполнения запроса
   TRADE_RETCODE_PRICE_CHANGED, // Цены изменились
   TRADE_RETCODE_PRICE_OFF, // Отсутствуют котировки для обработки запроса
   TRADE_RETCODE_INVALID_EXPIRATION, // Неверная дата истечения ордера в запросе
   TRADE_RETCODE_ORDER_CHANGED, // Состояние ордера изменилось
   TRADE_RETCODE_TOO_MANY_REQUESTS, // Слишком частые запросы
   TRADE_RETCODE_NO_CHANGES, // В запросе нет изменений
   TRADE_RETCODE_SERVER_DISABLES_AT, // Автотрейдинг запрещен сервером
   TRADE_RETCODE_CLIENT_DISABLES_AT, // Автотрейдинг запрещен клиентским терминалом
   TRADE_RETCODE_LOCKED, // Запрос заблокирован для обработки
   TRADE_RETCODE_FROZEN, // Ордер или позиция заморожены
   TRADE_RETCODE_INVALID_FILL, // Указан неподдерживаемый тип исполнения ордера по остатку 
   TRADE_RETCODE_CONNECTION, // Нет соединения с торговым сервером
   TRADE_RETCODE_ONLY_REAL, // Операция разрешена только для реальных счетов
   TRADE_RETCODE_LIMIT_ORDERS, // Достигнут лимит на количество отложенных ордеров
   TRADE_RETCODE_LIMIT_VOLUME, // Достигнут лимит на объем ордеров и позиций для данного символа
   TRADE_RETCODE_INVALID_ORDER, // Неверный или запрещённый тип ордера
   TRADE_RETCODE_POSITION_CLOSED // Позиция с указанным POSITION_IDENTIFIER уже закрыта
  };
enum ENUM_ORDER_TYPE_FILLING
  {
   ORDER_FILLING_FOK,
   ORDER_FILLING_IOC,
   ORDER_FILLING_RETURN
  };
enum ENUM_ORDER_TYPE_TIME
  {
   ORDER_TIME_GTC,
   ORDER_TIME_DAY,
   ORDER_TIME_SPECIFIED,
   ORDER_TIME_SPECIFIED_DAY
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
struct MqlTradeRequest
  {
   ENUM_TRADE_REQUEST_ACTIONS    action;           // Тип выполняемого действия
   ulong                         magic;            // Штамп эксперта (идентификатор magic number)
   ulong                         order;            // Тикет ордера
   string                        symbol;           // Имя торгового инструмента
   double                        volume;           // Запрашиваемый объем сделки в лотах
   double                        price;            // Цена 
   double                        stoplimit;        // Уровень StopLimit ордера
   double                        sl;               // Уровень Stop Loss ордера
   double                        tp;               // Уровень Take Profit ордера
   ulong                         deviation;        // Максимально приемлемое отклонение от запрашиваемой цены
   ENUM_ORDER_TYPE               type;             // Тип ордера
   ENUM_ORDER_TYPE_FILLING       type_filling;     // Тип ордера по исполнению
   ENUM_ORDER_TYPE_TIME          type_time;        // Тип ордера по времени действия
   datetime                      expiration;       // Срок истечения ордера (для ордеров типа ORDER_TIME_SPECIFIED)
   string                        comment;          // Комментарий к ордеру
  };
struct MqlTradeResult
  {
   uint     retcode;          // Код результата операции
   ulong    deal;             // Тикет сделки, если она совершена
   ulong    order;            // Тикет ордера, если он выставлен
   double   volume;           // Объем сделки, подтверждённый брокером
   double   price;            // Цена в сделке, подтверждённая брокером
   double   bid;              // Текущая рыночная цена предложения (цены реквота)
   double   ask;              // Текущая рыночная цена спроса (цены реквота)
   string   comment;          // Комментарий брокера к операции (по умолчанию заполняется расшифровкой)
   uint     request_id;       // Идентификатор запроса, устанавливается терминалом при отправке 
  };
struct MqlTradeCheckResult
  {
   uint         retcode;             // Код ответа
   double       balance;             // Баланс после совершения сделки
   double       equity;              // Эквити после совершения сделки
   double       profit;              // Плавающая прибыль
   double       margin;              // Маржевые требования
   double       margin_free;         // Свободная маржа
   double       margin_level;        // Уровень маржи
   string       comment;             // Комментарий к коду ответа (описание ошибки)
  };
//+------------------------------------------------------------------+
//| enumerations                                                     |
//+------------------------------------------------------------------+
enum ENUM_LOG_LEVELS
  {
   LOG_LEVEL_NO    =0,
   LOG_LEVEL_ERRORS=1,
   LOG_LEVEL_ALL   =2
  };
//+------------------------------------------------------------------+
//| Class CTrade.                                                    |
//| Appointment: Class trade operations.                             |
//|              Derives from class CObject.                         |
//+------------------------------------------------------------------+
class CTrade : public CObject
  {
protected:
   MqlTradeRequest   m_request;         // request data
   MqlTradeResult    m_result;          // result data
   MqlTradeCheckResult m_check_result;  // result check data
   ulong             m_magic;           // expert magic number
   ulong             m_deviation;       // deviation default
   ENUM_ORDER_TYPE_FILLING m_type_filling;
   //---
   ENUM_LOG_LEVELS   m_log_level;

public:
                     CTrade(void);
                    ~CTrade(void);
   //--- methods of access to protected data
   void              LogLevel(const ENUM_LOG_LEVELS log_level)   { m_log_level=log_level;               }
   void              Request(MqlTradeRequest &request) const;
   ENUM_TRADE_REQUEST_ACTIONS RequestAction(void)          const { return(m_request.action);            }
   string            RequestActionDescription(void)        const;
   ulong             RequestMagic(void)                    const { return(m_request.magic);             }
   ulong             RequestOrder(void)                    const { return(m_request.order);             }
   string            RequestSymbol(void)                   const { return(m_request.symbol);            }
   double            RequestVolume(void)                   const { return(m_request.volume);            }
   double            RequestPrice(void)                    const { return(m_request.price);             }
   double            RequestStopLimit(void)                const { return(m_request.stoplimit);         }
   double            RequestSL(void)                       const { return(m_request.sl);                }
   double            RequestTP(void)                       const { return(m_request.tp);                }
   ulong             RequestDeviation(void)                const { return(m_request.deviation);         }
   ENUM_ORDER_TYPE   RequestType(void)                     const { return(m_request.type);              }
   string            RequestTypeDescription(void) const;
   ENUM_ORDER_TYPE_FILLING RequestTypeFilling(void)        const { return(m_request.type_filling);      }
   string            RequestTypeFillingDescription(void)   const;
   ENUM_ORDER_TYPE_TIME RequestTypeTime(void)              const { return(m_request.type_time);         }
   string            RequestTypeTimeDescription(void)      const;
   datetime          RequestExpiration(void)               const { return(m_request.expiration);        }
   string            RequestComment(void)                  const { return(m_request.comment);           }
   //---
   void              Result(MqlTradeResult &result)        const;
   uint              ResultRetcode(void)                   const { return(m_result.retcode);            }
   string            ResultRetcodeDescription(void)        const;
   ulong             ResultDeal(void)                      const { return(m_result.deal);               }
   ulong             ResultOrder(void)                     const { return(m_result.order);              }
   double            ResultVolume(void)                    const { return(m_result.volume);             }
   double            ResultPrice(void)                     const { return(m_result.price);              }
   double            ResultBid(void)                       const { return(m_result.bid);                }
   double            ResultAsk(void)                       const { return(m_result.ask);                }
   string            ResultComment(void)                   const { return(m_result.comment);            }
   //---
   void              CheckResult(MqlTradeCheckResult &check_result) const;
   uint              CheckResultRetcode(void)              const { return(m_check_result.retcode);      }
   string            CheckResultRetcodeDescription(void)   const;
   double            CheckResultBalance(void)              const { return(m_check_result.balance);      }
   double            CheckResultEquity(void)               const { return(m_check_result.equity);       }
   double            CheckResultProfit(void)               const { return(m_check_result.profit);       }
   double            CheckResultMargin(void)               const { return(m_check_result.margin);       }
   double            CheckResultMarginFree(void)           const { return(m_check_result.margin_free);  }
   double            CheckResultMarginLevel(void)          const { return(m_check_result.margin_level); }
   string            CheckResultComment(void)              const { return(m_check_result.comment);      }
   //--- trade methods
   void              SetAsyncMode(const bool mode)               {                                      }
   void              SetExpertMagicNumber(const ulong magic)     { m_magic=magic;                       }
   void              SetDeviationInPoints(const ulong deviation) { m_deviation=deviation;               }
   void              SetTypeFilling(const ENUM_ORDER_TYPE_FILLING filling) { m_type_filling=filling;    }
   void              SetTypeFilling(const int filling)           { }
   //--- methods for working with positions
   bool              PositionOpen(const string symbol,const ENUM_ORDER_TYPE order_type,const double volume,
                                  const double price,const double sl,const double tp,const string comment="");
   bool              PositionModify(const string symbol,const double sl,const double tp);
   bool              PositionClose(const string symbol,const ulong deviation=ULONG_MAX);
   //--- methods for working with pending orders
   bool              OrderOpen(const string symbol,const ENUM_ORDER_TYPE order_type,const double volume,
                               const double limit_price,const double price,const double sl,const double tp,
                               ENUM_ORDER_TYPE_TIME type_time=ORDER_TIME_GTC,const datetime expiration=0,
                               const string comment="");
   bool              OrderModify(const ulong ticket,const double price,const double sl,const double tp,
                                 const ENUM_ORDER_TYPE_TIME type_time,const datetime expiration,const double stoplimit=0.0);
   bool              OrderDelete(const ulong ticket);
   //--- additions methods
   bool              Buy(const double volume,const string symbol=NULL,double price=0.0,const double sl=0.0,const double tp=0.0,const string comment="");
   bool              Sell(const double volume,const string symbol=NULL,double price=0.0,const double sl=0.0,const double tp=0.0,const string comment="");
   bool              BuyLimit(const double volume,const double price,const string symbol=NULL,const double sl=0.0,const double tp=0.0,
                              const ENUM_ORDER_TYPE_TIME type_time=ORDER_TIME_GTC,const datetime expiration=0,const string comment="");
   bool              BuyStop(const double volume,const double price,const string symbol=NULL,const double sl=0.0,const double tp=0.0,
                             const ENUM_ORDER_TYPE_TIME type_time=ORDER_TIME_GTC,const datetime expiration=0,const string comment="");
   bool              SellLimit(const double volume,const double price,const string symbol=NULL,const double sl=0.0,const double tp=0.0,
                               const ENUM_ORDER_TYPE_TIME type_time=ORDER_TIME_GTC,const datetime expiration=0,const string comment="");
   bool              SellStop(const double volume,const double price,const string symbol=NULL,const double sl=0.0,const double tp=0.0,
                              const ENUM_ORDER_TYPE_TIME type_time=ORDER_TIME_GTC,const datetime expiration=0,const string comment="");
   //--- method check
   virtual bool      OrderCheck(const MqlTradeRequest &request,MqlTradeCheckResult &check_result);
   virtual bool      OrderSend(const MqlTradeRequest &request,MqlTradeResult &result);
   //--- info methods
   void              PrintRequest(void) const;
   void              PrintResult(void) const;
   //--- positions
   string            FormatPositionType(string &str,const uint type) const;
   //--- orders
   string            FormatOrderType(string &str,const uint type) const;
   string            FormatOrderStatus(string &str,const uint status) const;
   string            FormatOrderTypeFilling(string &str,const uint type) const;
   string            FormatOrderTypeTime(string &str,const uint type) const;
   string            FormatOrderPrice(string &str,const double price_order,const double price_trigger,const uint digits) const;
   //--- trade request
   string            FormatRequest(string &str,const MqlTradeRequest &request) const;
   string            FormatRequestResult(string &str,const MqlTradeRequest &request,const MqlTradeResult &result) const;

protected:
   bool              FillingCheck(const string symbol);
   bool              ExpirationCheck(const string symbol);
   bool              OrderTypeCheck(const string symbol);
   void              ClearStructures(void);
   bool              IsStopped(const string function);
  };
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CTrade::CTrade(void) : m_magic(0),
                       m_deviation(10),
                       m_log_level(LOG_LEVEL_ERRORS)

  {
//--- initialize protected data
   ClearStructures();
//--- check programm mode
   if(IsOptimization())
      m_log_level=LOG_LEVEL_NO;
   if(IsTesting())
      m_log_level=LOG_LEVEL_ALL;
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CTrade::~CTrade(void)
  {
  }
//+------------------------------------------------------------------+
//| Get the request structure                                        |
//+------------------------------------------------------------------+
void CTrade::Request(MqlTradeRequest &request) const
  {
   request.action      =m_request.action;
   request.magic       =m_request.magic;
   request.order       =m_request.order;
   request.symbol      =m_request.symbol;
   request.volume      =m_request.volume;
   request.price       =m_request.price;
   request.stoplimit   =m_request.stoplimit;
   request.sl          =m_request.sl;
   request.tp          =m_request.tp;
   request.deviation   =m_request.deviation;
   request.type        =m_request.type;
   request.type_filling=m_request.type_filling;
   request.type_time   =m_request.type_time;
   request.expiration  =m_request.expiration;
   request.comment     =m_request.comment;
  }
//+------------------------------------------------------------------+
//| Get the trade action as string                                   |
//+------------------------------------------------------------------+
string CTrade::RequestActionDescription(void) const
  {
   string str;
//---
   FormatRequest(str,m_request);
//---
   return(str);
  }
//+------------------------------------------------------------------+
//| Get the order type as string                                     |
//+------------------------------------------------------------------+
string CTrade::RequestTypeDescription(void) const
  {
   string str;
//---
   FormatOrderType(str,(uint)m_request.order);
//---
   return(str);
  }
//+------------------------------------------------------------------+
//| Get the order type filling as string                             |
//+------------------------------------------------------------------+
string CTrade::RequestTypeFillingDescription(void) const
  {
   string str;
//---
   FormatOrderTypeFilling(str,m_request.type_filling);
//---
   return(str);
  }
//+------------------------------------------------------------------+
//| Get the order type time as string                                |
//+------------------------------------------------------------------+
string CTrade::RequestTypeTimeDescription(void) const
  {
   string str;
//---
   FormatOrderTypeTime(str,m_request.type_time);
//---
   return(str);
  }
//+------------------------------------------------------------------+
//| Get the result structure                                         |
//+------------------------------------------------------------------+
void CTrade::Result(MqlTradeResult &result) const
  {
   result.retcode   =m_result.retcode;
   result.deal      =m_result.deal;
   result.order     =m_result.order;
   result.volume    =m_result.volume;
   result.price     =m_result.price;
   result.bid       =m_result.bid;
   result.ask       =m_result.ask;
   result.comment   =m_result.comment;
   result.request_id=m_result.request_id;
  }
//+------------------------------------------------------------------+
//| Get the retcode value as string                                  |
//+------------------------------------------------------------------+
string CTrade::ResultRetcodeDescription(void) const
  {
   string str;
//---
   FormatRequestResult(str,m_request,m_result);
//---
   return(str);
  }
//+------------------------------------------------------------------+
//| Get the check result structure                                   |
//+------------------------------------------------------------------+
void CTrade::CheckResult(MqlTradeCheckResult &check_result) const
  {
//--- copy structure
   check_result.retcode     =m_check_result.retcode;
   check_result.balance     =m_check_result.balance;
   check_result.equity      =m_check_result.equity;
   check_result.profit      =m_check_result.profit;
   check_result.margin      =m_check_result.margin;
   check_result.margin_free =m_check_result.margin_free;
   check_result.margin_level=m_check_result.margin_level;
   check_result.comment     =m_check_result.comment;
  }
//+------------------------------------------------------------------+
//| Get the check retcode value as string                            |
//+------------------------------------------------------------------+
string CTrade::CheckResultRetcodeDescription(void) const
  {
   string str;
   MqlTradeResult result;
//---
   result.retcode=m_check_result.retcode;
   FormatRequestResult(str,m_request,result);
//---
   return(str);
  }
//+------------------------------------------------------------------+
//| Open position                                                    |
//+------------------------------------------------------------------+
bool CTrade::PositionOpen(const string symbol,const ENUM_ORDER_TYPE order_type,const double volume,
                          const double price,const double sl,const double tp,const string comment)
  {
//--- check stopped
   if(IsStopped(__FUNCTION__))
      return(false);
//--- clean
   ClearStructures();
//--- check
   if(order_type!=ORDER_TYPE_BUY && order_type!=ORDER_TYPE_SELL)
     {
      m_result.retcode=TRADE_RETCODE_INVALID;
      m_result.comment="Invalid order type";
      return(false);
     }
//--- setting request
   m_request.action      =TRADE_ACTION_DEAL;
   m_request.symbol      =symbol;
   m_request.magic       =m_magic;
   m_request.volume      =volume;
   m_request.type        =order_type;
   m_request.price       =price;
   m_request.sl          =sl;
   m_request.tp          =tp;
   m_request.deviation   =m_deviation;
//--- check order type
   if(!OrderTypeCheck(symbol))
      return(false);
//--- check filling
   if(!FillingCheck(symbol))
      return(false);
   m_request.comment=comment;
//--- action and return the result
   return(OrderSend(m_request,m_result));
  }
//+------------------------------------------------------------------+
//| Modify specified opened position                                 |
//+------------------------------------------------------------------+
bool CTrade::PositionModify(const string symbol,const double sl,const double tp)
  {
//--- check stopped
   if(IsStopped(__FUNCTION__))
      return(false);
//--- clean
   ClearStructures();
//--- setting request
   m_request.action=TRADE_ACTION_SLTP;
   m_request.symbol=symbol;
   m_request.magic =m_magic;
   m_request.sl    =sl;
   m_request.tp    =tp;
//--- action and return the result
   return(OrderSend(m_request,m_result));
  }
//+------------------------------------------------------------------+
//| Close specified opened position                                  |
//+------------------------------------------------------------------+
bool CTrade::PositionClose(const string symbol,const ulong deviation)
  {
   bool   partial_close=false;
   int    retry_count  =10;
   uint   retcode      =TRADE_RETCODE_REJECT;
//--- check stopped
   if(IsStopped(__FUNCTION__))
      return(false);
//--- clean
   ClearStructures();
/*   do
     {
      //--- check
      if(PositionSelect(symbol))
        {
         if((ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
           {
            //--- prepare request for close BUY position
            m_request.type =ORDER_TYPE_SELL;
            m_request.price=MarketInfo(Symbol(),MODE_BID);
           }
         else
           {
            //--- prepare request for close SELL position
            m_request.type =ORDER_TYPE_BUY;
            m_request.price=MarketInfo(Symbol(),MODE_ASK);
           }
        }
      else
        {
         //--- position not found
         m_result.retcode=retcode;
         return(false);
        }
      //--- setting request
      m_request.action      =TRADE_ACTION_DEAL;
      m_request.symbol      =symbol;
      m_request.magic       =m_magic;
      m_request.deviation   =(deviation==ULONG_MAX) ? m_deviation : deviation;
      //--- check filling
      if(!FillingCheck(symbol))
         return(false);
      m_request.volume=PositionGetDouble(POSITION_VOLUME);
      //--- check volume
      double max_volume=SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX);
      if(m_request.volume>max_volume)
        {
         m_request.volume=max_volume;
         partial_close=true;
        }
      else
         partial_close=false;
      //--- order send
      if(!OrderSend(m_request,m_result))
        {
         if(--retry_count!=0) continue;
         if(retcode==TRADE_RETCODE_DONE_PARTIAL)
            m_result.retcode=retcode;
         return(false);
        }
      //--- WARNING. If position volume exceeds the maximum volume allowed for deal,
      //--- and when the asynchronous trade mode is on, for safety reasons, position is closed not completely,
      //--- but partially. It is decreased by the maximum volume allowed for deal.
      if(m_async_mode)
         break;
      retcode=TRADE_RETCODE_DONE_PARTIAL;
      if(partial_close)
         Sleep(1000);
     }
   while(partial_close);*/
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Installation pending order                                       |
//+------------------------------------------------------------------+
bool CTrade::OrderOpen(const string symbol,const ENUM_ORDER_TYPE order_type,const double volume,const double limit_price,
                       const double price,const double sl,const double tp,
                       ENUM_ORDER_TYPE_TIME type_time,const datetime expiration,const string comment)
  {
//--- check stopped
   if(IsStopped(__FUNCTION__))
      return(false);
//--- clean
   ClearStructures();
//--- check order type
   if(order_type==ORDER_TYPE_BUY || order_type==ORDER_TYPE_SELL)
     {
      m_result.retcode=TRADE_RETCODE_INVALID;
      m_result.comment="Invalid order type";
      return(false);
     }
//--- check order expiration
/*   if(type_time==ORDER_TIME_GTC && expiration==0)
     {
      int exp=(int)SymbolInfoInteger(symbol,SYMBOL_EXPIRATION_MODE);
      if((exp&SYMBOL_EXPIRATION_GTC)!=SYMBOL_EXPIRATION_GTC)
        {
         //--- if you place order for an unlimited time and if placing of such orders is prohibited
         //--- try to place order with expiration at the end of the day
         if((exp&SYMBOL_EXPIRATION_DAY)!=SYMBOL_EXPIRATION_DAY)
           {
            //--- if even this is not possible - error
            Print(__FUNCTION__,": Error: Unable to place order without explicitly specified expiration time");
            m_result.retcode=TRADE_RETCODE_INVALID_EXPIRATION;
            m_result.comment="Invalid expiration type";
            return(false);
           }
         type_time=ORDER_TIME_DAY;
        }
     }*/
//--- setting request
   m_request.action      =TRADE_ACTION_PENDING;
   m_request.symbol      =symbol;
   m_request.magic       =m_magic;
   m_request.volume      =volume;
   m_request.type        =order_type;
   m_request.stoplimit   =limit_price;
   m_request.price       =price;
   m_request.sl          =sl;
   m_request.tp          =tp;
   m_request.type_filling=m_type_filling;
   m_request.type_time   =type_time;
   m_request.expiration  =expiration;
//--- check order type
   if(!OrderTypeCheck(symbol))
      return(false);
//--- check filling
   if(!FillingCheck(symbol))
     {
      m_result.retcode=TRADE_RETCODE_INVALID_FILL;
      Print(__FUNCTION__+": Invalid filling type");
      return(false);
     }
//--- check expiration
   if(!ExpirationCheck(symbol))
     {
      m_result.retcode=TRADE_RETCODE_INVALID_EXPIRATION;
      Print(__FUNCTION__+": Invalid expiration type");
      return(false);
     }
   m_request.comment=comment;
//--- action and return the result
   return(OrderSend(m_request,m_result));
  }
//+------------------------------------------------------------------+
//| Modify specified pending order                                   |
//+------------------------------------------------------------------+
bool CTrade::OrderModify(const ulong ticket,const double price,const double sl,const double tp,
                         const ENUM_ORDER_TYPE_TIME type_time,const datetime expiration,const double stoplimit)
  {
//--- check stopped
   if(IsStopped(__FUNCTION__))
      return(false);
//--- clean
   ClearStructures();
//--- setting request
   m_request.action      =TRADE_ACTION_MODIFY;
   m_request.magic       =m_magic;
   m_request.order       =ticket;
   m_request.price       =price;
   m_request.stoplimit   =stoplimit;
   m_request.sl          =sl;
   m_request.tp          =tp;
   m_request.type_time   =type_time;
   m_request.expiration  =expiration;
//--- action and return the result
   return(OrderSend(m_request,m_result));
  }
//+------------------------------------------------------------------+
//| Delete specified pending order                                   |
//+------------------------------------------------------------------+
bool CTrade::OrderDelete(const ulong ticket)
  {
//--- check stopped
   if(IsStopped(__FUNCTION__))
      return(false);
//--- clean
   ClearStructures();
//--- setting request
   m_request.action    =TRADE_ACTION_REMOVE;
   m_request.magic     =m_magic;
   m_request.order     =ticket;
//--- action and return the result
   return(OrderSend(m_request,m_result));
  }
//+------------------------------------------------------------------+
//| Output full information of request to log                        |
//+------------------------------------------------------------------+
void CTrade::PrintRequest(void) const
  {
   if(m_log_level<LOG_LEVEL_ALL)
      return;
//---
   string str;
   printf("%s",FormatRequest(str,m_request));
  }
//+------------------------------------------------------------------+
//| Output full information of result to log                         |
//+------------------------------------------------------------------+
void CTrade::PrintResult(void) const
  {
   if(m_log_level<LOG_LEVEL_ALL)
      return;
//---
   string str;
   printf("%s",FormatRequestResult(str,m_request,m_result));
  }
//+------------------------------------------------------------------+
//| Clear structures m_request,m_result and m_check_result           |
//+------------------------------------------------------------------+
void CTrade::ClearStructures(void)
  {
   ZeroMemory(m_request);
   ZeroMemory(m_result);
   ZeroMemory(m_check_result);
  }
//+------------------------------------------------------------------+
//| Checks forced shutdown of MQL5-program                           |
//+------------------------------------------------------------------+
bool CTrade::IsStopped(const string function)
  {
   if(!IsStopped())
      return(false);
//--- MQL5 program is stopped
   printf("%s: MQL5 program is stopped. Trading is disabled",function);
   m_result.retcode=TRADE_RETCODE_CLIENT_DISABLES_AT;
   return(true);
  }
//+------------------------------------------------------------------+
//| Buy operation                                                    |
//+------------------------------------------------------------------+
bool CTrade::Buy(const double volume,const string symbol=NULL,double price=0.0,const double sl=0.0,const double tp=0.0,const string comment="")
  {
   CSymbolInfo sym;
//--- check volume
   if(volume<=0.0)
     {
      m_result.retcode=TRADE_RETCODE_INVALID_VOLUME;
      return(false);
     }
//--- check symbol
   sym.Name((symbol==NULL)?Symbol():symbol);
//--- check price
   if(price==0.0)
     {
      sym.RefreshRates();
      price=sym.Ask();
     }
//---
   return(PositionOpen(sym.Name(),ORDER_TYPE_BUY,volume,price,sl,tp,comment));
  }
//+------------------------------------------------------------------+
//| Sell operation                                                   |
//+------------------------------------------------------------------+
bool CTrade::Sell(const double volume,const string symbol=NULL,double price=0.0,const double sl=0.0,const double tp=0.0,const string comment="")
  {
   CSymbolInfo sym;
//--- check volume
   if(volume<=0.0)
     {
      m_result.retcode=TRADE_RETCODE_INVALID_VOLUME;
      return(false);
     }
//--- check symbol
   sym.Name((symbol==NULL)?Symbol():symbol);
//--- check price
   if(price==0.0)
     {
      sym.RefreshRates();
      price=sym.Bid();
     }
//---
   return(PositionOpen(sym.Name(),ORDER_TYPE_SELL,volume,price,sl,tp,comment));
  }
//+------------------------------------------------------------------+
//| Send BUY_LIMIT order                                             |
//+------------------------------------------------------------------+
bool CTrade::BuyLimit(const double volume,const double price,const string symbol=NULL,const double sl=0.0,const double tp=0.0,
                      const ENUM_ORDER_TYPE_TIME type_time=ORDER_TIME_GTC,const datetime expiration=0,const string comment="")
  {
   string sym;
//--- check volume
   if(volume<=0.0)
     {
      m_result.retcode=TRADE_RETCODE_INVALID_VOLUME;
      return(false);
     }
//--- check price
   if(price==0.0)
     {
      m_result.retcode=TRADE_RETCODE_INVALID_PRICE;
      return(false);
     }
//--- check symbol
   sym=(symbol==NULL)?Symbol():symbol;
//--- send "BUY_LIMIT" order
   return(OrderOpen(sym,ORDER_TYPE_BUY_LIMIT,volume,0.0,price,sl,tp,type_time,expiration,comment));
  }
//+------------------------------------------------------------------+
//| Send BUY_STOP order                                              |
//+------------------------------------------------------------------+
bool CTrade::BuyStop(const double volume,const double price,const string symbol=NULL,const double sl=0.0,const double tp=0.0,
                     const ENUM_ORDER_TYPE_TIME type_time=ORDER_TIME_GTC,const datetime expiration=0,const string comment="")
  {
   string sym;
//--- check volume
   if(volume<=0.0)
     {
      m_result.retcode=TRADE_RETCODE_INVALID_VOLUME;
      return(false);
     }
//--- check price
   if(price==0.0)
     {
      m_result.retcode=TRADE_RETCODE_INVALID_PRICE;
      return(false);
     }
//--- check symbol
   sym=(symbol==NULL)?Symbol():symbol;
//--- send "BUY_STOP" order
   return(OrderOpen(sym,ORDER_TYPE_BUY_STOP,volume,0.0,price,sl,tp,type_time,expiration,comment));
  }
//+------------------------------------------------------------------+
//| Send SELL_LIMIT order                                            |
//+------------------------------------------------------------------+
bool CTrade::SellLimit(const double volume,const double price,const string symbol=NULL,const double sl=0.0,const double tp=0.0,
                       const ENUM_ORDER_TYPE_TIME type_time=ORDER_TIME_GTC,const datetime expiration=0,const string comment="")
  {
   string sym;
//--- check volume
   if(volume<=0.0)
     {
      m_result.retcode=TRADE_RETCODE_INVALID_VOLUME;
      return(false);
     }
//--- check price
   if(price==0.0)
     {
      m_result.retcode=TRADE_RETCODE_INVALID_PRICE;
      return(false);
     }
//--- check symbol
   sym=(symbol==NULL)?Symbol():symbol;
//--- send "SELL_LIMIT" order
   return(OrderOpen(sym,ORDER_TYPE_SELL_LIMIT,volume,0.0,price,sl,tp,type_time,expiration,comment));
  }
//+------------------------------------------------------------------+
//| Send SELL_STOP order                                             |
//+------------------------------------------------------------------+
bool CTrade::SellStop(const double volume,const double price,const string symbol=NULL,const double sl=0.0,const double tp=0.0,
                      const ENUM_ORDER_TYPE_TIME type_time=ORDER_TIME_GTC,const datetime expiration=0,const string comment="")
  {
   string sym;
//--- check volume
   if(volume<=0.0)
     {
      m_result.retcode=TRADE_RETCODE_INVALID_VOLUME;
      return(false);
     }
//--- check price
   if(price==0.0)
     {
      m_result.retcode=TRADE_RETCODE_INVALID_PRICE;
      return(false);
     }
//--- check symbol
   sym=(symbol==NULL)?Symbol():symbol;
//--- send "SELL_STOP" order
   return(OrderOpen(sym,ORDER_TYPE_SELL_STOP,volume,0.0,price,sl,tp,type_time,expiration,comment));
  }
//+------------------------------------------------------------------+
//| Converts the position type to text                               |
//+------------------------------------------------------------------+
string CTrade::FormatPositionType(string &str,const uint type) const
  {
//--- clean
   str="";
//--- see the type
   switch(type)
     {
      case POSITION_TYPE_BUY : str="buy";  break;
      case POSITION_TYPE_SELL: str="sell"; break;

      default:
         str="unknown position type "+(string)type;
         break;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the order type to text                                  |
//+------------------------------------------------------------------+
string CTrade::FormatOrderType(string &str,const uint type) const
  {
//--- clean
   str="";
//--- see the type
   switch(type)
     {
      case ORDER_TYPE_BUY            : str="buy";             break;
      case ORDER_TYPE_SELL           : str="sell";            break;

      default:
         str="unknown order type "+(string)type;
         break;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the order filling type to text                          |
//+------------------------------------------------------------------+
string CTrade::FormatOrderTypeFilling(string &str,const uint type) const
  {
//--- clean
   str="";
//--- see the type
   switch(type)
     {
      case ORDER_FILLING_RETURN: str="return remainder"; break;
      case ORDER_FILLING_IOC   : str="cancel remainder"; break;
      case ORDER_FILLING_FOK   : str="fill or kill";     break;

      default:
         str="unknown type filling "+(string)type;
         break;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the type of order by expiration to text                 |
//+------------------------------------------------------------------+
string CTrade::FormatOrderTypeTime(string &str,const uint type) const
  {
//--- clean
   str="";
//--- see the type
   switch(type)
     {
      case ORDER_TIME_GTC          : str="gtc";           break;
      case ORDER_TIME_DAY          : str="day";           break;
      case ORDER_TIME_SPECIFIED    : str="specified";     break;
      case ORDER_TIME_SPECIFIED_DAY: str="specified day"; break;

      default:
         str="unknown type time "+(string)type;
         break;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the order prices to text                                |
//+------------------------------------------------------------------+
string CTrade::FormatOrderPrice(string &str,const double price_order,const double price_trigger,const uint digits) const
  {
   string price,trigger;
//--- clean
   str="";
//--- Is there its trigger price?
   if(price_trigger)
     {
      price  =DoubleToString(price_order,digits);
      trigger=DoubleToString(price_trigger,digits);
      str    =StringFormat("%s (%s)",price,trigger);
     }
   else
      str=DoubleToString(price_order,digits);
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the parameters of a trade request to text               |
//+------------------------------------------------------------------+
string CTrade::FormatRequest(string &str,const MqlTradeRequest &request) const
  {
   string      type,price,price_new;
   string      tmp;
   CSymbolInfo symbol;
//--- clean
   str="";
//--- set up
   int digits=5;
   if(request.symbol!=NULL)
     {
      if(symbol.Name(request.symbol))
         digits=symbol.Digits();
     }
//--- see what is wanted
   switch(request.action)
     {
      //--- instant execution of a deal
      case TRADE_ACTION_DEAL:
         switch(symbol.TradeExecution())
           {
            //--- request execution
            case SYMBOL_TRADE_EXECUTION_REQUEST:
               str=StringFormat("request %s %s %s at %s",
                                FormatOrderType(type,request.type),
                                DoubleToString(request.volume,2),
                                request.symbol,
                                DoubleToString(request.price,digits));
            //--- Is there SL or TP?
            if(request.sl!=0.0)
              {
               tmp=StringFormat(" sl: %s",DoubleToString(request.sl,digits));
               str+=tmp;
              }
            if(request.tp!=0.0)
              {
               tmp=StringFormat(" tp: %s",DoubleToString(request.tp,digits));
               str+=tmp;
              }
            break;
            //--- instant execution
            case SYMBOL_TRADE_EXECUTION_INSTANT:
               str=StringFormat("instant %s %s %s at %s",
                                FormatOrderType(type,request.type),
                                DoubleToString(request.volume,2),
                                request.symbol,
                                DoubleToString(request.price,digits));
            //--- Is there SL or TP?
            if(request.sl!=0.0)
              {
               tmp=StringFormat(" sl: %s",DoubleToString(request.sl,digits));
               str+=tmp;
              }
            if(request.tp!=0.0)
              {
               tmp=StringFormat(" tp: %s",DoubleToString(request.tp,digits));
               str+=tmp;
              }
            break;
            //--- market execution
            case SYMBOL_TRADE_EXECUTION_MARKET:
               str=StringFormat("market %s %s %s",
                                FormatOrderType(type,request.type),
                                DoubleToString(request.volume,2),
                                request.symbol);
            //--- Is there SL or TP?
            if(request.sl!=0.0)
              {
               tmp=StringFormat(" sl: %s",DoubleToString(request.sl,digits));
               str+=tmp;
              }
            if(request.tp!=0.0)
              {
               tmp=StringFormat(" tp: %s",DoubleToString(request.tp,digits));
               str+=tmp;
              }
            break;
           }
         break;
         //--- setting a pending order
      case TRADE_ACTION_PENDING:
         str=StringFormat("%s %s %s at %s",
                          FormatOrderType(type,request.type),
                          DoubleToString(request.volume,2),
                          request.symbol,
                          FormatOrderPrice(price,request.price,request.stoplimit,digits));
      //--- Is there SL or TP?
      if(request.sl!=0.0)
        {
         tmp=StringFormat(" sl: %s",DoubleToString(request.sl,digits));
         str+=tmp;
        }
      if(request.tp!=0.0)
        {
         tmp=StringFormat(" tp: %s",DoubleToString(request.tp,digits));
         str+=tmp;
        }
      break;

      //--- Setting SL/TP
      case TRADE_ACTION_SLTP:
         str=StringFormat("modify %s (sl: %s, tp: %s)",
                          request.symbol,
                          DoubleToString(request.sl,digits),
                          DoubleToString(request.tp,digits));
      break;

      //--- modifying a pending order
      case TRADE_ACTION_MODIFY:
         str=StringFormat("modify #%I64u at %s (sl: %s tp: %s)",
                          request.order,
                          FormatOrderPrice(price_new,request.price,request.stoplimit,digits),
                          DoubleToString(request.sl,digits),
                          DoubleToString(request.tp,digits));
      break;

      //--- deleting a pending order
      case TRADE_ACTION_REMOVE:
         str=StringFormat("cancel #%I64u",request.order);
         break;

      default:
         str="unknown action "+(string)request.action;
         break;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the result of a request to text                         |
//+------------------------------------------------------------------+
string CTrade::FormatRequestResult(string &str,const MqlTradeRequest &request,const MqlTradeResult &result) const
  {
   CSymbolInfo symbol;
//--- clean
   str="";
//--- set up
   int digits=5;
   if(request.symbol!=NULL)
     {
      if(symbol.Name(request.symbol))
         digits=symbol.Digits();
     }
//--- see the response code
   switch(result.retcode)
     {
      case TRADE_RETCODE_REQUOTE:
         str=StringFormat("requote (%s/%s)",
                          DoubleToString(result.bid,digits),
                          DoubleToString(result.ask,digits));
      break;

      case TRADE_RETCODE_DONE:
         if(request.action==TRADE_ACTION_DEAL && 
            (symbol.TradeExecution()==SYMBOL_TRADE_EXECUTION_REQUEST || 
            symbol.TradeExecution()==SYMBOL_TRADE_EXECUTION_INSTANT ||
            symbol.TradeExecution()==SYMBOL_TRADE_EXECUTION_MARKET))
            str=StringFormat("done at %s",DoubleToString(result.price,digits));
      else
         str="done";
      break;

      case TRADE_RETCODE_DONE_PARTIAL:
         if(request.action==TRADE_ACTION_DEAL && 
            (symbol.TradeExecution()==SYMBOL_TRADE_EXECUTION_REQUEST || 
            symbol.TradeExecution()==SYMBOL_TRADE_EXECUTION_INSTANT ||
            symbol.TradeExecution()==SYMBOL_TRADE_EXECUTION_MARKET))
            str=StringFormat("done partially %s at %s",
                             DoubleToString(result.volume,2),
                             DoubleToString(result.price,digits));
      else
         str=StringFormat("done partially %s",
                          DoubleToString(result.volume,2));
      break;

      case TRADE_RETCODE_REJECT            : str="rejected";                        break;
      case TRADE_RETCODE_CANCEL            : str="canceled";                        break;
      case TRADE_RETCODE_PLACED            : str="placed";                          break;
      case TRADE_RETCODE_ERROR             : str="common error";                    break;
      case TRADE_RETCODE_TIMEOUT           : str="timeout";                         break;
      case TRADE_RETCODE_INVALID           : str="invalid request";                 break;
      case TRADE_RETCODE_INVALID_VOLUME    : str="invalid volume";                  break;
      case TRADE_RETCODE_INVALID_PRICE     : str="invalid price";                   break;
      case TRADE_RETCODE_INVALID_STOPS     : str="invalid stops";                   break;
      case TRADE_RETCODE_TRADE_DISABLED    : str="trade disabled";                  break;
      case TRADE_RETCODE_MARKET_CLOSED     : str="market closed";                   break;
      case TRADE_RETCODE_NO_MONEY          : str="not enough money";                break;
      case TRADE_RETCODE_PRICE_CHANGED     : str="price changed";                   break;
      case TRADE_RETCODE_PRICE_OFF         : str="off quotes";                      break;
      case TRADE_RETCODE_INVALID_EXPIRATION: str="invalid expiration";              break;
      case TRADE_RETCODE_ORDER_CHANGED     : str="order changed";                   break;
      case TRADE_RETCODE_TOO_MANY_REQUESTS : str="too many requests";               break;
      case TRADE_RETCODE_NO_CHANGES        : str="no changes";                      break;
      case TRADE_RETCODE_SERVER_DISABLES_AT: str="auto trading disabled by server"; break;
      case TRADE_RETCODE_CLIENT_DISABLES_AT: str="auto trading disabled by client"; break;
      case TRADE_RETCODE_LOCKED            : str="locked";                          break;
      case TRADE_RETCODE_FROZEN            : str="frozen";                          break;
      case TRADE_RETCODE_INVALID_FILL      : str="invalid fill";                    break;
      case TRADE_RETCODE_CONNECTION        : str="no connection";                   break;
      case TRADE_RETCODE_ONLY_REAL         : str="only real";                       break;
      case TRADE_RETCODE_LIMIT_ORDERS      : str="limit orders";                    break;
      case TRADE_RETCODE_LIMIT_VOLUME      : str="limit volume";                    break;
      case TRADE_RETCODE_POSITION_CLOSED   : str="position closed";                 break;
      case TRADE_RETCODE_INVALID_ORDER     : str="invalid order";                   break;

      default:
         str="unknown retcode "+(string)result.retcode;
         break;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Checks if the m_request structure is filled correctly            |
//+------------------------------------------------------------------+
bool CTrade::OrderCheck(const MqlTradeRequest &request,MqlTradeCheckResult &check_result)
  {
//--- action and return the result
//   return(::OrderCheck(request,check_result));
   return(true);
  }
//+------------------------------------------------------------------+
//| Checks and corrects type of filling policy                       |
//+------------------------------------------------------------------+
bool CTrade::FillingCheck(const string symbol)
  {
//--- get execution mode of orders by symbol
/*   ENUM_SYMBOL_TRADE_EXECUTION exec=(ENUM_SYMBOL_TRADE_EXECUTION)SymbolInfoInteger(symbol,SYMBOL_TRADE_EXEMODE);
//--- check execution mode
   if(exec==SYMBOL_TRADE_EXECUTION_REQUEST || exec==SYMBOL_TRADE_EXECUTION_INSTANT)
     {
      //--- neccessary filling type will be placed automatically
      return(true);
     }
//--- get possible filling policy types by symbol
   uint filling=(uint)SymbolInfoInteger(symbol,SYMBOL_FILLING_MODE);
//--- check execution mode again
   if(exec==SYMBOL_TRADE_EXECUTION_MARKET)
     {
      //--- for the MARKET execution mode
      //--- analyze order
      if(m_request.action!=TRADE_ACTION_PENDING)
        {
         //--- in case of instant execution order
         //--- if the required filling policy is supported, add it to the request
         if(m_type_filling==ORDER_FILLING_FOK && (filling & SYMBOL_FILLING_ALL_OR_NONE)!=0)
           {
            m_request.type_filling=m_type_filling;
            return(true);
           }
         if(m_type_filling==ORDER_FILLING_IOC && (filling & SYMBOL_CANCEL_REMAIND)!=0)
           {
            m_request.type_filling=m_type_filling;
            return(true);
           }
         //--- wrong filling policy, set error code
         m_result.retcode=TRADE_RETCODE_INVALID_FILL;
         return(false);
        }
      return(true);
     }
//--- EXCHANGE execution mode
   switch(m_type_filling)
     {
      case ORDER_FILLING_FOK:
         //--- analyze order
         if(m_request.action==TRADE_ACTION_PENDING)
           {
            //--- in case of pending order
            //--- add the expiration mode to the request
            if(!ExpirationCheck(symbol))
               m_request.type_time=ORDER_TIME_DAY;
            if(m_request.type==ORDER_TYPE_BUY_STOP || m_request.type==ORDER_TYPE_SELL_STOP)
              {
               //--- in case of stop order
               //--- add the corresponding filling policy to the request
               m_request.type_filling=ORDER_FILLING_RETURN;
               return(true);
              }
            }
         //--- in case of limit order or instant execution order
         //--- if the required filling policy is supported, add it to the request
         if((filling & SYMBOL_FILLING_ALL_OR_NONE)!=0)
           {
            m_request.type_filling=m_type_filling;
            return(true);
           }
         //--- wrong filling policy, set error code
         m_result.retcode=TRADE_RETCODE_INVALID_FILL;
         return(false);
      case ORDER_FILLING_IOC:
         //--- analyze order
         if(m_request.action==TRADE_ACTION_PENDING)
           {
            //--- in case of pending order
            //--- add the expiration mode to the request
            if(!ExpirationCheck(symbol))
               m_request.type_time=ORDER_TIME_DAY;
            if(m_request.type==ORDER_TYPE_BUY_STOP || m_request.type==ORDER_TYPE_SELL_STOP)
              {
               //--- in case of stop order
               //--- add the corresponding filling policy to the request
               m_request.type_filling=ORDER_FILLING_RETURN;
               return(true);
              }
           }
         //--- in case of limit order or instant execution order
         //--- if the required filling policy is supported, add it to the request
         if((filling & SYMBOL_CANCEL_REMAIND)!=0)
           {
            m_request.type_filling=m_type_filling;
            return(true);
           }
         //--- wrong filling policy, set error code
         m_result.retcode=TRADE_RETCODE_INVALID_FILL;
         return(false);
      case ORDER_FILLING_RETURN:
         //--- add filling policy to the request
         m_request.type_filling=m_type_filling;
         return(true);
     }
//--- unknown execution mode, set error code
   m_result.retcode=TRADE_RETCODE_ERROR;
   return(false);*/
   return(true);
  }
//+------------------------------------------------------------------+
//| Check expiration type of pending order                           |
//+------------------------------------------------------------------+
bool CTrade::ExpirationCheck(const string symbol)
  {
/*   CSymbolInfo sym;
//--- check symbol
   if(!sym.Name((symbol==NULL)?Symbol():symbol))
      return(false);
//--- get flags
   int flags=sym.TradeTimeFlags();
//--- check type
   switch(m_request.type_time)
     {
      case ORDER_TIME_GTC:
         if((flags&SYMBOL_EXPIRATION_GTC)!=0)
            return(true);
         break;
      case ORDER_TIME_DAY:
         if((flags&SYMBOL_EXPIRATION_DAY)!=0)
            return(true);
         break;
      case ORDER_TIME_SPECIFIED:
         if((flags&SYMBOL_EXPIRATION_SPECIFIED)!=0)
            return(true);
         break;
      case ORDER_TIME_SPECIFIED_DAY:
         if((flags&SYMBOL_EXPIRATION_SPECIFIED_DAY)!=0)
            return(true);
         break;
      default:
         Print(__FUNCTION__+": Unknown expiration type");
         break;
     }
//--- failed
   return(false);*/
   return(true);
  }
//+------------------------------------------------------------------+
//| Checks order                                                     |
//+------------------------------------------------------------------+
bool CTrade::OrderTypeCheck(const string symbol)
  {
/*   bool res=false;   
//--- check symbol
   CSymbolInfo sym;
   if(!sym.Name((symbol==NULL)?Symbol():symbol))
      return(false);
//--- get flags of allowed trade orders
   int flags=sym.OrderMode();
//--- depending on the type of order in request
   switch(m_request.type)
     {
      case ORDER_TYPE_BUY:
      case ORDER_TYPE_SELL:
         //--- check possibility of execution
         res=((flags&SYMBOL_ORDER_MARKET)!=0);
         break;
      case ORDER_TYPE_BUY_LIMIT:
      case ORDER_TYPE_SELL_LIMIT:
         //--- check possibility of execution
         res=((flags&SYMBOL_ORDER_LIMIT)!=0);
         break;
      case ORDER_TYPE_BUY_STOP:
      case ORDER_TYPE_SELL_STOP:
         //--- check possibility of execution
         res=((flags&SYMBOL_ORDER_STOP)!=0);
         break;
      case ORDER_TYPE_BUY_STOP_LIMIT:
      case ORDER_TYPE_SELL_STOP_LIMIT:
         //--- check possibility of execution
         res=((flags&SYMBOL_ORDER_STOP_LIMIT)!=0);
         break;
      default:
         break;
     }
   if(res)
     {
      //--- trading order is valid
      //--- check if we need and able to set protective orders
      if(m_request.sl!=0.0 || m_request.tp!=0.0)
        {
         if((flags&SYMBOL_ORDER_SL)==0)
            m_request.sl=0.0;
         if((flags&SYMBOL_ORDER_TP)==0)
            m_request.tp=0.0;
        }
     }
   else
     {
      //--- trading order is not valid
      //--- set error
      m_result.retcode=TRADE_RETCODE_INVALID_ORDER;
      Print(__FUNCTION__+": Invalid order type");
     }
//--- result
   return(res);*/
   return(true);
  }
//+------------------------------------------------------------------+
//| Send order                                                       |
//+------------------------------------------------------------------+
bool CTrade::OrderSend(const MqlTradeRequest &request,MqlTradeResult &result)
  {
   bool   res=true;
   int    ticket;
   string action="";
   string fmt   ="";
//--- action
   switch(request.action)
     {
      case TRADE_ACTION_DEAL:
         ticket=OrderSend(request.symbol,request.type,request.volume,request.price,(int)request.deviation,request.sl,request.tp,request.comment,(int)request.magic);
//         res=OrderClose((int)request.order,request.volume,request.price,(int)request.deviation);
         break;
      case TRADE_ACTION_PENDING:
         ticket=OrderSend(request.symbol,request.type,request.volume,request.price,(int)request.deviation,request.sl,request.tp,request.comment,(int)request.magic,request.expiration);
         break;
      case TRADE_ACTION_SLTP:
         res=OrderModify((int)request.order,request.price,request.sl,request.tp,request.expiration);
         break;
      case TRADE_ACTION_MODIFY:
         res=OrderModify((int)request.order,request.price,request.sl,request.tp,request.expiration);
         break;
      case TRADE_ACTION_REMOVE:
         break;
      default:
         break;
     }
//--- check
   if(res)
     {
      if(m_log_level>LOG_LEVEL_ERRORS)
         printf(__FUNCTION__+": %s [%s]",FormatRequest(action,request),FormatRequestResult(fmt,request,result));
     }
   else
     {
      if(m_log_level>LOG_LEVEL_NO)
         printf(__FUNCTION__+": %s [%s]",FormatRequest(action,request),FormatRequestResult(fmt,request,result));
     }
//--- return the result
   return(res);
  }
//+------------------------------------------------------------------+
