import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class internal_bar_strength_ibs_mean_reversion_strategy(Strategy):
    def __init__(self):
        super(internal_bar_strength_ibs_mean_reversion_strategy, self).__init__()
        self._upper_threshold = self.Param("UpperThreshold", 0.9) \
            .SetDisplay("Upper Threshold", "IBS value to trigger entry", "Parameters")
        self._lower_threshold = self.Param("LowerThreshold", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Lower Threshold", "IBS value to exit", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_high = 0.0
        self._is_ready = False

    @property
    def upper_threshold(self):
        return self._upper_threshold.Value

    @property
    def lower_threshold(self):
        return self._lower_threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(internal_bar_strength_ibs_mean_reversion_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._is_ready = False

    def OnStarted(self, time):
        super(internal_bar_strength_ibs_mean_reversion_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = 10
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, _dummy):
        if candle.State != CandleStates.Finished:
            return
        range = candle.HighPrice - candle.LowPrice
        if range == 0:
            self._prev_high = candle.HighPrice
            self._is_ready = True
            return
        if not self._is_ready:
            self._prev_high = candle.HighPrice
            self._is_ready = True
            return
        ibs = (candle.ClosePrice - candle.LowPrice) / range
        # Short when close above previous high and IBS is high (near candle top)
        if candle.ClosePrice > self._prev_high and ibs >= self.upper_threshold and self.Position >= 0:
            self.SellMarket()
        elif self.Position < 0 and ibs <= self.lower_threshold:
            self.BuyMarket()
        self._prev_high = candle.HighPrice

    def CreateClone(self):
        return internal_bar_strength_ibs_mean_reversion_strategy()
