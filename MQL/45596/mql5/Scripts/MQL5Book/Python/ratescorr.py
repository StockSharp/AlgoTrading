#+------------------------------------------------------------------+
#|                                                     ratescorr.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5
import pandas as pd              # подключаем модуль pandas для вывода данных
import matplotlib.pyplot as plt  # подключаем модуль matplotlib для рисования

# установим подключение к терминалу MetaTrader 5
if not mt5.initialize():
   print("initialize() failed, error code =", mt5.last_error())
   mt5.shutdown()
   quit()

# создадим путь в "песочнице" для картинки с результатом
image = mt5.terminal_info().data_path + r'\MQL5\Files\MQL5Book\ratescorr'

# список рабочих валют для расчета корреляции
sym = ['EURUSD','GBPUSD','USDJPY','USDCHF','AUDUSD','USDCAD','NZDUSD','XAUUSD']

# копируем цены закрытия баров в структуры DataFrame
d = pd.DataFrame()
for i in sym:        # для каждого символа по 1000 последних баров M1
   rates = mt5.copy_rates_from_pos(i, mt5.TIMEFRAME_M1, 0, 1000)
   d[i] = [y['close'] for y in rates]

# завершим подключение к терминалу MetaTrader 5
mt5.shutdown()

# вычислим изменение цен в процентах
rets = d.pct_change()

# вычислим корреляции
corr = rets.corr()

# рисуем корреляционную матрицу
fig = plt.figure(figsize = (5, 5))
fig.add_axes([0.15, 0.1, 0.8, 0.8])
plt.imshow(corr, cmap = 'RdYlGn', interpolation = 'none', aspect = 'equal')
plt.colorbar()
plt.xticks(range(len(corr)), corr.columns, rotation = 'vertical')
plt.yticks(range(len(corr)), corr.columns)
# покажем диграмму корреляции
plt.show()         
# сохранение в файл для просмотра после запуска 
plt.savefig(image) 
#+------------------------------------------------------------------+
