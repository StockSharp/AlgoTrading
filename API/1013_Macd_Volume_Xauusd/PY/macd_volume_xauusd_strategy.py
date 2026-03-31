import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, MovingAverageConvergenceDivergence,
    DecimalIndicatorValue, CandleIndicatorValue,
)
from StockSharp.Algo.Strategies import Strategy


class macd_volume_xauusd_strategy(Strategy):
    def __init__(self):
        super(macd_volume_xauusd_strategy, self).__init__()
        self._short_length = self.Param("ShortLength", 5)
        self._long_length = self.Param("LongLength", 10)
        self._cooldown_bars = self.Param("CooldownBars", 2)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._prev_macd = 0.0
        self._prev_macd_set = False
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(macd_volume_xauusd_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_macd_set = False
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(macd_volume_xauusd_strategy, self).OnStarted2(time)
        self._short_vol_ema = ExponentialMovingAverage()
        self._short_vol_ema.Length = self._short_length.Value
        self._long_vol_ema = ExponentialMovingAverage()
        self._long_vol_ema.Length = self._long_length.Value
        self._macd = MovingAverageConvergenceDivergence()
        self._prev_macd = 0.0
        self._prev_macd_set = False
        self._bars_from_signal = 0
        dummy1 = ExponentialMovingAverage()
        dummy1.Length = 10
        dummy2 = ExponentialMovingAverage()
        dummy2.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(dummy1, dummy2, self._process_candle).Start()

    def _process_candle(self, candle, d1, d2):
        if candle.State != CandleStates.Finished:
            return
        t = candle.ServerTime
        self._short_vol_ema.Process(DecimalIndicatorValue(self._short_vol_ema, candle.TotalVolume, t))
        self._long_vol_ema.Process(DecimalIndicatorValue(self._long_vol_ema, candle.TotalVolume, t))
        macd_result = self._macd.Process(CandleIndicatorValue(self._macd, candle))
        if not self._macd.IsFormed:
            return
        macd_val = 0.0 if macd_result.IsEmpty else float(macd_result)
        if not self._prev_macd_set:
            self._prev_macd = macd_val
            self._prev_macd_set = True
            return
        long_signal = self._prev_macd <= 0 and macd_val > 0
        short_signal = self._prev_macd >= 0 and macd_val < 0
        if self._bars_from_signal < 10000:
            self._bars_from_signal += 1
        can_signal = self._bars_from_signal >= self._cooldown_bars.Value
        if can_signal and long_signal and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif can_signal and short_signal and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0
        self._prev_macd = macd_val

    def CreateClone(self):
        return macd_volume_xauusd_strategy()
