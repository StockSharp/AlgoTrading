import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class donchain_counter_channel_system_strategy(Strategy):
    def __init__(self):
        super(donchain_counter_channel_system_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Channel Period", "Donchian channel lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_prev_high = 0.0
        self._prev_prev_low = 0.0
        self._bar_count = 0

    @property
    def channel_period(self):
        return self._channel_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchain_counter_channel_system_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_prev_high = 0.0
        self._prev_prev_low = 0.0
        self._bar_count = 0

    def OnStarted2(self, time):
        super(donchain_counter_channel_system_strategy, self).OnStarted2(time)
        self._bar_count = 0
        highest = Highest()
        highest.Length = self.channel_period
        lowest = Lowest()
        lowest.Length = self.channel_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.process_candle).Start()

    def process_candle(self, candle, highest, lowest):
        if candle.State != CandleStates.Finished:
            return
        high_val = float(highest)
        low_val = float(lowest)
        self._bar_count += 1
        if self._bar_count < 3:
            self._prev_prev_high = self._prev_high
            self._prev_prev_low = self._prev_low
            self._prev_high = high_val
            self._prev_low = low_val
            return
        lower_turning_up = low_val > self._prev_low and self._prev_low <= self._prev_prev_low
        upper_turning_down = high_val < self._prev_high and self._prev_high >= self._prev_prev_high
        if lower_turning_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif upper_turning_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_prev_high = self._prev_high
        self._prev_prev_low = self._prev_low
        self._prev_high = high_val
        self._prev_low = low_val

    def CreateClone(self):
        return donchain_counter_channel_system_strategy()
