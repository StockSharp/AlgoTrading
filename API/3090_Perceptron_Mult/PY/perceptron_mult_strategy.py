import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class perceptron_mult_strategy(Strategy):
    def __init__(self):
        super(perceptron_mult_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 50).SetGreaterThanZero().SetDisplay("Fast Period", "Fast EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 200).SetGreaterThanZero().SetDisplay("Slow Period", "Slow EMA period", "Indicator")
        self._sl_points = self.Param("StopLossPoints", 100).SetNotNegative().SetDisplay("Stop Loss", "Stop-loss in price steps", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 200).SetNotNegative().SetDisplay("Take Profit", "Take-profit in price steps", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(perceptron_mult_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0

    def OnStarted(self, time):
        super(perceptron_mult_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, self.OnProcess).Start()

    def _get_step(self):
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            return float(self.Security.PriceStep)
        return 1.0

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        close = candle.ClosePrice
        step = self._get_step()

        if self.Position > 0 and self._entry_price > 0:
            if self._sl_points.Value > 0 and close <= self._entry_price - self._sl_points.Value * step:
                self.SellMarket()
                self._entry_price = 0
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
            if self._tp_points.Value > 0 and close >= self._entry_price + self._tp_points.Value * step:
                self.SellMarket()
                self._entry_price = 0
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self._sl_points.Value > 0 and close >= self._entry_price + self._sl_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return
            if self._tp_points.Value > 0 and close <= self._entry_price - self._tp_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0
                self._prev_fast = fast_val
                self._prev_slow = slow_val
                return

        if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return perceptron_mult_strategy()
