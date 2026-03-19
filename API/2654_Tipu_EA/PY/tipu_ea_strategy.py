import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class tipu_ea_strategy(Strategy):
    """Trend following with fast/slow EMA crossover and ATR stop."""
    def __init__(self):
        super(tipu_ea_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 8).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "Signals")
        self._slow_length = self.Param("SlowLength", 21).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "Signals")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period for stop", "Risk")
        self._atr_mult = self.Param("AtrMultiplier", 1.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier for stop", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(tipu_ea_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._has_prev = False
        self._stop_price = 0

    def OnStarted(self, time):
        super(tipu_ea_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._has_prev = False
        self._stop_price = 0

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        atr_val = float(atr_val)

        # Check stop
        if self.Position > 0 and self._stop_price > 0 and float(candle.LowPrice) <= self._stop_price:
            self.SellMarket()
            self._stop_price = 0
        elif self.Position < 0 and self._stop_price > 0 and float(candle.HighPrice) >= self._stop_price:
            self.BuyMarket()
            self._stop_price = 0

        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._has_prev = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val
        atr_dist = atr_val * self._atr_mult.Value

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._stop_price = close - atr_dist if atr_dist > 0 else 0
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._stop_price = close + atr_dist if atr_dist > 0 else 0

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return tipu_ea_strategy()
