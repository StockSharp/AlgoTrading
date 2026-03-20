import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class security_revisited_strategy(Strategy):
    def __init__(self):
        super(security_revisited_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("EMA Length", "EMA period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._prev_diff = 0.0

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(security_revisited_strategy, self).OnReseted()
        self._prev_diff = 0.0

    def OnStarted(self, time):
        super(security_revisited_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return
        diff = candle.ClosePrice - ema_value
        cross_up = self._prev_diff <= 0 and diff > 0
        cross_down = self._prev_diff >= 0 and diff < 0
        self._prev_diff = diff
        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return security_revisited_strategy()
