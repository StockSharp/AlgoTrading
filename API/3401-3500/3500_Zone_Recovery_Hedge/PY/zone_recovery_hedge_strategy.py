import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class zone_recovery_hedge_strategy(Strategy):
    def __init__(self):
        super(zone_recovery_hedge_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._sma_period = self.Param("SmaPeriod", 50)

        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def SmaPeriod(self):
        return self._sma_period.Value

    @SmaPeriod.setter
    def SmaPeriod(self, value):
        self._sma_period.Value = value

    def OnReseted(self):
        super(zone_recovery_hedge_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(zone_recovery_hedge_strategy, self).OnStarted2(time)
        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        sma = SimpleMovingAverage()
        sma.Length = self.SmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, sma, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        sma_val = float(sma_value)
        close = float(candle.ClosePrice)

        if self._has_prev:
            if self._prev_rsi < 30 and rsi_val >= 30 and close > sma_val and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_rsi > 70 and rsi_val <= 70 and close < sma_val and self.Position >= 0:
                self.SellMarket()

        self._prev_rsi = rsi_val
        self._has_prev = True

    def CreateClone(self):
        return zone_recovery_hedge_strategy()
