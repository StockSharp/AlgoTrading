import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class multi_breakout_v001k_strategy(Strategy):
    """Multi-breakout strategy using Highest/Lowest channel midpoint crossover.
    Buys when price crosses above midpoint, sells when below."""

    def __init__(self):
        super(multi_breakout_v001k_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Channel Period", "Lookback for breakout channel", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def ChannelPeriod(self):
        return self._channel_period.Value

    def OnReseted(self):
        super(multi_breakout_v001k_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(multi_breakout_v001k_strategy, self).OnStarted2(time)

        self._has_prev = False

        highest = Highest()
        highest.Length = self.ChannelPeriod
        lowest = Lowest()
        lowest.Length = self.ChannelPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self._process_candle).Start()

    def _process_candle(self, candle, high, low):
        if candle.State != CandleStates.Finished:
            return

        high_val = float(high)
        low_val = float(low)
        close = float(candle.ClosePrice)
        mid = (high_val + low_val) / 2.0

        if not self._has_prev:
            self._prev_close = close
            self._prev_mid = mid
            self._has_prev = True
            return

        # Cross above midpoint - go long
        if self._prev_close <= self._prev_mid and close > mid and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Cross below midpoint - go short
        elif self._prev_close >= self._prev_mid and close < mid and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_close = close
        self._prev_mid = mid

    def CreateClone(self):
        return multi_breakout_v001k_strategy()
