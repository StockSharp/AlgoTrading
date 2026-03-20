import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy


class elderv30aug05v_strategy(Strategy):
    def __init__(self):
        super(elderv30aug05v_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 13) \
            .SetDisplay("EMA Period", "EMA period for trend", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 10) \
            .SetDisplay("Momentum Period", "Momentum period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_ema = 0.0
        self._prev_momentum = 0.0
        self._has_prev = False

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def momentum_period(self):
        return self._momentum_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(elderv30aug05v_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_momentum = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(elderv30aug05v_strategy, self).OnStarted(time)
        self._has_prev = False
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        momentum = Momentum()
        momentum.Length = self.momentum_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, momentum, self.process_candle).Start()

    def process_candle(self, candle, ema, momentum):
        if candle.State != CandleStates.Finished:
            return
        ema_val = float(ema)
        mom_val = float(momentum)
        if not self._has_prev:
            self._prev_ema = ema_val
            self._prev_momentum = mom_val
            self._has_prev = True
            return
        ema_rising = ema_val > self._prev_ema
        ema_falling = ema_val < self._prev_ema
        mom_cross_up = self._prev_momentum <= 0 and mom_val > 0
        mom_cross_down = self._prev_momentum >= 0 and mom_val < 0
        if ema_rising and mom_cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif ema_falling and mom_cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_ema = ema_val
        self._prev_momentum = mom_val

    def CreateClone(self):
        return elderv30aug05v_strategy()
