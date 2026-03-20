import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class three_line_break_strategy(Strategy):
    def __init__(self):
        super(three_line_break_strategy, self).__init__()
        self._lines_break = self.Param("LinesBreak", 3) \
            .SetDisplay("Lines Break", "Number of lines for trend detection", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._lowest = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._trend_up = True

    @property
    def lines_break(self):
        return self._lines_break.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_line_break_strategy, self).OnReseted()
        self._lowest = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._trend_up = True

    def OnStarted(self, time):
        super(three_line_break_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self.lines_break
        self._lowest = Lowest()
        self._lowest.Length = self.lines_break
        self.Indicators.Add(self._lowest)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(highest, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, high_value):
        if candle.State != CandleStates.Finished:
            return
        low_value = self._lowest.Process(high_value)
        if not high_value.IsFormed or not low_value.IsFormed:
            return
        current_high = float(high_value)
        current_low = float(low_value)
        if self._prev_high == 0.0 or self._prev_low == 0.0:
            self._prev_high = current_high
            self._prev_low = current_low
            return
        trend_up = self._trend_up
        if trend_up and float(candle.LowPrice) < self._prev_low:
            trend_up = False
        elif not trend_up and float(candle.HighPrice) > self._prev_high:
            trend_up = True
        if trend_up != self._trend_up:
            if trend_up and self.Position <= 0:
                self.BuyMarket()
            elif not trend_up and self.Position >= 0:
                self.SellMarket()
        self._trend_up = trend_up
        self._prev_high = current_high
        self._prev_low = current_low

    def CreateClone(self):
        return three_line_break_strategy()
