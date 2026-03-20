import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class close_profit_end_of_week_strategy(Strategy):
    def __init__(self):
        super(close_profit_end_of_week_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._mom_period = self.Param("MomPeriod", 20)
        self._ema_period = self.Param("EmaPeriod", 50)
        self._momentum_level = self.Param("MomentumLevel", 101.0)

        self._prev_mom = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MomPeriod(self):
        return self._mom_period.Value

    @MomPeriod.setter
    def MomPeriod(self, value):
        self._mom_period.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def MomentumLevel(self):
        return self._momentum_level.Value

    @MomentumLevel.setter
    def MomentumLevel(self, value):
        self._momentum_level.Value = value

    def OnReseted(self):
        super(close_profit_end_of_week_strategy, self).OnReseted()
        self._prev_mom = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(close_profit_end_of_week_strategy, self).OnStarted(time)
        self._prev_mom = 0.0
        self._has_prev = False

        mom = Momentum()
        mom.Length = self.MomPeriod
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(mom, ema, self._process_candle).Start()

    def _process_candle(self, candle, mom_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        mom_val = float(mom_value)
        ema_val = float(ema_value)
        close = float(candle.ClosePrice)
        level = float(self.MomentumLevel)

        if self._has_prev:
            if self._prev_mom <= level and mom_val > level and close > ema_val and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_mom >= (200.0 - level) and mom_val < (200.0 - level) and close < ema_val and self.Position >= 0:
                self.SellMarket()

        self._prev_mom = mom_val
        self._has_prev = True

    def CreateClone(self):
        return close_profit_end_of_week_strategy()
