import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage, LinearReg
from StockSharp.Algo.Strategies import Strategy


class trigger_line_strategy(Strategy):
    def __init__(self):
        super(trigger_line_strategy, self).__init__()
        self._wma_period = self.Param("WmaPeriod", 24) \
            .SetDisplay("WT Period", "Period for weighted trend line", "Trigger Line")
        self._lsma_period = self.Param("LsmaPeriod", 6) \
            .SetDisplay("LSMA Period", "Period for least squares moving average", "Trigger Line")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle type used for the strategy", "General")
        self._initialized = False
        self._prev_line = 0.0
        self._prev_signal = 0.0

    @property
    def wma_period(self):
        return self._wma_period.Value

    @property
    def lsma_period(self):
        return self._lsma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trigger_line_strategy, self).OnReseted()
        self._initialized = False
        self._prev_line = 0.0
        self._prev_signal = 0.0

    def OnStarted2(self, time):
        super(trigger_line_strategy, self).OnStarted2(time)
        self._initialized = False
        self._prev_line = 0.0
        self._prev_signal = 0.0
        wma = WeightedMovingAverage()
        wma.Length = int(self.wma_period)
        lsma = LinearReg()
        lsma.Length = int(self.lsma_period)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wma, lsma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, wma_value, lsma_value):
        if candle.State != CandleStates.Finished:
            return
        wma_value = float(wma_value)
        lsma_value = float(lsma_value)
        if not self._initialized:
            self._prev_line = wma_value
            self._prev_signal = lsma_value
            self._initialized = True
            return
        cross_up = self._prev_line <= self._prev_signal and wma_value > lsma_value
        cross_down = self._prev_line >= self._prev_signal and wma_value < lsma_value
        if cross_up and self.Position <= 0:
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            self.SellMarket()
        self._prev_line = wma_value
        self._prev_signal = lsma_value

    def CreateClone(self):
        return trigger_line_strategy()
