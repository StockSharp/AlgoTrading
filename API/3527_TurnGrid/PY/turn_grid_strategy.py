import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class turn_grid_strategy(Strategy):
    """Simplified grid: SMA crossover (10/30) for direction with alternating trades."""
    def __init__(self):
        super(turn_grid_strategy, self).__init__()
        self._grid_dist = self.Param("GridDistance", 0.01).SetDisplay("Grid Distance", "Relative distance between levels", "Grid")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Candle type", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(turn_grid_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._last_trade_price = 0

    def OnStarted(self, time):
        super(turn_grid_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._last_trade_price = 0

        fast = SimpleMovingAverage()
        fast.Length = 10
        slow = SimpleMovingAverage()
        slow.Length = 30

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        slow = float(slow_val)
        close = float(candle.ClosePrice)
        dist = self._grid_dist.Value

        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fast
            self._prev_slow = slow
            self._last_trade_price = close
            return

        # Grid re-entry check
        if self._last_trade_price > 0 and dist > 0:
            price_move = abs(close - self._last_trade_price) / self._last_trade_price
            if price_move < dist:
                self._prev_fast = fast
                self._prev_slow = slow
                return

        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._last_trade_price = close
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._last_trade_price = close

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return turn_grid_strategy()
