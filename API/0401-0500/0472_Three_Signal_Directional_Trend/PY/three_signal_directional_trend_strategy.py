import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (MovingAverageConvergenceDivergenceSignal, StochasticOscillator,
                                         RelativeStrengthIndex, IndicatorHelper)
from StockSharp.Algo.Strategies import Strategy


class three_signal_directional_trend_strategy(Strategy):
    """Three Signal Directional Trend Strategy."""

    def __init__(self):
        super(three_signal_directional_trend_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._macd_fast_length = self.Param("MacdFastLength", 12) \
            .SetDisplay("MACD Fast", "Fast EMA length", "MACD")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetDisplay("MACD Slow", "Slow EMA length", "MACD")
        self._macd_avg_length = self.Param("MacdAvgLength", 9) \
            .SetDisplay("MACD Signal", "Signal EMA length", "MACD")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._macd = None
        self._stochastic = None
        self._rsi = None
        self._prev_macd_signal = 0.0
        self._macd_init = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_signal_directional_trend_strategy, self).OnReseted()
        self._macd = None
        self._stochastic = None
        self._rsi = None
        self._prev_macd_signal = 0.0
        self._macd_init = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(three_signal_directional_trend_strategy, self).OnStarted2(time)

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = int(self._macd_fast_length.Value)
        self._macd.Macd.LongMa.Length = int(self._macd_slow_length.Value)
        self._macd.SignalMa.Length = int(self._macd_avg_length.Value)

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = 14
        self._stochastic.D.Length = 3

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._macd, self._stochastic, self._rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, macd_val, stoch_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._macd.IsFormed or not self._stochastic.IsFormed or not self._rsi.IsFormed:
            return

        if macd_val.IsEmpty or stoch_val.IsEmpty or rsi_val.IsEmpty:
            return

        if macd_val.Signal is None:
            return

        macd_signal = float(macd_val.Signal)

        if stoch_val.K is None:
            return

        stoch_k = float(stoch_val.K)
        rsi = float(IndicatorHelper.ToDecimal(rsi_val))

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_macd_signal = macd_signal
            self._macd_init = True
            return

        if not self._macd_init:
            self._prev_macd_signal = macd_signal
            self._macd_init = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_macd_signal = macd_signal
            return

        cooldown = int(self._cooldown_bars.Value)
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
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif short_count >= 2 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._prev_macd_signal = macd_signal

    def CreateClone(self):
        return three_signal_directional_trend_strategy()
