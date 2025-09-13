#+------------------------------------------------------------------+
#|                                                  positionsget.py |
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

# получим открытые позиции на USDCHF 
positions = mt5.positions_get(symbol = "USDCHF")
if positions == None: 
   print("No positions on USDCHF, error code={}".format(mt5.last_error()))
elif len(positions) > 0:
   print("Total positions on USDCHF =", len(positions))
   # выведем все открытые позиции
   for position in positions:
      print(position)

# получим список позиций на символах, чьи имена содержат "*USD*" 
usd_positions = mt5.positions_get(group = "*USD*") 
if usd_positions == None:
   print("No positions with group=\"*USD*\", error code={}".format(mt5.last_error())) 
elif len(usd_positions) > 0: 
   print("positions_get(group=\"*USD*\") = {}".format(len(usd_positions)))
   # выведем позиции в виде таблицы с помощью pandas.DataFrame 
   df=pd.DataFrame(list(usd_positions), columns = usd_positions[0]._asdict().keys())
   df['time'] = pd.to_datetime(df['time'], unit='s')
   df.drop(['time_update', 'time_msc', 'time_update_msc', 'external_id'], axis=1, inplace=True)
   print(df)

# завершим подключение к терминалу MetaTrader 5 
mt5.shutdown()
#+------------------------------------------------------------------+
