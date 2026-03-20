import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class bezier_st_dev_strategy(Strategy):
    def __init__(self):
        super(bezier_st_dev_strategy, self).__init__()
        self._std_dev_period = self.Param("StdDevPeriod", 9) \
            .SetDisplay("StdDev Period", "Period for standard deviation calculation", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used", "General")
        self._prev_value1 = 0.0
        self._prev_value2 = 0.0

    @property
    def std_dev_period(self):
        return self._std_dev_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bezier_st_dev_strategy, self).OnReseted()
        self._prev_value1 = 0.0
        self._prev_value2 = 0.0

    def OnStarted(self, time):
        super(bezier_st_dev_strategy, self).OnStarted(time)
        std_dev = StandardDeviation()
        std_dev.Length = self.std_dev_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(std_dev, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, std_dev)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, std_dev_value):
        if candle.State != CandleStates.Finished:
            return
        std_dev_value = float(std_dev_value)
        if self._prev_value2 != 0.0:
            is_local_min = self._prev_value1 < self._prev_value2 and self._prev_value1 < std_dev_value
            is_local_max = self._prev_value1 > self._prev_value2 and self._prev_value1 > std_dev_value
            if is_local_min:
                if self.Position <= 0:
                    self.BuyMarket()
            elif is_local_max:
                if self.Position >= 0:
                    self.SellMarket()
        self._prev_value2 = self._prev_value1
        self._prev_value1 = std_dev_value

    def CreateClone(self):
        return bezier_st_dev_strategy()
