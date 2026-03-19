import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adx_macd_deev_strategy(Strategy):
    """
    ADX MACD Deev: dual EMA crossover with stop-loss/take-profit in price steps.
    """

    def __init__(self):
        super(adx_macd_deev_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicator")

        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Period", "Slow EMA period", "Indicator")

        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Stop Loss", "Stop-loss in price steps", "Risk")

        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Take Profit", "Take-profit in price steps", "Risk")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    def OnReseted(self):
        super(adx_macd_deev_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(adx_macd_deev_strategy, self).OnStarted(time)

        fast = ExponentialMovingAverage()
        fast.Length = self.FastPeriod
        slow = ExponentialMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(tf(5))
        subscription.Bind(fast, slow, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast_value
            self._prev_slow = slow_value
            return

        close = float(candle.ClosePrice)
        step = 1.0

        if self.Position > 0 and self._entry_price > 0:
            if self.StopLossPoints > 0 and close <= self._entry_price - self.StopLossPoints * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                self._prev_fast = fast_value
                self._prev_slow = slow_value
                return
            if self.TakeProfitPoints > 0 and close >= self._entry_price + self.TakeProfitPoints * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                self._prev_fast = fast_value
                self._prev_slow = slow_value
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self.StopLossPoints > 0 and close >= self._entry_price + self.StopLossPoints * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                self._prev_fast = fast_value
                self._prev_slow = slow_value
                return
            if self.TakeProfitPoints > 0 and close <= self._entry_price - self.TakeProfitPoints * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 100
                self._prev_fast = fast_value
                self._prev_slow = slow_value
                return

        if self._prev_fast <= self._prev_slow and fast_value > slow_value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 100
        elif self._prev_fast >= self._prev_slow and fast_value < slow_value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 100

        self._prev_fast = fast_value
        self._prev_slow = slow_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_macd_deev_strategy()
