import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class gap_fill_strategy(Strategy):
    def __init__(self):
        super(gap_fill_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 12) \
            .SetDisplay("Channel Period", "Highest/Lowest period", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "Data")
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    @property
    def channel_period(self):
        return self._channel_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gap_fill_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(gap_fill_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self.channel_period
        lowest = Lowest()
        lowest.Length = self.channel_period
        self.SubscribeCandles(self.candle_type) \
            .Bind(highest, lowest, self.process_candle) \
            .Start()

    def process_candle(self, candle, high_val, low_val):
        if candle.State != CandleStates.Finished:
            return
        high_val = float(high_val)
        low_val = float(low_val)
        if not self._has_prev:
            self._prev_high = high_val
            self._prev_low = low_val
            self._has_prev = True
            return
        if float(candle.ClosePrice) > self._prev_high and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif float(candle.ClosePrice) < self._prev_low and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_high = high_val
        self._prev_low = low_val

    def CreateClone(self):
        return gap_fill_strategy()
