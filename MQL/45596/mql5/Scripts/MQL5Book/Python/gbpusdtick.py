#+------------------------------------------------------------------+
#|                                                    gbpusdtick.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5

# установим подключение к терминалу MetaTrader 5
if not mt5.initialize():
   print("initialize() failed, error code =", mt5.last_error())
   quit()

# попробуем включить символ GBPUSD в Обзоре рынка
selected=mt5.symbol_select("GBPUSD", True)
if not selected:
   print("Failed to select GBPUSD")
   mt5.shutdown()
   quit()

# выведем последний тик по символу GBPUSD в виде кортежа
lasttick = mt5.symbol_info_tick("GBPUSD")
print(lasttick)
# выведем значения полей тика в виде словаря
print("Show symbol_info_tick(\"GBPUSD\")._asdict():")
symbol_info_tick_dict = lasttick._asdict()
for prop in symbol_info_tick_dict:
   print("  {}={}".format(prop, symbol_info_tick_dict[prop]))

# завершим подключение к терминалу MetaTrader 5
mt5.shutdown()
#+------------------------------------------------------------------+
