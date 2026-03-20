import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class scalping_assistant_strategy(Strategy):
    def __init__(self):
        super(scalping_assistant_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._ema_period = self.Param("EmaPeriod", 20)

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
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    def OnReseted(self):
        super(scalping_assistant_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(scalping_assistant_strategy, self).OnStarted(time)
        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, ema, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        ema_val = float(ema_value)
        close = float(candle.ClosePrice)

        if self._has_prev:
            if self._prev_rsi < 30 and rsi_val >= 30 and close > ema_val and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_rsi > 70 and rsi_val <= 70 and close < ema_val and self.Position >= 0:
                self.SellMarket()

        self._prev_rsi = rsi_val
        self._has_prev = True

    def CreateClone(self):
        return scalping_assistant_strategy()
