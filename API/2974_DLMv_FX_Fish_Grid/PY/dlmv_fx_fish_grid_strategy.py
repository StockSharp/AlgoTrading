import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class dlmv_fx_fish_grid_strategy(Strategy):
    def __init__(self):
        super(dlmv_fx_fish_grid_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._period = self.Param("Period", 14) \
            .SetDisplay("Period", "Channel period", "Indicators")

        self._prev_high = None
        self._prev_low = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def Period(self):
        return self._period.Value

    def OnReseted(self):
        super(dlmv_fx_fish_grid_strategy, self).OnReseted()
        self._prev_high = None
        self._prev_low = None

    def OnStarted(self, time):
        super(dlmv_fx_fish_grid_strategy, self).OnStarted(time)
        self._prev_high = None
        self._prev_low = None

        highest = Highest()
        highest.Length = self.Period
        lowest = Lowest()
        lowest.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, high_value, low_value):
        if candle.State != CandleStates.Finished:
            return
        hv = float(high_value)
        lv = float(low_value)
        close = float(candle.ClosePrice)
        if self._prev_high is None or self._prev_low is None:
            self._prev_high = hv
            self._prev_low = lv
            return
        if close > self._prev_high and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif close < self._prev_low and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_high = hv
        self._prev_low = lv

    def CreateClone(self):
        return dlmv_fx_fish_grid_strategy()
