#+------------------------------------------------------------------+
#|                                                  terminalinfo.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5 

# установим подключение к терминалу MetaTrader 5
if not mt5.initialize():
   print("initialize() failed, error code =", mt5.last_error())
   quit() 

# выведем краткую информацию о версии MetaTrader 5
print(mt5.version()) 
# выведем полную информацию о настройках и состоянии терминала
terminal_info = mt5.terminal_info()
if terminal_info != None: 
   # выведем данные о терминале как есть
   print(terminal_info) 
   # выведем данные в виде словаря
   print("Show terminal_info()._asdict():")
   terminal_info_dict = mt5.terminal_info()._asdict()
   for prop in terminal_info_dict: 
      print("  {}={}".format(prop, terminal_info_dict[prop]))

# завершим подключение к терминалу MetaTrader 5
mt5.shutdown() 
#+------------------------------------------------------------------+
