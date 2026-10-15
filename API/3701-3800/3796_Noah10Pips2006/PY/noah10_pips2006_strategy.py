import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class noah10_pips2006_strategy(Strategy):
    """Session breakout strategy. Tracks channel high/low/midpoint.
    Buys on breakout above channel high or cross above midpoint.
    Sells on breakout below channel low or cross below midpoint."""

    def __init__(self):
        super(noah10_pips2006_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 24) \
            .SetDisplay("Channel Period", "Lookback for high/low channel", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_mid = 0.0
        self._prev_close = 0.0
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
        super(noah10_pips2006_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_mid = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(noah10_pips2006_strategy, self).OnStarted2(time)

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
            self._prev_high = high_val
            self._prev_low = low_val
            self._prev_mid = mid
            self._prev_close = close
            self._has_prev = True
            return

        # Breakout above channel high - buy
        if self._prev_close <= self._prev_high and close > high_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Breakout below channel low - sell
        elif self._prev_close >= self._prev_low and close < low_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Cross above midpoint from below - buy
        elif self._prev_close <= self._prev_mid and close > mid and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Cross below midpoint from above - sell
        elif self._prev_close >= self._prev_mid and close < mid and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_high = high_val
        self._prev_low = low_val
        self._prev_mid = mid
        self._prev_close = close

    def CreateClone(self):
        return noah10_pips2006_strategy()
