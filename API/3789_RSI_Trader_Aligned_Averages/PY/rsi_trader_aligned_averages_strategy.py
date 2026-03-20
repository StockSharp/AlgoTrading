import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class rsi_trader_aligned_averages_strategy(Strategy):
    """RSI Trader combining SMA crossover with RSI trend confirmation.
    Buy when short SMA crosses above long SMA with RSI above 50.
    Sell when short SMA crosses below long SMA with RSI below 50."""

    def __init__(self):
        super(rsi_trader_aligned_averages_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._short_ma_period = self.Param("ShortMaPeriod", 9) \
            .SetDisplay("Short MA", "Short moving average period", "Indicators")
        self._long_ma_period = self.Param("LongMaPeriod", 26) \
            .SetDisplay("Long MA", "Long moving average period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def ShortMaPeriod(self):
        return self._short_ma_period.Value

    @property
    def LongMaPeriod(self):
        return self._long_ma_period.Value

    def OnReseted(self):
        super(rsi_trader_aligned_averages_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(rsi_trader_aligned_averages_strategy, self).OnStarted(time)

        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        short_ma = SimpleMovingAverage()
        short_ma.Length = self.ShortMaPeriod
        long_ma = SimpleMovingAverage()
        long_ma.Length = self.LongMaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, short_ma, long_ma, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value, short_ma, long_ma):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        short_val = float(short_ma)
        long_val = float(long_ma)

        if not self._has_prev:
            self._prev_short = short_val
            self._prev_long = long_val
            self._has_prev = True
            return

        bull_cross = self._prev_short <= self._prev_long and short_val > long_val
        bear_cross = self._prev_short >= self._prev_long and short_val < long_val

        if self.Position <= 0 and bull_cross and rsi_val > 50:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self.Position >= 0 and bear_cross and rsi_val < 50:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_short = short_val
        self._prev_long = long_val

    def CreateClone(self):
        return rsi_trader_aligned_averages_strategy()
