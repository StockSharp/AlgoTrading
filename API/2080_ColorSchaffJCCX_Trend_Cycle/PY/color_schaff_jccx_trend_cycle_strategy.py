import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SchaffTrendCycle
from StockSharp.Algo.Strategies import Strategy


class color_schaff_jccx_trend_cycle_strategy(Strategy):
    def __init__(self):
        super(color_schaff_jccx_trend_cycle_strategy, self).__init__()
        self._high_level = self.Param("HighLevel", 75.0) \
            .SetDisplay("High Level", "Upper trigger level", "Signal")
        self._low_level = self.Param("LowLevel", 25.0) \
            .SetDisplay("Low Level", "Lower trigger level", "Signal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev = None

    @property
    def high_level(self):
        return self._high_level.Value

    @property
    def low_level(self):
        return self._low_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_schaff_jccx_trend_cycle_strategy, self).OnReseted()
        self._prev = None

    def OnStarted(self, time):
        super(color_schaff_jccx_trend_cycle_strategy, self).OnStarted(time)
        stc = SchaffTrendCycle()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(stc, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stc)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, stc_val):
        if candle.State != CandleStates.Finished:
            return
        stc_val = float(stc_val)
        if self._prev is None:
            self._prev = stc_val
            return

        if self._prev > float(self.high_level) and stc_val <= float(self.high_level) and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev < float(self.low_level) and stc_val >= float(self.low_level) and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev = stc_val

    def CreateClone(self):
        return color_schaff_jccx_trend_cycle_strategy()
