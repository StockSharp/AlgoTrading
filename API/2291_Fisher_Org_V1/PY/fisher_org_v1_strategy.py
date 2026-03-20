import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import EhlersFisherTransform
from StockSharp.Algo.Strategies import Strategy


class fisher_org_v1_strategy(Strategy):
    def __init__(self):
        super(fisher_org_v1_strategy, self).__init__()
        self._length = self.Param("Length", 7) \
            .SetDisplay("Fisher Length", "Period for Fisher Transform", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev = 0.0
        self._prev_prev = 0.0
        self._value_count = 0

    @property
    def length(self):
        return self._length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fisher_org_v1_strategy, self).OnReseted()
        self._prev = 0.0
        self._prev_prev = 0.0
        self._value_count = 0

    def OnStarted(self, time):
        super(fisher_org_v1_strategy, self).OnStarted(time)
        self._prev = 0.0
        self._prev_prev = 0.0
        self._value_count = 0
        fisher = EhlersFisherTransform()
        fisher.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fisher, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fisher)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fisher_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        main_line = fisher_val.MainLine
        if main_line is None:
            return
        fisher_value = float(main_line)
        if self._value_count < 2:
            if self._value_count == 0:
                self._prev = fisher_value
            else:
                self._prev_prev = self._prev
                self._prev = fisher_value
            self._value_count += 1
            return
        is_long_signal = self._prev_prev > self._prev and self._prev <= fisher_value
        is_short_signal = self._prev_prev < self._prev and self._prev >= fisher_value
        if is_long_signal and self.Position <= 0:
            self.BuyMarket()
        if is_short_signal and self.Position >= 0:
            self.SellMarket()
        self._prev_prev = self._prev
        self._prev = fisher_value

    def CreateClone(self):
        return fisher_org_v1_strategy()
