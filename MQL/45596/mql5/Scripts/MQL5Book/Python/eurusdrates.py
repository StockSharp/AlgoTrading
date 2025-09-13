#+------------------------------------------------------------------+
#|                                                   eurusdrates.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
from datetime import datetime
import MetaTrader5 as mt5
# импортируем модуль pytz для работы с таймзоной
import pytz

# установим подключение к терминалу MetaTrader 5
if not mt5.initialize():
   print("initialize() failed, error code =", mt5.last_error())
   mt5.shutdown()
   quit()

# установим таймзону в UTC 
timezone = pytz.timezone("Etc/UTC")

# создадим объект datetime в таймзоне UTC, чтобы не применялось смещение локальной таймзоны
utc_from = datetime(2022, 1, 10, tzinfo = timezone)

# получим 10 баров с EURUSD H1 начиная с 01.10.2022 в таймзоне UTC
rates = mt5.copy_rates_from("EURUSD", mt5.TIMEFRAME_H1, utc_from, 10)

# завершим подключение к терминалу MetaTrader 5
mt5.shutdown()

# выведем каждый элемент полученных данных (кортеж)
for rate in rates:
   print(rate)
#+------------------------------------------------------------------+
