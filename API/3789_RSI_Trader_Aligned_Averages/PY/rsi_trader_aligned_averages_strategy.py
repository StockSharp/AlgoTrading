import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class rsi_trader_aligned_averages_strategy(Strategy):
    def __init__(self):
        super(rsi_trader_aligned_averages_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._short_ma_period = self.Param("ShortMaPeriod", 9) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._long_ma_period = self.Param("LongMaPeriod", 26) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")

        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_trader_aligned_averages_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(rsi_trader_aligned_averages_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._short_ma = SimpleMovingAverage()
        self._short_ma.Length = self.short_ma_period
        self._long_ma = SimpleMovingAverage()
        self._long_ma.Length = self.long_ma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._short_ma, self._long_ma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rsi_trader_aligned_averages_strategy()
