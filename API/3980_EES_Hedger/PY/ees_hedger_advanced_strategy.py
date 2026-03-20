import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage

class ees_hedger_advanced_strategy(Strategy):
    def __init__(self):
        super(ees_hedger_advanced_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._stop_loss_pips = self.Param("StopLossPips", 500) \
            .SetDisplay("Stop Loss", "Stop-loss distance", "Risk Management")
        self._take_profit_pips = self.Param("TakeProfitPips", 500) \
            .SetDisplay("Take Profit", "Take-profit distance", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candles used for calculations", "Market Data")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(ees_hedger_advanced_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self.ProcessCandle).Start()

        tp = Unit(self.TakeProfitPips, UnitTypes.Absolute) if self.TakeProfitPips > 0 else None
        sl = Unit(self.StopLossPips, UnitTypes.Absolute) if self.StopLossPips > 0 else None
        self.StartProtection(tp, sl)

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_value = float(fast_value)
        slow_value = float(slow_value)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast = fast_value
            self._prev_slow = slow_value
            self._has_prev = True
            return

        if not self._has_prev:
            self._prev_fast = fast_value
            self._prev_slow = slow_value
            self._has_prev = True
            return

        crossed_up = self._prev_fast <= self._prev_slow and fast_value > slow_value
        crossed_down = self._prev_fast >= self._prev_slow and fast_value < slow_value

        if crossed_up:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            if self.Position <= 0:
                self.BuyMarket(self.Volume)
        elif crossed_down:
            if self.Position > 0:
                self.SellMarket(self.Position)
            if self.Position >= 0:
                self.SellMarket(self.Volume)

        self._prev_fast = fast_value
        self._prev_slow = slow_value

    def OnReseted(self):
        super(ees_hedger_advanced_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def CreateClone(self):
        return ees_hedger_advanced_strategy()
