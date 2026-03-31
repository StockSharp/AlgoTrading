import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, Momentum
from StockSharp.Algo.Strategies import Strategy


class momentum_keltner_stochastic_combo_strategy(Strategy):
    def __init__(self):
        super(momentum_keltner_stochastic_combo_strategy, self).__init__()
        self._mom_length = self.Param("MomLength", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Lookback", "Momentum lookback length", "Indicators")
        self._keltner_length = self.Param("KeltnerLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Keltner EMA Length", "EMA length for Keltner basis", "Indicators")
        self._keltner_multiplier = self.Param("KeltnerMultiplier", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Keltner Mult", "Keltner multiplier", "Indicators")
        self._threshold = self.Param("Threshold", 10.0) \
            .SetDisplay("Stochastic Threshold", "Threshold for Keltner stochastic", "Indicators")
        self._atr_length = self.Param("AtrLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR length for Keltner", "Indicators")
        self._sl_points = self.Param("SlPoints", 1185.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss Points", "Stop loss in price points", "Risk Management")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 24) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles for calculations", "General")
        self._prev_momentum = 0.0
        self._has_prev_momentum = False
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(momentum_keltner_stochastic_combo_strategy, self).OnReseted()
        self._prev_momentum = 0.0
        self._has_prev_momentum = False
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(momentum_keltner_stochastic_combo_strategy, self).OnStarted2(time)
        self._prev_momentum = 0.0
        self._has_prev_momentum = False
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._keltner_length.Value
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_length.Value
        self._momentum = Momentum()
        self._momentum.Length = self._mom_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._atr, self._momentum, self.OnProcess).Start()
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),
            Unit(self._sl_points.Value, UnitTypes.Absolute)
        )

    def OnProcess(self, candle, ema_value, atr_value, momentum_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        ev = float(ema_value)
        av = float(atr_value)
        mv = float(momentum_value)
        km = float(self._keltner_multiplier.Value)
        upper = ev + km * av
        lower = ev - km * av
        denom = upper - lower
        close = float(candle.ClosePrice)
        keltner_stoch = 100.0 * (close - lower) / denom if denom != 0.0 else 50.0
        momentum_cross_up = self._has_prev_momentum and self._prev_momentum <= 0.0 and mv > 0.0
        momentum_cross_down = self._has_prev_momentum and self._prev_momentum >= 0.0 and mv < 0.0
        thr = float(self._threshold.Value)
        long_condition = momentum_cross_up and keltner_stoch <= thr
        short_condition = momentum_cross_down and keltner_stoch >= (100.0 - thr)
        self._bars_from_signal += 1
        cd = self._signal_cooldown_bars.Value
        if self._bars_from_signal >= cd and long_condition and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= cd and short_condition and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0
        self._prev_momentum = mv
        self._has_prev_momentum = True

    def CreateClone(self):
        return momentum_keltner_stochastic_combo_strategy()
