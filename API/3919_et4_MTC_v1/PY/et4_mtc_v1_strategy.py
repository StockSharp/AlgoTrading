import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy

class et4_mtc_v1_strategy(Strategy):
    """
    EMA + Momentum crossover strategy.
    Buys when price > EMA and momentum crosses above 0.
    Sells when price < EMA and momentum crosses below 0.
    """

    def __init__(self):
        super(et4_mtc_v1_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA trend filter", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetDisplay("Momentum", "Momentum period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_mom = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(et4_mtc_v1_strategy, self).OnReseted()
        self._prev_mom = 0.0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(et4_mtc_v1_strategy, self).OnStarted(time)

        self._has_prev = False
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        mom = Momentum()
        mom.Length = self._momentum_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, mom, self._process_candle).Start()

    def _process_candle(self, candle, ema_val, mom_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema_val)
        mom_val = float(mom_val)

        if not self._has_prev:
            self._prev_mom = mom_val
            self._has_prev = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_mom = mom_val
            return

        if close > ema_val and self._prev_mom <= 0 and mom_val > 0 and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = 2
        elif close < ema_val and self._prev_mom >= 0 and mom_val < 0 and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = 2

        self._prev_mom = mom_val

    def CreateClone(self):
        return et4_mtc_v1_strategy()
