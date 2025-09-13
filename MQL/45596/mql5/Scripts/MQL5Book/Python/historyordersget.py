#+------------------------------------------------------------------+
#|                                              historyordersget.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
from datetime import datetime 
import MetaTrader5 as mt5 
import pandas as pd 
pd.set_option('display.max_columns', 500) # сколько столбцов показываем 
pd.set_option('display.width', 1500)      # макс. ширина таблицы для показа 
print()

# установим подключение к терминалу MetaTrader 5 
if not mt5.initialize(): 
   print("initialize() failed, error code =", mt5.last_error())
   quit()

# получим количество ордеров в истории (всего и по *GBP*)
from_date = datetime(2022, 9, 1)
to_date = datetime.now()
total = mt5.history_orders_total(from_date, to_date)
history_orders=mt5.history_orders_get(from_date, to_date, group="*GBP*")
# print(history_orders)
if history_orders == None: 
   print("No history orders with group=\"*GBP*\", error code={}".format(mt5.last_error())) 
else :
   print("history_orders_get({}, {}, group=\"*GBP*\")={} of total {}".format(from_date, to_date, len(history_orders), total))

# выведем все отмененные исторические ордера по тикету позиции 0
position_id = 0
position_history_orders = mt5.history_orders_get(position = position_id)
if position_history_orders == None:
   print("No orders with position #{}".format(position_id))
   print("error code =", mt5.last_error())
elif len(position_history_orders) > 0:
   print("Total history orders on position #{}: {}".format(position_id,len(position_history_orders)))
   # выведем полученные ордера как есть
   for position_order in position_history_orders:
      print(position_order)
   print()
   # выведем эти ордера в виде таблицы с помощью pandas.DataFrame
   df = pd.DataFrame(list(position_history_orders), columns = position_history_orders[0]._asdict().keys())
   df.drop(['time_expiration','type_time','state','position_by_id','reason','volume_current','price_stoplimit','sl','tp', 'time_setup_msc', 'time_done_msc', 'type_filling', 'external_id'], axis = 1, inplace = True)
   df['time_setup'] = pd.to_datetime(df['time_setup'], unit='s')
   df['time_done'] = pd.to_datetime(df['time_done'], unit='s')
   print(df)

# завершим подключение к терминалу MetaTrader 5 
mt5.shutdown()
#+------------------------------------------------------------------+
