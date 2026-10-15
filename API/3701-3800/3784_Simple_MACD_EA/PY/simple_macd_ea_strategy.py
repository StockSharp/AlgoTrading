import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class simple_macd_ea_strategy(Strategy):
    """Simple MACD EA using fast/slow EMA difference for trend detection.
    Buy when EMA difference crosses above zero, sell when it crosses below zero."""

    def __init__(self):
        super(simple_macd_ea_strategy, self).__init__()

        self._fast_ema_period = self.Param("FastEmaPeriod", 12) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_diff = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    def OnReseted(self):
        super(simple_macd_ea_strategy, self).OnReseted()
        self._prev_diff = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(simple_macd_ea_strategy, self).OnStarted2(time)

        self._has_prev = False

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastEmaPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowEmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self._process_candle).Start()

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        diff = float(fast) - float(slow)

        if not self._has_prev:
            self._prev_diff = diff
            self._has_prev = True
            return

        # Buy: MACD crosses above zero
        long_signal = self._prev_diff <= 0 and diff > 0
        # Sell: MACD crosses below zero
        short_signal = self._prev_diff >= 0 and diff < 0

        if self.Position <= 0 and long_signal:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self.Position >= 0 and short_signal:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_diff = diff

    def CreateClone(self):
        return simple_macd_ea_strategy()
