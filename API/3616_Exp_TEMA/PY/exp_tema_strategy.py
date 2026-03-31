import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class exp_tema_strategy(Strategy):
    def __init__(self):
        super(exp_tema_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._tema_period = self.Param("TemaPeriod", 40)

        self._prev1 = None
        self._prev2 = None
        self._prev3 = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TemaPeriod(self):
        return self._tema_period.Value

    @TemaPeriod.setter
    def TemaPeriod(self, value):
        self._tema_period.Value = value

    def OnReseted(self):
        super(exp_tema_strategy, self).OnReseted()
        self._prev1 = None
        self._prev2 = None
        self._prev3 = None

    def OnStarted2(self, time):
        super(exp_tema_strategy, self).OnStarted2(time)
        self._prev1 = None
        self._prev2 = None
        self._prev3 = None

        tema = ExponentialMovingAverage()
        tema.Length = self.TemaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(tema, self._process_candle).Start()

    def _process_candle(self, candle, tema_value):
        if candle.State != CandleStates.Finished:
            return

        val = float(tema_value)

        if self._prev1 is None:
            self._prev1 = val
            return

        if self._prev2 is None:
            self._prev2 = self._prev1
            self._prev1 = val
            return

        if self._prev3 is None:
            self._prev3 = self._prev2
            self._prev2 = self._prev1
            self._prev1 = val
            return

        dtema1 = self._prev1 - self._prev2
        dtema2 = self._prev2 - self._prev3

        turned_up = dtema2 < 0 and dtema1 > 0
        turned_down = dtema2 > 0 and dtema1 < 0

        if turned_up and self.Position <= 0:
            self.BuyMarket()
        elif turned_down and self.Position >= 0:
            self.SellMarket()

        self._prev3 = self._prev2
        self._prev2 = self._prev1
        self._prev1 = val

    def CreateClone(self):
        return exp_tema_strategy()
