import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

import math

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

SHORT_LENGTH = 14
LONG_LENGTH = 46


class adaptive_hma_plus_strategy(Strategy):
    """
    Adaptive HMA Plus strategy.
    The Hull moving average period moves between MinPeriod and MaxPeriod: while the market is active (short ATR above long ATR, or
    short volume average above long volume average when UseVolume is set) it shrinks by AdaptPercent per bar, otherwise it grows by
    the same rate. During active conditions a rising HMA slope above FlatThreshold goes long and a falling slope below -FlatThreshold
    goes short; the opposite signal reverses the position.
    """

    def __init__(self):
        super(adaptive_hma_plus_strategy, self).__init__()
        self._min_period = self.Param("MinPeriod", 172).SetGreaterThanZero().SetDisplay("Min Period", "Shortest HMA period", "HMA")
        self._max_period = self.Param("MaxPeriod", 233).SetGreaterThanZero().SetDisplay("Max Period", "Longest HMA period", "HMA")
        self._adapt_percent = self.Param("AdaptPercent", 0.031).SetNotNegative().SetDisplay("Adapt Percent", "Fraction by which the period changes per bar", "HMA")
        self._flat_threshold = self.Param("FlatThreshold", 0.0).SetNotNegative().SetDisplay("Flat Threshold", "Minimum absolute HMA slope that counts as a trend", "Signals")
        self._use_volume = self.Param("UseVolume", False).SetDisplay("Use Volume", "Measure market activity by volume instead of ATR", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_short = None
        self._volume_long = None
        self._closes = []
        self._dynamic_period = 0.0
        self._prev_hma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_hma_plus_strategy, self).OnReseted()
        self._volume_short = None
        self._volume_long = None
        self._closes = []
        self._dynamic_period = 0.0
        self._prev_hma = None

    def OnStarted2(self, time):
        super(adaptive_hma_plus_strategy, self).OnStarted2(time)

        self._closes = []
        self._dynamic_period = float(self._max_period.Value)
        self._prev_hma = None

        atr_short = AverageTrueRange()
        atr_short.Length = SHORT_LENGTH
        atr_long = AverageTrueRange()
        atr_long.Length = LONG_LENGTH
        self._volume_short = SimpleMovingAverage()
        self._volume_short.Length = SHORT_LENGTH
        self._volume_long = SimpleMovingAverage()
        self._volume_long.Length = LONG_LENGTH

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr_short, atr_long, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_short_value, atr_long_value):
        if candle.State != CandleStates.Finished:
            return

        volume_short = process_float(self._volume_short, candle.TotalVolume, candle.ServerTime, True).GetValue[Decimal](None)
        volume_long = process_float(self._volume_long, candle.TotalVolume, candle.ServerTime, True).GetValue[Decimal](None)

        min_period = self._min_period.Value
        max_period = max(min_period, self._max_period.Value)
        self._closes.append(float(candle.ClosePrice))
        capacity = max_period + int(math.ceil(math.sqrt(max_period)))
        while len(self._closes) > capacity:
            self._closes.pop(0)

        if not atr_short_value.IsFormed or not atr_long_value.IsFormed or not self._volume_short.IsFormed or not self._volume_long.IsFormed:
            return

        if self._use_volume.Value:
            active = volume_short > volume_long
        else:
            active = atr_short_value.GetValue[Decimal](None) > atr_long_value.GetValue[Decimal](None)

        # Active markets shorten the period to react faster, quiet ones lengthen it.
        adapt = float(self._adapt_percent.Value)
        if active:
            self._dynamic_period = max(float(min_period), self._dynamic_period * (1.0 - adapt))
        else:
            self._dynamic_period = min(float(max_period), self._dynamic_period * (1.0 + adapt))

        current = self._calculate_hma(int(round(self._dynamic_period)))
        if current is None:
            return

        previous = self._prev_hma
        self._prev_hma = current

        if previous is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        slope = current - previous
        flat = float(self._flat_threshold.Value)

        if active and slope > flat and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif active and slope < -flat and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def _calculate_hma(self, length):
        length = max(2, length)
        half = max(1, length // 2)
        sqrt_length = max(1, int(round(math.sqrt(length))))

        if len(self._closes) < length + sqrt_length - 1:
            return None

        total = 0.0
        weight_sum = 0.0
        for i in range(sqrt_length):
            # i bars back from the latest close.
            end = len(self._closes) - 1 - i
            diff = 2.0 * self._wma(end, half) - self._wma(end, length)
            weight = sqrt_length - i
            total += diff * weight
            weight_sum += weight

        return total / weight_sum

    def _wma(self, end, length):
        total = 0.0
        weight_sum = 0.0
        for i in range(length):
            weight = length - i
            total += self._closes[end - i] * weight
            weight_sum += weight
        return total / weight_sum

    def CreateClone(self):
        return adaptive_hma_plus_strategy()
