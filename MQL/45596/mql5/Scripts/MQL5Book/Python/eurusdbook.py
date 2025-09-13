#+------------------------------------------------------------------+
#|                                                    eurusdbook.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5 
import time               # подключаем пакет для паузы

# установим подключение к терминалу MetaTrader 5
if not mt5.initialize():
   print("initialize() failed, error code =", mt5.last_error())
   mt5.shutdown()
   quit()

# подпишемся на получение обновлений стакана по символу EURUSD
if mt5.market_book_add('EURUSD'):
   # запустим 10 раз цикл для чтения данных из стакана
   for i in range(10):
      # получим содержимое стакана
      items = mt5.market_book_get('EURUSD')
      # выведем весь стакан одной строкой как есть
      print(items)
      # теперь выведем каждый ценовой уровень отдельно в виде словаря, для наглядности
      for it in items or []:
         print(it._asdict())
      # сделаем паузу в 5 секунд перед следующим запросом данных из стакана
      time.sleep(5) 
   # отменим подписку на изменения стакана
   mt5.market_book_release('EURUSD')
else:
   print("mt5.market_book_add('EURUSD') failed, error code =", mt5.last_error())

# завершим подключение к терминалу MetaTrader 5
mt5.shutdown()
#+------------------------------------------------------------------+
