import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class validate_me_strategy(Strategy):
    def __init__(self):
        super(validate_me_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._period = self.Param("Period", 14)

        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    def OnReseted(self):
        super(validate_me_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(validate_me_strategy, self).OnStarted2(time)
        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)

        if self._has_prev:
            if self._prev_rsi < 30 and rsi_val >= 30 and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_rsi > 70 and rsi_val <= 70 and self.Position >= 0:
                self.SellMarket()

        self._prev_rsi = rsi_val
        self._has_prev = True

    def CreateClone(self):
        return validate_me_strategy()
