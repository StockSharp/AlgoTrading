import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vwap_ema_atr_pullback_strategy(Strategy):
    def __init__(self):
        super(vwap_ema_atr_pullback_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 30) \
            .SetDisplay("Fast EMA", "Fast EMA length", "Trend")
        self._slow_ema_length = self.Param("SlowEmaLength", 200) \
            .SetDisplay("Slow EMA", "Slow EMA length", "Trend")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Volatility")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5) \
            .SetDisplay("ATR Mult", "ATR multiplier", "Volatility")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def fast_ema_length(self):
        return self._fast_ema_length.Value

    @property
    def slow_ema_length(self):
        return self._slow_ema_length.Value

    @property
    def atr_length(self):
        return self._atr_length.Value

    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_ema_atr_pullback_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(vwap_ema_atr_pullback_strategy, self).OnStarted(time)
        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self.fast_ema_length
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self.slow_ema_length
        atr = AverageTrueRange()
        atr.Length = self.atr_length
        vwap = VolumeWeightedMovingAverage()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(vwap, ema_fast, ema_slow, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, vwap_value, ema_fast_value, ema_slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        uptrend = ema_fast_value > ema_slow_value and (ema_fast_value - ema_slow_value) > atr_value * self.atr_multiplier
        downtrend = ema_fast_value < ema_slow_value and (ema_slow_value - ema_fast_value) > atr_value * self.atr_multiplier
        long_entry = uptrend and candle.ClosePrice < vwap_value
        short_entry = downtrend and candle.ClosePrice > vwap_value
        if long_entry and self.Position <= 0:
            self.BuyMarket()
        elif short_entry and self.Position >= 0:
            self.SellMarket()
        long_target = vwap_value + atr_value * self.atr_multiplier
        short_target = vwap_value - atr_value * self.atr_multiplier
        if self.Position > 0 and candle.ClosePrice >= long_target:
            self.SellMarket()
        elif self.Position < 0 and candle.ClosePrice <= short_target:
            self.BuyMarket()

    def CreateClone(self):
        return vwap_ema_atr_pullback_strategy()
