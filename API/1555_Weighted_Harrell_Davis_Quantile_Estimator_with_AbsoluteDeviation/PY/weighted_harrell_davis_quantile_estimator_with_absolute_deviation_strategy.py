import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class weighted_harrell_davis_quantile_estimator_with_absolute_deviation_strategy(Strategy):
    def __init__(self):
        super(weighted_harrell_davis_quantile_estimator_with_absolute_deviation_strategy, self).__init__()
        self._length = self.Param("Length", 39) \
            .SetDisplay("Length", "Lookback period", "General")
        self._dev_mult = self.Param("DevMult", TimeSpan.FromMinutes(5)) \
            .SetDisplay("Deviation Mult", "Band multiplier", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def length(self):
        return self._length.Value

    @property
    def dev_mult(self):
        return self._dev_mult.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(weighted_harrell_davis_quantile_estimator_with_absolute_deviation_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.length
        std_dev = StandardDeviation()
        std_dev.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        if std_val <= 0:
            return
        upper = sma_val + self.dev_mult * std_val
        lower = sma_val - self.dev_mult * std_val
        if candle.ClosePrice > upper and self.Position >= 0:
            self.SellMarket()
        elif candle.ClosePrice < lower and self.Position <= 0:
            self.BuyMarket()

    def CreateClone(self):
        return weighted_harrell_davis_quantile_estimator_with_absolute_deviation_strategy()
