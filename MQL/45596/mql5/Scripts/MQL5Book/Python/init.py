#+------------------------------------------------------------------+
#|                                                          init.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5 
# покажем версию пакета MetaTrader5 
print("MetaTrader5 package version: ", mt5.__version__)  #  5.0.37

# пробуем установить подключение или запустить терминал MetaTrader 5
if not mt5.initialize():
   print("initialize() failed, error code =", mt5.last_error()) 
   quit()
# рабочая часть скрипта будет здесь
# ... 
# завершаем подключение к терминалу
mt5.shutdown()
#+------------------------------------------------------------------+
