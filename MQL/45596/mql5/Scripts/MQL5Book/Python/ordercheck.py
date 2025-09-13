#+------------------------------------------------------------------+
#|                                                    ordercheck.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5

# установим подключение к терминалу MetaTrader 5
if not mt5.initialize():
   print("initialize() failed, error code =", mt5.last_error())
   quit()

# получим валюту счета для информации
account_currency=mt5.account_info().currency
print("Account сurrency:", account_currency)

# получим необходимые свойства символа сделки
symbol = "USDJPY"
symbol_info = mt5.symbol_info(symbol)
if symbol_info is None:
   print(symbol, "not found, can not call order_check()")
   mt5.shutdown()
   quit()

point = mt5.symbol_info(symbol).point
# если символ недоступен в Обзоре рынка, добавим его
if not symbol_info.visible:
   print(symbol, "is not visible, trying to switch on")
   if not mt5.symbol_select(symbol, True):
      print("symbol_select({}) failed, exit", symbol)
      mt5.shutdown()
      quit()

# подготовим структуру запроса как словарь
request = \
{
   "action": mt5.TRADE_ACTION_DEAL,
   "symbol": symbol,
   "volume": 1.0,
   "type": mt5.ORDER_TYPE_BUY,
   "price": mt5.symbol_info_tick(symbol).ask,
   "sl": mt5.symbol_info_tick(symbol).ask - 100 * point,
   "tp": mt5.symbol_info_tick(symbol).ask + 100 * point,
   "deviation": 10,
   "magic": 234000,
   "comment": "python script",
   "type_time": mt5.ORDER_TIME_GTC,
   "type_filling": mt5.ORDER_FILLING_RETURN,
}

# выполним проверку и выведем результат как есть 
result = mt5.order_check(request)
print(result)
print(type(result))

# преобразуем результат в словарь и выведем поэлементно
result_dict = result._asdict()
for field in result_dict.keys():
   print("   {}={}".format(field, result_dict[field]))
   # если это структура торгового запроса, то выведем её тоже поэлементно
   if field == "request":
      traderequest_dict = result_dict[field]._asdict()
      for tradereq_filed in traderequest_dict:
         print("       traderequest: {}={}".format(tradereq_filed, traderequest_dict[tradereq_filed]))

# завершим подключение к терминалу
mt5.shutdown()
#+------------------------------------------------------------------+
