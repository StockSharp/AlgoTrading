import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_j_fatl_digit_strategy(Strategy):
    def __init__(self):
        super(color_j_fatl_digit_strategy, self).__init__()
        self._jma_length = self.Param("JmaLength", 5) \
            .SetDisplay("JMA Length", "Period for Jurik Moving Average", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe of indicator", "Parameters")
        self._prev_jma = None
        self._prev_slope = None

    @property
    def jma_length(self):
        return self._jma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_j_fatl_digit_strategy, self).OnReseted()
        self._prev_jma = None
        self._prev_slope = None

    def OnStarted(self, time):
        super(color_j_fatl_digit_strategy, self).OnStarted(time)
        self._prev_jma = None
        self._prev_slope = None
        jma = JurikMovingAverage()
        jma.Length = self.jma_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(jma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, jma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, jma_value):
        if candle.State != CandleStates.Finished:
            return
        jma_value = float(jma_value)
        slope = None
        if self._prev_jma is not None:
            slope = jma_value - self._prev_jma
        if slope is not None and self._prev_slope is not None:
            if self._prev_slope <= 0 and slope > 0 and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_slope >= 0 and slope < 0 and self.Position >= 0:
                self.SellMarket()
        self._prev_slope = slope
        self._prev_jma = jma_value

    def CreateClone(self):
        return color_j_fatl_digit_strategy()
