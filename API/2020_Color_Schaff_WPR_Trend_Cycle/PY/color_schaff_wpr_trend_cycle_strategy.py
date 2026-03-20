import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SchaffTrendCycle
from StockSharp.Algo.Strategies import Strategy


class color_schaff_wpr_trend_cycle_strategy(Strategy):

    def __init__(self):
        super(color_schaff_wpr_trend_cycle_strategy, self).__init__()

        self._fast_wpr = self.Param("FastWpr", 23) \
            .SetDisplay("Fast WPR", "Fast Williams %R period", "Indicator")
        self._slow_wpr = self.Param("SlowWpr", 50) \
            .SetDisplay("Slow WPR", "Slow Williams %R period", "Indicator")
        self._cycle = self.Param("Cycle", 10) \
            .SetDisplay("Cycle", "Cycle length", "Indicator")
        self._high_level = self.Param("HighLevel", 60) \
            .SetDisplay("High Level", "Upper trigger level", "Indicator")
        self._low_level = self.Param("LowLevel", -60) \
            .SetDisplay("Low Level", "Lower trigger level", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_stc = 0.0

    @property
    def FastWpr(self):
        return self._fast_wpr.Value

    @FastWpr.setter
    def FastWpr(self, value):
        self._fast_wpr.Value = value

    @property
    def SlowWpr(self):
        return self._slow_wpr.Value

    @SlowWpr.setter
    def SlowWpr(self, value):
        self._slow_wpr.Value = value

    @property
    def Cycle(self):
        return self._cycle.Value

    @Cycle.setter
    def Cycle(self, value):
        self._cycle.Value = value

    @property
    def HighLevel(self):
        return self._high_level.Value

    @HighLevel.setter
    def HighLevel(self, value):
        self._high_level.Value = value

    @property
    def LowLevel(self):
        return self._low_level.Value

    @LowLevel.setter
    def LowLevel(self, value):
        self._low_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(color_schaff_wpr_trend_cycle_strategy, self).OnStarted(time)

        stc = SchaffTrendCycle()
        stc.Length = self.Cycle
        stc.Macd.Macd.ShortMa.Length = self.FastWpr
        stc.Macd.Macd.LongMa.Length = self.SlowWpr

        self.SubscribeCandles(self.CandleType) \
            .Bind(stc, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, stc_value):
        if candle.State != CandleStates.Finished:
            return

        stc_val = float(stc_value)
        prev = self._prev_stc
        self._prev_stc = stc_val

        high = float(self.HighLevel)
        low = float(self.LowLevel)

        cross_up = prev <= high and stc_val > high
        cross_down = prev >= low and stc_val < low

        if cross_up:
            if self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
        elif cross_down:
            if self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

    def OnReseted(self):
        super(color_schaff_wpr_trend_cycle_strategy, self).OnReseted()
        self._prev_stc = 0.0

    def CreateClone(self):
        return color_schaff_wpr_trend_cycle_strategy()
