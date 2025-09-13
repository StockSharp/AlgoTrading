#+------------------------------------------------------------------+
#|                                               historydealsget.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5 
from datetime import datetime 
import pandas as pd 
pd.set_option('display.max_columns', 500) # сколько столбцов показываем 
pd.set_option('display.width', 1500)      # макс. ширина таблицы для показа 

print()
# установим подключение к терминалу MetaTrader 5 
if not mt5.initialize(): 
   print("initialize() failed, error code =", mt5.last_error())
   quit() 

# получим количество сделок в истории 
from_date = datetime(2020, 1, 1)
to_date = datetime.now() 

# получим  сделки по символам, имена которых не содержат ни "EUR" ни "GBP" 
deals = mt5.history_deals_get(from_date, to_date, group="*,!*EUR*,!*GBP*") 
if deals == None: 
   print("No deals, error code={}".format(mt5.last_error()))
elif len(deals) > 0: 
   print("history_deals_get(from_date, to_date, group=\"*,!*EUR*,!*GBP*\") =", len(deals)) 
   # выведем все полученные сделки как есть 
   for deal in deals: 
      print("  ",deal) 
   print() 
   # выведем эти сделки в виде таблицы с помощью pandas.DataFrame 
   df = pd.DataFrame(list(deals), columns = deals[0]._asdict().keys()) 
   df['time'] = pd.to_datetime(df['time'], unit='s')
   df.drop(['time_msc','commission','fee'], axis = 1, inplace = True)
   print(df) 
print("") 

# завершим подключение к терминалу MetaTrader 5 
mt5.shutdown() 
#+------------------------------------------------------------------+
