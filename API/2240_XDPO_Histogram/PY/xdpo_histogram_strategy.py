import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class xdpo_histogram_strategy(Strategy):
    def __init__(self):
        super(xdpo_histogram_strategy, self).__init__()
        self._first_ma_length = self.Param("FirstMaLength", 12) \
            .SetDisplay("First MA Length", "Length of the initial moving average.", "Indicators")
        self._second_ma_length = self.Param("SecondMaLength", 5) \
            .SetDisplay("Second MA Length", "Length of the moving average applied to the difference.", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculations.", "General")
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._initialized = False

    @property
    def first_ma_length(self):
        return self._first_ma_length.Value

    @property
    def second_ma_length(self):
        return self._second_ma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(xdpo_histogram_strategy, self).OnReseted()
        self._prev1 = 0.0
        self._prev2 = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(xdpo_histogram_strategy, self).OnStarted(time)
        ma1 = ExponentialMovingAverage()
        ma1.Length = self.first_ma_length
        ma2 = ExponentialMovingAverage()
        ma2.Length = self.second_ma_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma1, ma2, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ma1_value, ma2_value):
        if candle.State != CandleStates.Finished:
            return
        ma1_value = float(ma1_value)
        ma2_value = float(ma2_value)
        xdpo = ma1_value - ma2_value
        if not self._initialized:
            self._prev1 = xdpo
            self._prev2 = xdpo
            self._initialized = True
            return
        if self._prev1 < self._prev2 and xdpo > self._prev1 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev1 > self._prev2 and xdpo < self._prev1 and self.Position >= 0:
            self.SellMarket()
        self._prev2 = self._prev1
        self._prev1 = xdpo

    def CreateClone(self):
        return xdpo_histogram_strategy()
