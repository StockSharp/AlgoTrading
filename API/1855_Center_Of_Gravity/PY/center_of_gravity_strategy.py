import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class center_of_gravity_strategy(Strategy):
    def __init__(self):
        super(center_of_gravity_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculation", "General")
        self._period = self.Param("Period", 10) \
            .SetDisplay("Period", "Center of Gravity averaging period", "Indicators")
        self._prev_sma = 0.0
        self._prev_wma = 0.0
        self._initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def period(self):
        return self._period.Value

    def OnReseted(self):
        super(center_of_gravity_strategy, self).OnReseted()
        self._prev_sma = 0.0
        self._prev_wma = 0.0
        self._initialized = False

    def OnStarted2(self, time):
        super(center_of_gravity_strategy, self).OnStarted2(time)
        sma = SimpleMovingAverage()
        sma.Length = self.period
        wma = WeightedMovingAverage()
        wma.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, wma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, wma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_value, wma_value):
        if candle.State != CandleStates.Finished:
            return
        sma_value = float(sma_value)
        wma_value = float(wma_value)
        if not self._initialized:
            self._prev_sma = sma_value
            self._prev_wma = wma_value
            self._initialized = True
            return
        cross_up = self._prev_sma <= self._prev_wma and sma_value > wma_value
        cross_down = self._prev_sma >= self._prev_wma and sma_value < wma_value
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_sma = sma_value
        self._prev_wma = wma_value

    def CreateClone(self):
        return center_of_gravity_strategy()
