#+------------------------------------------------------------------+
#|                                                        eurjpy.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5

# установим подключение к терминалу MetaTrader 5
if not mt5.initialize():
   print("initialize() failed, error code =", mt5.last_error())
   quit()

# убедимся, что EURJPY присутствует в Обзоре рынка, или прерываем алгоритм
selected = mt5.symbol_select("EURJPY", True)
if not selected:
   print("Failed to select EURJPY")
   mt5.shutdown()
   quit()

# выведем свойства символа EURJPY
symbol_info = mt5.symbol_info("EURJPY")
if symbol_info != None:
   # выведем данные как есть (как кортеж)
   print(symbol_info)
   # выведем пару конкретных свойств
   print("EURJPY: spread =", symbol_info.spread, ", digits =", symbol_info.digits)
   # выведем свойства символа в виде словаря
   print("Show symbol_info(\"EURJPY\")._asdict():")
   symbol_info_dict = mt5.symbol_info("EURJPY")._asdict()
   for prop in symbol_info_dict:
      print("  {}={}".format(prop, symbol_info_dict[prop]))

# завершим подключение к терминалу MetaTrader 5
mt5.shutdown()
#+------------------------------------------------------------------+
