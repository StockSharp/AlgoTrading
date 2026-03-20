import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class z_score_normalized_vix_strategy(Strategy):
    """Z-Score Normalized VIX strategy.
    Original uses multiple VIX securities; simplified to single security z-score.
    Buys when z-score drops below -threshold, sells when it rises above -threshold.
    """
    def __init__(self):
        super(z_score_normalized_vix_strategy, self).__init__()
        self._z_score_length = self.Param("ZScoreLength", 6) \
            .SetDisplay("Z-Score Length", "Lookback period for z-score", "Parameters")
        self._threshold = self.Param("Threshold", 1.0) \
            .SetDisplay("Z-Score Threshold", "Entry and exit threshold", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "Data")

    @property
    def z_score_length(self):
        return self._z_score_length.Value

    @property
    def threshold(self):
        return self._threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(z_score_normalized_vix_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.z_score_length
        std = StandardDeviation()
        std.Length = self.z_score_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        if std_val == 0:
            return
        z_score = (float(candle.ClosePrice) - float(sma_val)) / float(std_val)
        if self.Position <= 0 and z_score < -self.threshold:
            self.BuyMarket()
        elif self.Position > 0 and z_score > -self.threshold:
            self.SellMarket()

    def CreateClone(self):
        return z_score_normalized_vix_strategy()
