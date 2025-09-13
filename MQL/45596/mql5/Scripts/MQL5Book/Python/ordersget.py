#+------------------------------------------------------------------+
#|                                                     ordersget.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5
import pandas as pd
pd.set_option('display.max_columns', 500) # сколько столбцов показываем
pd.set_option('display.width', 1500)      # макс. ширина таблицы для показа

# установим подключение к терминалу MetaTrader 5 
if not mt5.initialize(): 
   print("initialize() failed, error code =", mt5.last_error())
   quit()

# выведем информацию о действующих ордерах на символе GBPUSD  
orders = mt5.orders_get(symbol = "GBPUSD")
if orders is None or len(orders) == 0:
   print("No orders on GBPUSD, error code={}".format(mt5.last_error()))
else:
   print("Total orders on GBPUSD:", len(orders))
   # выведем все действующие ордера
   for order in orders:
      print(order)
print()

# получим список ордеров на символах, чьи имена содержат "*GBP*" 
gbp_orders = mt5.orders_get(group="*GBP*")
if gbp_orders is None or len(gbp_orders) == 0:
   print("No orders with group=\"*GBP*\", error code={}".format(mt5.last_error()))
else: 
   print("orders_get(group=\"*GBP*\")={}".format(len(gbp_orders)))
   # выведем ордера в виде таблицы с помощью pandas.DataFrame
   df = pd.DataFrame(list(gbp_orders), columns = gbp_orders[0]._asdict().keys())
   df.drop(['time_done', 'time_done_msc', 'position_id', 'position_by_id', 'reason', 'volume_initial', 'price_stoplimit'], axis = 1, inplace = True)
   df['time_setup'] = pd.to_datetime(df['time_setup'], unit = 's')
   print(df)

# завершим подключение к терминалу MetaTrader 5 
mt5.shutdown()
#+------------------------------------------------------------------+
