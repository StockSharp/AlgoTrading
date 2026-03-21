import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

class gpf_tcp_pivot_limit_strategy(Strategy):
    """
    GPF TCP Pivot Limit: Donchian channel midline crossover.
    Buys when close crosses above channel midpoint.
    Sells when close crosses below channel midpoint.
    """

    def __init__(self):
        super(gpf_tcp_pivot_limit_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Channel Period", "Pivot lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 14) \
            .SetDisplay("EMA Period", "EMA filter", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gpf_tcp_pivot_limit_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(gpf_tcp_pivot_limit_strategy, self).OnStarted(time)

        highest = Highest()
        highest.Length = self._channel_period.Value
        lowest = Lowest()
        lowest.Length = self._channel_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self._process_candle).Start()

    def _process_candle(self, candle, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        h = float(highest_val)
        l = float(lowest_val)
        mid = (h + l) / 2.0

        if not self._has_prev:
            self._prev_close = close
            self._prev_mid = mid
            self._has_prev = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_close = close
            self._prev_mid = mid
            return

        if self._prev_close <= self._prev_mid and close > mid and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 2
        elif self._prev_close >= self._prev_mid and close < mid and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 2

        self._prev_close = close
        self._prev_mid = mid

    def CreateClone(self):
        return gpf_tcp_pivot_limit_strategy()
