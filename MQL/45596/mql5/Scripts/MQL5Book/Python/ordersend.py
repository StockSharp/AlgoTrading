#+------------------------------------------------------------------+
#|                                                     ordersend.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import time 
import MetaTrader5 as mt5 

# установим подключение к терминалу MetaTrader 5
if not mt5.initialize():
   print("initialize() failed, error code =", mt5.last_error())
   quit()

# назначим свойства рабочего символа
symbol = "USDJPY"
symbol_info = mt5.symbol_info(symbol)
if symbol_info is None:
   print(symbol, "not found, can not trade")
   mt5.shutdown()
   quit()

# если символ недоступен в Обзоре рынка, добавим его
if not symbol_info.visible:
   print(symbol, "is not visible, trying to switch on")
   if not mt5.symbol_select(symbol, True):
      print("symbol_select({}) failed, exit", symbol)
      mt5.shutdown()
      quit()

# подготовим структуру запроса для покупки
lot = 0.1
point = mt5.symbol_info(symbol).point
price = mt5.symbol_info_tick(symbol).ask
deviation = 20
request = \
{
   "action": mt5.TRADE_ACTION_DEAL, 
   "symbol": symbol, 
   "volume": lot, 
   "type": mt5.ORDER_TYPE_BUY, 
   "price": price, 
   "sl": price - 100 * point, 
   "tp": price + 100 * point, 
   "deviation": deviation, 
   "magic": 234000, 
   "comment": "python script open", 
   "type_time": mt5.ORDER_TIME_GTC, 
   "type_filling": mt5.ORDER_FILLING_RETURN, 
}

# отправим торговый запрос
result = mt5.order_send(request)
# проверим результат выполнения 
print("1. order_send(): by {} {} lots at {}".format(symbol, lot, price));
if result.retcode != mt5.TRADE_RETCODE_DONE:
   print("2. order_send failed, retcode={}".format(result.retcode))
   # запросим результат в виде словаря и выведем поэлементно 
   result_dict = result._asdict()
   for field in result_dict.keys():
      print("   {}={}".format(field, result_dict[field]))
      # если это структура торгового запроса, то выведем её тоже поэлементно 
      if field == "request":
         traderequest_dict = result_dict[field]._asdict()
         for tradereq_filed in traderequest_dict: 
            print("       traderequest: {}={}".format(tradereq_filed, traderequest_dict[tradereq_filed]))
   print("shutdown() and quit")
   mt5.shutdown()
   quit()

print("2. order_send done, ", result)
print("   opened position with POSITION_TICKET={}".format(result.order))
print("   sleep 2 seconds before closing position #{}".format(result.order))
time.sleep(2)
# создадим запрос на закрытие
position_id = result.order
price = mt5.symbol_info_tick(symbol).bid
request = \
{
   "action": mt5.TRADE_ACTION_DEAL, 
   "symbol": symbol, 
   "volume": lot, 
   "type": mt5.ORDER_TYPE_SELL, 
   "position": position_id, 
   "price": price, 
   "deviation": deviation, 
   "magic": 234000, 
   "comment": "python script close", 
   "type_time": mt5.ORDER_TIME_GTC, 
   "type_filling": mt5.ORDER_FILLING_RETURN, 
} 
# отправим торговый запрос 
result = mt5.order_send(request)
# проверим результат выполнения 
print("3. close position #{}: sell {} {} lots at {}".format(position_id, symbol, lot, price));
if result.retcode != mt5.TRADE_RETCODE_DONE:
   print("4. order_send failed, retcode={}".format(result.retcode))
   print("   result", result)
else: 
   print("4. position #{} closed, {}".format(position_id, result))
   # запросим результат в виде словаря и выведем поэлементно
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
