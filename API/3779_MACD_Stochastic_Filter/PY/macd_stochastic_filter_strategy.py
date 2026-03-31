import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage,
    MovingAverageConvergenceDivergenceSignal,
    StochasticOscillator
)
from StockSharp.Algo.Strategies import Strategy


class macd_stochastic_filter_strategy(Strategy):
    """MACD strategy with stochastic confirmation and EMA trend filter.
    Buy when MACD crosses above signal with stochastic K > D and price above EMA.
    Sell when MACD crosses below signal with stochastic K < D and price below EMA."""

    def __init__(self):
        super(macd_stochastic_filter_strategy, self).__init__()

        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("MACD Signal", "Signal EMA period for MACD", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 26) \
            .SetDisplay("Trend EMA", "EMA period for trend filter", "Indicators")
        self._stoch_k_length = self.Param("StochKLength", 14) \
            .SetDisplay("Stochastic K", "Look-back length for stochastic K", "Indicators")
        self._stoch_d_length = self.Param("StochDLength", 3) \
            .SetDisplay("Stochastic D", "Smoothing for D line", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for price data", "General")

        self._prev_macd = None
        self._prev_signal = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MacdFastPeriod(self):
        return self._macd_fast_period.Value

    @property
    def MacdSlowPeriod(self):
        return self._macd_slow_period.Value

    @property
    def MacdSignalPeriod(self):
        return self._macd_signal_period.Value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @property
    def StochKLength(self):
        return self._stoch_k_length.Value

    @property
    def StochDLength(self):
        return self._stoch_d_length.Value

    def OnReseted(self):
        super(macd_stochastic_filter_strategy, self).OnReseted()
        self._prev_macd = None
        self._prev_signal = None

    def OnStarted2(self, time):
        super(macd_stochastic_filter_strategy, self).OnStarted2(time)

        self._prev_macd = None
        self._prev_signal = None

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.MacdFastPeriod
        macd.Macd.LongMa.Length = self.MacdSlowPeriod
        macd.SignalMa.Length = self.MacdSignalPeriod

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochKLength
        stochastic.D.Length = self.StochDLength

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, stochastic, ema, self._process_candle).Start()

    def _process_candle(self, candle, macd_value, stoch_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFinal or not stoch_value.IsFinal or not ema_value.IsFinal:
            return

        macd_raw = macd_value.Macd if hasattr(macd_value, 'Macd') else None
        signal_raw = macd_value.Signal if hasattr(macd_value, 'Signal') else None

        if macd_raw is None or signal_raw is None:
            return

        macd_main = float(macd_raw)
        signal_line = float(signal_raw)

        k_raw = stoch_value.K if hasattr(stoch_value, 'K') else None
        d_raw = stoch_value.D if hasattr(stoch_value, 'D') else None

        if k_raw is None or d_raw is None:
            return

        k_val = float(k_raw)
        d_val = float(d_raw)

        ema_val = float(ema_value)

        if self._prev_macd is None or self._prev_signal is None:
            self._prev_macd = macd_main
            self._prev_signal = signal_line
            return

        prev_macd = self._prev_macd
        prev_signal = self._prev_signal

        # MACD crossover signals
        macd_bullish_cross = prev_macd <= prev_signal and macd_main > signal_line
        macd_bearish_cross = prev_macd >= prev_signal and macd_main < signal_line

        close = float(candle.ClosePrice)

        # Long: MACD bullish cross + stochastic K > D + price above EMA
        if self.Position <= 0 and macd_bullish_cross and k_val > d_val and close > ema_val:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Short: MACD bearish cross + stochastic K < D + price below EMA
        elif self.Position >= 0 and macd_bearish_cross and k_val < d_val and close < ema_val:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_macd = macd_main
        self._prev_signal = signal_line

    def CreateClone(self):
        return macd_stochastic_filter_strategy()
