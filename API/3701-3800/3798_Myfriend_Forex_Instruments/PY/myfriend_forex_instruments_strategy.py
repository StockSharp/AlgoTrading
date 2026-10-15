import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class myfriend_forex_instruments_strategy(Strategy):
    """Donchian channel breakout with SMA momentum filter.
    Buys when close breaks above upper Donchian and fast SMA > slow SMA.
    Sells when close breaks below lower Donchian and fast SMA < slow SMA."""

    def __init__(self):
        super(myfriend_forex_instruments_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 16) \
            .SetDisplay("Channel Period", "Donchian channel period", "Indicators")
        self._fast_period = self.Param("FastPeriod", 3) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 9) \
            .SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
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

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    def OnReseted(self):
        super(myfriend_forex_instruments_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(myfriend_forex_instruments_strategy, self).OnStarted2(time)

        self._has_prev = False

        highest = Highest()
        highest.Length = self.ChannelPeriod
        lowest = Lowest()
        lowest.Length = self.ChannelPeriod
        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self.FastPeriod
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, fast_sma, slow_sma, self._process_candle).Start()

    def _process_candle(self, candle, high, low, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        high_val = float(high)
        low_val = float(low)
        fast_val = float(fast)
        slow_val = float(slow)
        close = float(candle.ClosePrice)

        if not self._has_prev:
            self._prev_close = close
            self._prev_upper = high_val
            self._prev_lower = low_val
            self._has_prev = True
            return

        # Breakout above channel with bullish momentum
        if self._prev_close <= self._prev_upper and close > high_val and fast_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Breakout below channel with bearish momentum
        elif self._prev_close >= self._prev_lower and close < low_val and fast_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        else:
            # Mean reversion: close crosses midpoint
            mid = (high_val + low_val) / 2.0
            prev_mid = (self._prev_upper + self._prev_lower) / 2.0

            if self._prev_close <= prev_mid and close > mid and fast_val > slow_val and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif self._prev_close >= prev_mid and close < mid and fast_val < slow_val and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_close = close
        self._prev_upper = high_val
        self._prev_lower = low_val

    def CreateClone(self):
        return myfriend_forex_instruments_strategy()
