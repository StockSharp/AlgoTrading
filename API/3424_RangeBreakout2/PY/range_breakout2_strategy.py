import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

class range_breakout2_strategy(Strategy):
    def __init__(self):
        super(range_breakout2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._channel_period = self.Param("ChannelPeriod", 30)

        self._prev_high = 0.0
        self._prev_low = 0.0
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

    @ChannelPeriod.setter
    def ChannelPeriod(self, value):
        self._channel_period.Value = value

    def OnReseted(self):
        super(range_breakout2_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(range_breakout2_strategy, self).OnStarted2(time)
        self._has_prev = False

        highest = Highest()
        highest.Length = self.ChannelPeriod
        lowest = Lowest()
        lowest.Length = self.ChannelPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self._process_candle).Start()

    def _process_candle(self, candle, high_value, low_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if self._has_prev:
            if close > self._prev_high and self.Position <= 0:
                self.BuyMarket()
            elif close < self._prev_low and self.Position >= 0:
                self.SellMarket()

        self._prev_high = float(high_value)
        self._prev_low = float(low_value)
        self._has_prev = True

    def CreateClone(self):
        return range_breakout2_strategy()
