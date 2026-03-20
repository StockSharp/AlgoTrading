import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class p_channel_system_strategy(Strategy):
    def __init__(self):
        super(p_channel_system_strategy, self).__init__()
        self._period = self.Param("Period", 20) \
            .SetDisplay("Period", "Channel calculation period", "Indicator")
        self._shift = self.Param("Shift", 2) \
            .SetDisplay("Shift", "Bars shift for channel", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "General")
        self._prev_above = False
        self._prev_below = False
        self._upper_queue = []
        self._lower_queue = []

    @property
    def period(self):
        return self._period.Value

    @property
    def shift(self):
        return self._shift.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(p_channel_system_strategy, self).OnReseted()
        self._prev_above = False
        self._prev_below = False
        self._upper_queue = []
        self._lower_queue = []

    def OnStarted(self, time):
        super(p_channel_system_strategy, self).OnStarted(time)
        self._prev_above = False
        self._prev_below = False
        self._upper_queue = []
        self._lower_queue = []
        highest = Highest()
        highest.Length = self.period
        lowest = Lowest()
        lowest.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, high_val, low_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        high_val = float(high_val)
        low_val = float(low_val)
        shift = int(self.shift)
        self._upper_queue.append(high_val)
        self._lower_queue.append(low_val)
        if len(self._upper_queue) <= shift or len(self._lower_queue) <= shift:
            return
        upper = self._upper_queue.pop(0)
        lower = self._lower_queue.pop(0)
        close_price = float(candle.ClosePrice)
        is_above = close_price > upper
        is_below = close_price < lower
        if self._prev_above and not is_above and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_below and not is_below and self.Position >= 0:
            self.SellMarket()
        self._prev_above = is_above
        self._prev_below = is_below

    def CreateClone(self):
        return p_channel_system_strategy()
