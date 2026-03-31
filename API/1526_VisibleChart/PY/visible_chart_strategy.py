import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class visible_chart_strategy(Strategy):
    def __init__(self):
        super(visible_chart_strategy, self).__init__()
        self._visible_bars = self.Param("VisibleBars", 40) \
            .SetDisplay("Visible Bars", "Number of bars considered visible", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to analyze", "General")
        self._high_val = 0.0
        self._low_val = 0.0
        self._cooldown = 0

    @property
    def visible_bars(self):
        return self._visible_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(visible_chart_strategy, self).OnReseted()
        self._high_val = 0.0
        self._low_val = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(visible_chart_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self.visible_bars
        lowest = Lowest()
        lowest.Length = self.visible_bars
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def on_process(self, candle, high, low):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._high_val = high
            self._low_val = low
            return
        if self._high_val == 0:
            self._high_val = high
            self._low_val = low
            return
        breakout_up = candle.ClosePrice >= self._high_val and self.Position <= 0
        breakout_down = candle.ClosePrice <= self._low_val and self.Position >= 0
        if breakout_up:
            self.BuyMarket()
            self._cooldown = 30
        elif breakout_down:
            self.SellMarket()
            self._cooldown = 30
        self._high_val = high
        self._low_val = low

    def CreateClone(self):
        return visible_chart_strategy()
