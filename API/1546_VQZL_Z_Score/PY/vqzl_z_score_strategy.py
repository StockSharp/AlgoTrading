import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class vqzl_z_score_strategy(Strategy):
    def __init__(self):
        super(vqzl_z_score_strategy, self).__init__()
        self._price_smoothing = self.Param("PriceSmoothing", 15) \
            .SetDisplay("Price Smoothing", "Length of smoothing moving average", "ZScore")
        self._z_length = self.Param("ZLength", 100) \
            .SetDisplay("Z Length", "Lookback for standard deviation", "ZScore")
        self._threshold = self.Param("Threshold", 1.64) \
            .SetDisplay("Z Threshold", "Z-score threshold", "ZScore")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def price_smoothing(self):
        return self._price_smoothing.Value

    @property
    def z_length(self):
        return self._z_length.Value

    @property
    def threshold(self):
        return self._threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vqzl_z_score_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(vqzl_z_score_strategy, self).OnStarted(time)
        ma = SimpleMovingAverage()
        ma.Length = self.price_smoothing
        dev = StandardDeviation()
        dev.Length = self.z_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, dev)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ma_value, dev_value):
        if candle.State != CandleStates.Finished:
            return
        if dev_value == 0:
            return
        z = float(candle.ClosePrice - ma_value) / float(dev_value)
        if z > float(self.threshold) and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif z < -float(self.threshold) and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return vqzl_z_score_strategy()
