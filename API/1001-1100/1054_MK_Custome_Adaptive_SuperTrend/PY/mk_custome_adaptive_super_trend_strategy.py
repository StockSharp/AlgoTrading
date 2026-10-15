import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class mk_custome_adaptive_super_trend_strategy(Strategy):
    def __init__(self):
        super(mk_custome_adaptive_super_trend_strategy, self).__init__()
        self._atr_length = self.Param("AtrLength", 10) \
            .SetGreaterThanZero()
        self._factor = self.Param("Factor", 3.0) \
            .SetGreaterThanZero()
        self._training_period = self.Param("TrainingPeriod", 20) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._atr_history = []
        self._prev_lower_band = 0.0
        self._prev_upper_band = 0.0
        self._prev_super_trend = 0.0
        self._prev_direction = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mk_custome_adaptive_super_trend_strategy, self).OnReseted()
        self._atr_history = []
        self._prev_lower_band = 0.0
        self._prev_upper_band = 0.0
        self._prev_super_trend = 0.0
        self._prev_direction = 0

    def OnStarted2(self, time):
        super(mk_custome_adaptive_super_trend_strategy, self).OnStarted2(time)
        self._atr_history = []
        self._prev_lower_band = 0.0
        self._prev_upper_band = 0.0
        self._prev_super_trend = 0.0
        self._prev_direction = 0
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self.OnProcess).Start()

    def OnProcess(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._atr.IsFormed:
            return
        av = float(atr_value)
        tp = self._training_period.Value
        self._atr_history.append(av)
        if len(self._atr_history) > tp:
            self._atr_history.pop(0)
        if len(self._atr_history) < tp:
            return
        atr_high = max(self._atr_history)
        atr_low = min(self._atr_history)
        rng = atr_high - atr_low
        if rng <= 0.0:
            rng = av * 0.01
        high_vol = atr_low + rng * 0.75
        mid_vol = atr_low + rng * 0.5
        low_vol = atr_low + rng * 0.25
        dist_high = abs(av - high_vol)
        dist_mid = abs(av - mid_vol)
        dist_low = abs(av - low_vol)
        if dist_high < dist_mid:
            assigned = high_vol if dist_high < dist_low else low_vol
        else:
            assigned = mid_vol if dist_mid < dist_low else low_vol
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        factor = float(self._factor.Value)
        src = (high + low) / 2.0
        upper_band = src + factor * assigned
        lower_band = src - factor * assigned
        if self._prev_lower_band != 0.0 and lower_band < self._prev_lower_band and close > self._prev_lower_band:
            lower_band = self._prev_lower_band
        if self._prev_upper_band != 0.0 and upper_band > self._prev_upper_band and close < self._prev_upper_band:
            upper_band = self._prev_upper_band
        if self._prev_super_trend == 0.0:
            direction = 1 if close > src else -1
        elif self._prev_super_trend == self._prev_upper_band:
            direction = 1 if close > upper_band else -1
        else:
            direction = -1 if close < lower_band else 1
        st = lower_band if direction == 1 else upper_band
        if self._prev_direction <= 0 and direction > 0 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_direction >= 0 and direction < 0 and self.Position >= 0:
            self.SellMarket()
        self._prev_lower_band = lower_band
        self._prev_upper_band = upper_band
        self._prev_super_trend = st
        self._prev_direction = direction

    def CreateClone(self):
        return mk_custome_adaptive_super_trend_strategy()
