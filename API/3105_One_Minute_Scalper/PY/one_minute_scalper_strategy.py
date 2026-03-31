import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class one_minute_scalper_strategy(Strategy):
    def __init__(self):
        super(one_minute_scalper_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 20).SetGreaterThanZero().SetDisplay("Fast Period", "Fast WMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 85).SetGreaterThanZero().SetDisplay("Slow Period", "Slow WMA period", "Indicator")
        self._sl_points = self.Param("StopLossPoints", 200).SetNotNegative().SetDisplay("Stop Loss", "Stop-loss in price steps", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 500).SetNotNegative().SetDisplay("Take Profit", "Take-profit in price steps", "Risk")

    def OnReseted(self):
        super(one_minute_scalper_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(one_minute_scalper_strategy, self).OnStarted2(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0
        self._cooldown = 0

        self._fast = WeightedMovingAverage()
        self._fast.Length = self._fast_period.Value
        self._slow = WeightedMovingAverage()
        self._slow.Length = self._slow_period.Value

        sub = self.SubscribeCandles(tf(5))
        sub.Bind(self._fast, self._slow, self.OnProcess).Start()

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast.IsFormed or not self._slow.IsFormed:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        close = candle.ClosePrice
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            step = float(self.Security.PriceStep)

        # Check SL/TP
        if self.Position > 0 and self._entry_price > 0:
            if self._sl_points.Value > 0 and close <= self._entry_price - self._sl_points.Value * step:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown = 60
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
            if self._tp_points.Value > 0 and close >= self._entry_price + self._tp_points.Value * step:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown = 60
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self._sl_points.Value > 0 and close >= self._entry_price + self._sl_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown = 60
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
            if self._tp_points.Value > 0 and close <= self._entry_price - self._tp_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown = 60
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return

        # WMA crossover
        if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 60
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 60

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return one_minute_scalper_strategy()
