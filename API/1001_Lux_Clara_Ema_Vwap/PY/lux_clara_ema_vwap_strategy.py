import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class lux_clara_ema_vwap_strategy(Strategy):
    """
    Lux Clara EMA + VWAP strategy.
    Buys on fast EMA crossing above slow EMA when above VWAP.
    """

    def __init__(self):
        super(lux_clara_ema_vwap_strategy, self).__init__()
        self._fast_length = self.Param("FastEmaLength", 8) \
            .SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_length = self.Param("SlowEmaLength", 21) \
            .SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(lux_clara_ema_vwap_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(lux_clara_ema_vwap_strategy, self).OnStarted(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self._fast_length.Value
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self._slow_length.Value
        self._vwap = VolumeWeightedMovingAverage()
        self._vwap.Length = 20

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._vwap, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawIndicator(area, self._vwap)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, vwap_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed:
            return

        f = float(fast_val)
        s = float(slow_val)
        v = float(vwap_val)
        close = float(candle.ClosePrice)

        if not self._initialized:
            self._prev_fast = f
            self._prev_slow = s
            self._initialized = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = f
            self._prev_slow = s
            return

        cross_above = self._prev_fast <= self._prev_slow and f > s
        cross_below = self._prev_fast >= self._prev_slow and f < s

        above_vwap = not self._vwap.IsFormed or close > v
        below_vwap = not self._vwap.IsFormed or close < v

        if self.Position <= 0 and cross_above and above_vwap:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown = 12
        elif self.Position >= 0 and cross_below and below_vwap:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown = 12
        elif self.Position > 0 and cross_below:
            self.SellMarket()
            self._cooldown = 12
        elif self.Position < 0 and cross_above:
            self.BuyMarket()
            self._cooldown = 12

        self._prev_fast = f
        self._prev_slow = s

    def CreateClone(self):
        return lux_clara_ema_vwap_strategy()
