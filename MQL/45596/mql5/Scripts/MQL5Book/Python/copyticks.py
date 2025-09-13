#+------------------------------------------------------------------+
#|                                                     copyticks.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5
import pandas as pd
import pytz
from datetime import datetime

# подключаемся к терминалу
if not mt5.initialize():
   print("initialize() failed, error code =", mt5.last_error())
   quit()

# зададим имя файла для сохранения в "песочницу"
path = mt5.terminal_info().data_path + r'\MQL5\Files\MQL5Book\copyticks.html'

# копируем 1000 тиков EURUSD с конкретного момента в истории
utc = pytz.timezone("Etc/UTC") 
rates = mt5.copy_ticks_from("EURUSD", datetime(2022, 5, 25, 1, 15, tzinfo = utc), 1000, mt5.COPY_TICKS_ALL)
bid = [x['bid'] for x in rates]
ask = [x['ask'] for x in rates]
time = [x['time'] for x in rates]
time = pd.to_datetime(time, unit = 's')

# завершим подключение к терминалу
mt5.shutdown()

# подключаем графический пакет и рисуем 2 ряда цен ask и bid в веб-странице
import plotly.graph_objs as go
from plotly.offline import download_plotlyjs, init_notebook_mode, plot, iplot
data = [go.Scatter(x = time, y = bid), go.Scatter(x = time, y = ask)]
plot(data, filename = path)
#+------------------------------------------------------------------+
