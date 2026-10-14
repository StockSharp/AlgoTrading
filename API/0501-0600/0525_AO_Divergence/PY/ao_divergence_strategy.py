import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class ao_divergence_strategy(Strategy):
    """
    AO divergence strategy.
    The Awesome Oscillator is the difference of a FastLength and a SlowLength moving average (SMA, or EMA when UseEma is set) of
    the median price. Oscillator swing lows and highs are confirmed as pivots with Lookback bars on each side. A pivot low where
    price made a lower low than at the previous pivot low while AO made a higher low is a bullish divergence and goes long; a
    pivot high where price made a higher high while AO made a lower high is a bearish divergence and goes short. The opposite
    divergence reverses the position.
    """

    def __init__(self):
        super(ao_divergence_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Length", "Fast moving average period of AO", "Indicator")
        self._slow_length = self.Param("SlowLength", 34) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Length", "Slow moving average period of AO", "Indicator")
        self._lookback = self.Param("Lookback", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback", "Bars on each side that confirm an AO pivot", "Divergence")
        self._use_ema = self.Param("UseEma", False) \
            .SetDisplay("Use EMA", "Use EMA instead of SMA for AO", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._fast_ma = None
        self._slow_ma = None
        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._bars = []
        self._last_pivot_low = None
        self._last_pivot_high = None

    def OnReseted(self):
        super(ao_divergence_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._reset_state()

    def _create_ma(self, length):
        ma = ExponentialMovingAverage() if self._use_ema.Value else SimpleMovingAverage()
        ma.Length = length
        return ma

    def OnStarted2(self, time):
        super(ao_divergence_strategy, self).OnStarted2(time)

        self._reset_state()

        self._fast_ma = self._create_ma(self._fast_length.Value)
        self._slow_ma = self._create_ma(self._slow_length.Value)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        fast = float(to_decimal(process_float(self._fast_ma, median, candle.ServerTime, True)))
        slow = float(to_decimal(process_float(self._slow_ma, median, candle.ServerTime, True)))

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed:
            return

        self._bars.append((fast - slow, float(candle.LowPrice), float(candle.HighPrice)))

        lookback = self._lookback.Value
        window = 2 * lookback + 1
        if len(self._bars) > window:
            self._bars.pop(0)

        if len(self._bars) < window:
            return

        # The middle bar is an AO pivot once Lookback bars on each side have finished.
        pivot_ao, pivot_low, pivot_high = self._bars[lookback]
        is_pivot_low = True
        is_pivot_high = True

        for i in range(window):
            if i == lookback:
                continue
            if self._bars[i][0] <= pivot_ao:
                is_pivot_low = False
            if self._bars[i][0] >= pivot_ao:
                is_pivot_high = False

        bullish = False
        bearish = False

        if is_pivot_low:
            if self._last_pivot_low is not None:
                prev_ao, prev_low = self._last_pivot_low
                bullish = pivot_low < prev_low and pivot_ao > prev_ao
            self._last_pivot_low = (pivot_ao, pivot_low)

        if is_pivot_high:
            if self._last_pivot_high is not None:
                prev_ao, prev_high = self._last_pivot_high
                bearish = pivot_high > prev_high and pivot_ao < prev_ao
            self._last_pivot_high = (pivot_ao, pivot_high)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if bullish and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif bearish and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ao_divergence_strategy()
