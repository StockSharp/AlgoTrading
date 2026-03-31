import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergenceSignal,
    AverageTrueRange,
    ExponentialMovingAverage as EMA,
    StochasticOscillator,
    MovingAverageConvergenceDivergenceSignalValue,
    StochasticOscillatorValue,
)
from StockSharp.Algo.Strategies import Strategy


class macd_stochastic_confirmation_reversal_strategy(Strategy):
    def __init__(self):
        super(macd_stochastic_confirmation_reversal_strategy, self).__init__()

        self._macd_fast = self.Param("MacdFastLength", 12) \
            .SetDisplay("MACD Fast", "Fast EMA length", "MACD")
        self._macd_slow = self.Param("MacdSlowLength", 26) \
            .SetDisplay("MACD Slow", "Slow EMA length", "MACD")
        self._macd_signal = self.Param("MacdSignalLength", 9) \
            .SetDisplay("MACD Signal", "Signal EMA length", "MACD")
        self._trailing_ema_length = self.Param("TrailingEmaLength", 20) \
            .SetDisplay("Trailing EMA", "EMA length for trailing take profit", "Strategy")
        self._stop_loss_atr = self.Param("StopLossAtrMultiplier", 3.25) \
            .SetDisplay("ATR Stop", "ATR multiplier for stop loss", "Strategy")
        self._trailing_activation_atr = self.Param("TrailingActivationAtrMultiplier", 4.25) \
            .SetDisplay("ATR Activate", "ATR multiplier to activate trailing", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Base candle type", "Common")

        self._daily_k = None
        self._daily_d = None
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
        self._stop_loss_level = 0.0
        self._activation_level = 0.0
        self._trailing_active = False
        self._take_profit_level = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_stochastic_confirmation_reversal_strategy, self).OnReseted()
        self._daily_k = None
        self._daily_d = None
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
        self._stop_loss_level = 0.0
        self._activation_level = 0.0
        self._trailing_active = False
        self._take_profit_level = None
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(macd_stochastic_confirmation_reversal_strategy, self).OnStarted2(time)

        self._macd_ind = MovingAverageConvergenceDivergenceSignal()
        self._macd_ind.Macd.ShortMa.Length = self._macd_fast.Value
        self._macd_ind.Macd.LongMa.Length = self._macd_slow.Value
        self._macd_ind.SignalMa.Length = self._macd_signal.Value

        self._atr = AverageTrueRange()
        self._atr.Length = 14
        self._ema = EMA()
        self._ema.Length = self._trailing_ema_length.Value
        self._daily_stoch = StochasticOscillator()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._macd_ind, self._atr, self._ema, self._process_candle).Start()

        daily_sub = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromDays(1)))
        daily_sub.BindEx(self._daily_stoch, self._process_daily).Start()

    def _process_daily(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        stoch = stoch_value
        k = stoch.K
        d = stoch.D
        if k is not None and d is not None:
            self._daily_k = float(k)
            self._daily_d = float(d)

    def _process_candle(self, candle, macd_value, atr_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        macd = macd_value
        macd_line_val = macd.Macd
        signal_line_val = macd.Signal
        if macd_line_val is None or signal_line_val is None:
            return

        macd_line = float(macd_line_val)
        signal_line = float(signal_line_val)
        atr = float(atr_value)
        ema = float(ema_value)

        if not self._macd_ind.IsFormed or self._daily_k is None or self._daily_d is None:
            self._prev_macd = macd_line
            self._prev_signal = signal_line
            self._has_prev = True
            return

        cross_up = self._has_prev and self._prev_macd <= self._prev_signal and macd_line > signal_line

        if cross_up and self._daily_k > self._daily_d and self._daily_k < 80.0 and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = float(candle.ClosePrice)
            self._stop_loss_level = self._entry_price - float(self._stop_loss_atr.Value) * atr
            self._activation_level = self._entry_price + float(self._trailing_activation_atr.Value) * atr
            self._trailing_active = False
            self._take_profit_level = None

        if self.Position > 0:
            if not self._trailing_active and float(candle.HighPrice) > self._activation_level:
                self._trailing_active = True

            if self._trailing_active:
                self._take_profit_level = ema

            if (self._take_profit_level is not None and float(candle.ClosePrice) < self._take_profit_level) or \
               float(candle.LowPrice) <= self._stop_loss_level:
                self.SellMarket(self.Position)
                self._trailing_active = False
                self._take_profit_level = None

        self._prev_macd = macd_line
        self._prev_signal = signal_line
        self._has_prev = True

    def CreateClone(self):
        return macd_stochastic_confirmation_reversal_strategy()
