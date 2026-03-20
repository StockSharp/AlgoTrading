import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class internal_bar_strength_ibs_strategy(Strategy):
    def __init__(self):
        super(internal_bar_strength_ibs_strategy, self).__init__()
        self._upper_threshold = self.Param("UpperThreshold", 0.8) \
            .SetDisplay("Upper Threshold", "IBS value to exit position", "Parameters")
        self._lower_threshold = self.Param("LowerThreshold", 0.2) \
            .SetDisplay("Lower Threshold", "IBS value to enter long", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(240))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(internal_bar_strength_ibs_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        rng = high - low
        if rng == 0:
            return
        ibs = (close - low) / rng
        lower = float(self._lower_threshold.Value)
        upper = float(self._upper_threshold.Value)
        if ibs < lower and self.Position <= 0:
            self.BuyMarket()
        if self.Position > 0 and ibs >= upper:
            self.SellMarket()

    def CreateClone(self):
        return internal_bar_strength_ibs_strategy()
