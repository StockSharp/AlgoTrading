import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class the_enchantress_strategy(Strategy):
    def __init__(self):
        super(the_enchantress_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._ema_period = self.Param("EmaPeriod", 50)
        self._rsi_period = self.Param("RsiPeriod", 21)

        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    def OnReseted(self):
        super(the_enchantress_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(the_enchantress_strategy, self).OnStarted(time)
        self._prev_rsi = 0.0
        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, rsi, self._process_candle).Start()

    def _process_candle(self, candle, ema_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        rsi_val = float(rsi_value)

        if self._has_prev:
            if close > ema_val and self._prev_rsi < 45 and rsi_val >= 45 and self.Position <= 0:
                self.BuyMarket()
            elif close < ema_val and self._prev_rsi > 55 and rsi_val <= 55 and self.Position >= 0:
                self.SellMarket()

        self._prev_rsi = rsi_val
        self._has_prev = True

    def CreateClone(self):
        return the_enchantress_strategy()
