import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, StochasticOscillator, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class three_signal_directional_trend_strategy(Strategy):
    def __init__(self):
        super(three_signal_directional_trend_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._macd_fast_length = self.Param("MacdFastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "Fast EMA length", "MACD")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "Slow EMA length", "MACD")
        self._macd_avg_length = self.Param("MacdAvgLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "Signal EMA length", "MACD")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_macd_signal = 0.0
        self._macd_init = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def macd_fast_length(self):
        return self._macd_fast_length.Value
    @property
    def macd_slow_length(self):
        return self._macd_slow_length.Value
    @property
    def macd_avg_length(self):
        return self._macd_avg_length.Value
    @property
    def rsi_length(self):
        return self._rsi_length.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(three_signal_directional_trend_strategy, self).OnReseted()
        self._prev_macd_signal = 0.0
        self._macd_init = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(three_signal_directional_trend_strategy, self).OnStarted(time)
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.macd_fast_length
        self._macd.Macd.LongMa.Length = self.macd_slow_length
        self._macd.SignalMa.Length = self.macd_avg_length
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = 14
        self._stochastic.D.Length = 3
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .BindEx(self._macd, self._stochastic, self._rsi, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, macd_val, stoch_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._macd.IsFormed or not self._stochastic.IsFormed or not self._rsi.IsFormed:
            return
        if macd_val.IsEmpty or stoch_val.IsEmpty or rsi_val.IsEmpty:
            return

        macd_signal = macd_val.Signal
        if macd_signal is None:
            return
        macd_signal = float(macd_signal)

        stoch_k = stoch_val.K
        if stoch_k is None:
            return
        stoch_k = float(stoch_k)
        rsi = float(rsi_val)

        if not self._macd_init:
            self._prev_macd_signal = macd_signal
            self._macd_init = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_macd_signal = macd_signal
            return

        long_count = 0
        short_count = 0

        if macd_signal > self._prev_macd_signal:
            long_count += 1
        elif macd_signal < self._prev_macd_signal:
            short_count += 1

        if stoch_k <= 20:
            long_count += 1
        elif stoch_k >= 80:
            short_count += 1

        if rsi < 40:
            long_count += 1
        elif rsi > 60:
            short_count += 1

        if long_count >= 2 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif short_count >= 2 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars

        self._prev_macd_signal = macd_signal

    def CreateClone(self):
        return three_signal_directional_trend_strategy()
