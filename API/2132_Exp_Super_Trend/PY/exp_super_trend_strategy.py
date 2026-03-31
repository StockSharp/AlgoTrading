import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class exp_super_trend_strategy(Strategy):
    def __init__(self):
        super(exp_super_trend_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 10) \
            .SetDisplay("ATR Period", "ATR period for SuperTrend", "SuperTrend")
        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetDisplay("Multiplier", "ATR multiplier for SuperTrend", "SuperTrend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for indicator calculation", "General")
        self._super_trend = None

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def multiplier(self):
        return self._multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_super_trend_strategy, self).OnReseted()
        self._super_trend = None

    def OnStarted2(self, time):
        super(exp_super_trend_strategy, self).OnStarted2(time)
        self._super_trend = SuperTrend()
        self._super_trend.Length = self.atr_period
        self._super_trend.Multiplier = self.multiplier
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._super_trend, self.process_candle).Start()

    def process_candle(self, candle, st_value):
        if candle.State != CandleStates.Finished:
            return
        is_up_trend = st_value.IsUpTrend
        if is_up_trend and self.Position <= 0:
            self.BuyMarket()
        elif not is_up_trend and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return exp_super_trend_strategy()
