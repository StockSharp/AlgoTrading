import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergenceSignal,
    StochasticOscillator,
    RelativeStrengthIndex,
)
from StockSharp.Algo.Strategies import Strategy


class three_indicators_strategy(Strategy):
    def __init__(self):
        super(three_indicators_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle type", "Primary timeframe", "General")
        self._macd_fast_period = self.Param("MacdFastPeriod", 11) \
            .SetDisplay("MACD fast", "Fast EMA length", "MACD")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 53) \
            .SetDisplay("MACD slow", "Slow EMA length", "MACD")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 26) \
            .SetDisplay("MACD signal", "Signal smoothing", "MACD")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 40) \
            .SetDisplay("Stochastic %K", "%K length", "Stochastic")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 23) \
            .SetDisplay("Stochastic %D", "%D smoothing", "Stochastic")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI period", "RSI length", "RSI")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 2) \
            .SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new entry", "General")

        self._previous_open = None
        self._previous_macd_main = None
        self._previous_composite_signal = 0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def macd_fast_period(self):
        return self._macd_fast_period.Value

    @property
    def macd_slow_period(self):
        return self._macd_slow_period.Value

    @property
    def macd_signal_period(self):
        return self._macd_signal_period.Value

    @property
    def stochastic_k_period(self):
        return self._stochastic_k_period.Value

    @property
    def stochastic_d_period(self):
        return self._stochastic_d_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def signal_cooldown_bars(self):
        return self._signal_cooldown_bars.Value

    def OnReseted(self):
        super(three_indicators_strategy, self).OnReseted()
        self._previous_open = None
        self._previous_macd_main = None
        self._previous_composite_signal = 0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(three_indicators_strategy, self).OnStarted2(time)

        self._previous_open = None
        self._previous_macd_main = None
        self._previous_composite_signal = 0
        self._cooldown_remaining = 0

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.macd_fast_period
        macd.Macd.LongMa.Length = self.macd_slow_period
        macd.SignalMa.Length = self.macd_signal_period

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.stochastic_k_period
        stochastic.D.Length = self.stochastic_d_period

        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, stochastic, rsi, self._process_candle).Start()

    def _process_candle(self, candle, macd_value, stoch_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFinal or not stoch_value.IsFinal or not rsi_value.IsFinal:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if not macd_value.IsFormed or not stoch_value.IsFormed or not rsi_value.IsFormed:
            return

        macd_main = macd_value.Macd if macd_value.Macd is not None else 0.0
        stochastic_d = stoch_value.D if stoch_value.D is not None else 50.0
        rsi = float(rsi_value)

        if self._previous_open is None or self._previous_macd_main is None:
            self._previous_open = float(candle.OpenPrice)
            self._previous_macd_main = float(macd_main)
            return

        open_price = float(candle.OpenPrice)

        # Candle direction
        if open_price > self._previous_open:
            candle_signal = 1
        elif open_price < self._previous_open:
            candle_signal = -1
        else:
            candle_signal = 0

        # MACD direction
        macd_delta = float(macd_main) - self._previous_macd_main
        if macd_delta < 0:
            macd_signal = 1
        elif macd_delta > 0:
            macd_signal = -1
        else:
            macd_signal = 0

        # Stochastic direction
        stoch_d_val = float(stochastic_d)
        if stoch_d_val < 50.0:
            stoch_signal = 1
        elif stoch_d_val > 50.0:
            stoch_signal = -1
        else:
            stoch_signal = 0

        # RSI direction
        if rsi < 50.0:
            rsi_signal = 1
        elif rsi > 50.0:
            rsi_signal = -1
        else:
            rsi_signal = 0

        long_signal = candle_signal >= 0 and macd_signal >= 0 and stoch_signal >= 0 and rsi_signal >= 0
        short_signal = candle_signal <= 0 and macd_signal <= 0 and stoch_signal <= 0 and rsi_signal <= 0

        if long_signal:
            composite_signal = 1
        elif short_signal:
            composite_signal = -1
        else:
            composite_signal = 0

        if self._cooldown_remaining == 0 and composite_signal != 0 and composite_signal != self._previous_composite_signal:
            if composite_signal > 0 and self.Position <= 0:
                vol = self.Volume + (-self.Position if self.Position < 0 else 0)
                self.BuyMarket(vol)
                self._cooldown_remaining = self.signal_cooldown_bars
            elif composite_signal < 0 and self.Position >= 0:
                vol = self.Volume + (self.Position if self.Position > 0 else 0)
                self.SellMarket(vol)
                self._cooldown_remaining = self.signal_cooldown_bars

        self._previous_open = open_price
        self._previous_macd_main = float(macd_main)
        self._previous_composite_signal = composite_signal

    def CreateClone(self):
        return three_indicators_strategy()
