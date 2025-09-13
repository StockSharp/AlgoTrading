#+------------------------------------------------------------------+
#|                                                   accountinfo.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5 

# установим подключение к терминалу MetaTrader 5 
if not mt5.initialize(): 
   print("initialize() failed, error code =", mt5.last_error())
   quit()

account_info = mt5.account_info()
if account_info != None:
   # выведем данные о торговом счете как есть
   print(account_info) 
   # выведем данные о торговом счете в виде словаря
   print("Show account_info()._asdict():")
   account_info_dict = mt5.account_info()._asdict()
   for prop in account_info_dict:
      print("  {}={}".format(prop, account_info_dict[prop]))

# завершим подключение к терминалу MetaTrader 5
mt5.shutdown()
#+------------------------------------------------------------------+
