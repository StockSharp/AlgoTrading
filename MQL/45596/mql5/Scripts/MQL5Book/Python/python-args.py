#+------------------------------------------------------------------+
#|                                                   python-args.py |
#|                                  Copyright 2022, MetaQuotes Ltd. |
#|                                             https://www.mql5.com |
#+------------------------------------------------------------------+
import MetaTrader5 as mt5
import sys

print('The command line arguments are:')
for i in sys.argv:
   print(i)

mt5.initialize()

#
# you code here
# 

mt5.shutdown()
#+------------------------------------------------------------------+
