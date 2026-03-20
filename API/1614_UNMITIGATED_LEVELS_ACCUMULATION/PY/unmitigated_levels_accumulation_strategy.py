import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class unmitigated_levels_accumulation_strategy(Strategy):
    def __init__(self):
        super(unmitigated_levels_accumulation_strategy, self).__init__()
        self._low_length = self.Param("LowLength", 50) \
            .SetDisplay("Low Length", "Lowest period for support", "General")
        self._high_length = self.Param("HighLength", 30) \
            .SetDisplay("High Length", "Highest period for resistance", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._prev_low = 0.0
        self._prev_high = 0.0

    @property
    def low_length(self):
        return self._low_length.Value

    @property
    def high_length(self):
        return self._high_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(unmitigated_levels_accumulation_strategy, self).OnReseted()
        self._prev_low = 0.0
        self._prev_high = 0.0

    def OnStarted(self, time):
        super(unmitigated_levels_accumulation_strategy, self).OnStarted(time)
        lowest = Lowest()
        lowest.Length = self.low_length
        highest = Highest()
        highest.Length = self.high_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(lowest, highest, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, lowest)
            self.DrawIndicator(area, highest)
            self.DrawOwnTrades(area)

    def on_process(self, candle, low_value, high_value):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_low == 0 or self._prev_high == 0:
            self._prev_low = low_value
            self._prev_high = high_value
            return
        # Buy when price bounces off support (touches lowest and recovers)
        if candle.LowPrice <= low_value and candle.ClosePrice > low_value and self.Position <= 0:
            self.BuyMarket()
        # Sell when price breaks to new high and pulls back
        if candle.HighPrice >= high_value and candle.ClosePrice < high_value and self.Position >= 0:
            self.SellMarket()
        # Exit long if price breaks below support
        if self.Position > 0 and candle.ClosePrice < self._prev_low * 0.99:
            self.SellMarket()
        # Exit short if price breaks above resistance
        if self.Position < 0 and candle.ClosePrice > self._prev_high * 1.01:
            self.BuyMarket()
        self._prev_low = low_value
        self._prev_high = high_value

    def CreateClone(self):
        return unmitigated_levels_accumulation_strategy()
