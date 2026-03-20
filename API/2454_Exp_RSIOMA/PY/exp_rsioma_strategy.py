import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_rsioma_strategy(Strategy):
    def __init__(self):
        super(exp_rsioma_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 21)
        self._ema_period = self.Param("EmaPeriod", 14)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._prev_rsi = 0.0
        self._prev_ema = 0.0
        self._has_prev = False
        self._rsi_ema = None

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

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(exp_rsioma_strategy, self).OnStarted(time)

        self._has_prev = False
        self._prev_rsi = 0.0
        self._prev_ema = 0.0

        self._rsi_ema = ExponentialMovingAverage()
        self._rsi_ema.Length = self.EmaPeriod

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2.0, UnitTypes.Percent),
            Unit(1.0, UnitTypes.Percent))

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)

        ema_result = self._rsi_ema.Process(rsi_value, candle.CloseTime, True)
        if not self._rsi_ema.IsFormed:
            return

        ema_val = float(ema_result)

        if self._has_prev:
            cross_up = self._prev_rsi <= self._prev_ema and rsi_val > ema_val
            cross_down = self._prev_rsi >= self._prev_ema and rsi_val < ema_val

            if cross_up and self.Position == 0:
                self.BuyMarket()
            elif cross_down and self.Position == 0:
                self.SellMarket()

        self._prev_rsi = rsi_val
        self._prev_ema = ema_val
        self._has_prev = True

    def OnReseted(self):
        super(exp_rsioma_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_ema = 0.0
        self._has_prev = False
        self._rsi_ema = None

    def CreateClone(self):
        return exp_rsioma_strategy()
