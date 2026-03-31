import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class tape_strategy(Strategy):
    def __init__(self):
        super(tape_strategy, self).__init__()
        self._threshold = self.Param("VolumeDeltaThreshold", 100) \
            .SetDisplay("Volume Delta", "Volume delta threshold", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._last_price = 0.0
        self._last_volume = 0.0

    @property
    def threshold(self):
        return self._threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tape_strategy, self).OnReseted()
        self._last_price = 0.0
        self._last_volume = 0.0

    def OnStarted2(self, time):
        super(tape_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self._last_price == 0:
            self._last_price = candle.ClosePrice
            self._last_volume = candle.TotalVolume
            return
        delta_volume = (candle.TotalVolume - self._last_volume) * Math.Sign(candle.ClosePrice - self._last_price)
        if delta_volume > self.threshold and self.Position <= 0:
            self.BuyMarket()
        elif delta_volume < -self.threshold and self.Position >= 0:
            self.SellMarket()
        self._last_price = candle.ClosePrice
        self._last_volume = candle.TotalVolume

    def CreateClone(self):
        return tape_strategy()
