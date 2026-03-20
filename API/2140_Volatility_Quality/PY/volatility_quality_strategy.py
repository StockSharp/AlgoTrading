import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class volatility_quality_strategy(Strategy):
    def __init__(self):
        super(volatility_quality_strategy, self).__init__()
        self._length = self.Param("Length", 5) \
            .SetDisplay("Length", "Smoothing period for median price", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "Common")
        self._sma = None
        self._prev_sma = 0.0
        self._prev_color = -1

    @property
    def length(self):
        return self._length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volatility_quality_strategy, self).OnReseted()
        self._sma = None
        self._prev_sma = 0.0
        self._prev_color = -1

    def OnStarted(self, time):
        super(volatility_quality_strategy, self).OnStarted(time)
        self._sma = ExponentialMovingAverage()
        self._sma.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(2.0, UnitTypes.Percent),
            stopLoss=Unit(1.0, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return
        sma_value = float(sma_value)
        if self._prev_sma == 0.0:
            self._prev_sma = sma_value
            self._prev_color = -1
            return
        if sma_value > self._prev_sma:
            color = 0  # rising
        elif sma_value < self._prev_sma:
            color = 1  # falling
        else:
            color = self._prev_color
        # Slope turned up - buy
        if self._prev_color == 1 and color == 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Slope turned down - sell
        elif self._prev_color == 0 and color == 1 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_sma = sma_value
        self._prev_color = color

    def CreateClone(self):
        return volatility_quality_strategy()
