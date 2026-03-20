import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class candles_smoothed_strategy(Strategy):
    def __init__(self):
        super(candles_smoothed_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame", "General")
        self._ma_length = self.Param("MaLength", 30) \
            .SetDisplay("MA Length", "Moving average smoothing length", "Indicator")
        self._ma = None
        self._prev_color = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def ma_length(self):
        return self._ma_length.Value

    def OnReseted(self):
        super(candles_smoothed_strategy, self).OnReseted()
        self._ma = None
        self._prev_color = None

    def OnStarted(self, time):
        super(candles_smoothed_strategy, self).OnStarted(time)
        self._ma = WeightedMovingAverage()
        self._ma.Length = self.ma_length
        self.Indicators.Add(self._ma)
        warmup = ExponentialMovingAverage()
        warmup.Length = self.ma_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(warmup, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, _sma_value):
        if candle.State != CandleStates.Finished:
            return
        diff = float(candle.ClosePrice) - float(candle.OpenPrice)
        ma_result = self._ma.Process(DecimalIndicatorValue(self._ma, diff, candle.OpenTime))
        if not ma_result.IsFormed:
            return
        smoothed = float(ma_result)
        color = 0 if smoothed > 0 else 1
        if self._prev_color is not None:
            if color == 1 and self._prev_color == 0 and self.Position <= 0:
                self.BuyMarket()
            elif color == 0 and self._prev_color == 1 and self.Position >= 0:
                self.SellMarket()
        self._prev_color = color

    def CreateClone(self):
        return candles_smoothed_strategy()
