import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, StochasticOscillator


class macd_stochastic_strategy(Strategy):
    """MACD crossover with optional stochastic confirmation, trailing stop, and session windows."""

    def __init__(self):
        super(macd_stochastic_strategy, self).__init__()

        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast Period", "Fast EMA length for MACD", "MACD")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow Period", "Slow EMA length for MACD", "MACD")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal Period", "Signal line length for MACD", "MACD")
        self._use_stochastic = self.Param("UseStochastic", False) \
            .SetDisplay("Use Stochastic Filter", "Enable stochastic confirmation", "Stochastic")
        self._stochastic_bars_to_check = self.Param("StochasticBarsToCheck", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Bars", "History depth for stochastic confirmation", "Stochastic")
        self._stochastic_length = self.Param("StochasticLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Length", "Number of bars for K calculation", "Stochastic")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic K Smoothing", "Smoothing period for K line", "Stochastic")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic D Period", "Smoothing period for D line", "Stochastic")
        self._stop_loss_pips = self.Param("StopLossPips", 100) \
            .SetDisplay("Stop Loss (pips)", "Initial stop-loss distance", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 100) \
            .SetDisplay("Take Profit (pips)", "Initial take-profit distance", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Trailing Step (pips)", "Minimum move before trailing", "Risk")
        self._max_positions = self.Param("MaxPositions", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Positions", "Maximum simultaneous positions", "Trading")
        self._no_loss_stop_pips = self.Param("NoLossStopPips", 1) \
            .SetDisplay("No Loss Stop (pips)", "Break-even offset for trailing", "Risk")
        self._when_set_no_loss_stop_pips = self.Param("WhenSetNoLossStopPips", 25) \
            .SetDisplay("Activation Profit (pips)", "Profit before enabling trailing", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles used for analysis", "General")

        self._stochastic_history = []
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev_macd = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._pip_size = 0.0
        self._last_entry_bar_time = None

    @property
    def MacdFastPeriod(self):
        return int(self._macd_fast_period.Value)
    @property
    def MacdSlowPeriod(self):
        return int(self._macd_slow_period.Value)
    @property
    def MacdSignalPeriod(self):
        return int(self._macd_signal_period.Value)
    @property
    def UseStochastic(self):
        return self._use_stochastic.Value
    @property
    def StochasticBarsToCheck(self):
        return int(self._stochastic_bars_to_check.Value)
    @property
    def StochasticLength(self):
        return int(self._stochastic_length.Value)
    @property
    def StochasticKPeriod(self):
        return int(self._stochastic_k_period.Value)
    @property
    def StochasticDPeriod(self):
        return int(self._stochastic_d_period.Value)
    @property
    def StopLossPips(self):
        return int(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return int(self._take_profit_pips.Value)
    @property
    def TrailingStopPips(self):
        return int(self._trailing_stop_pips.Value)
    @property
    def TrailingStepPips(self):
        return int(self._trailing_step_pips.Value)
    @property
    def MaxPositions(self):
        return int(self._max_positions.Value)
    @property
    def NoLossStopPips(self):
        return int(self._no_loss_stop_pips.Value)
    @property
    def WhenSetNoLossStopPips(self):
        return int(self._when_set_no_loss_stop_pips.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _update_pip_size(self):
        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0
        if price_step <= 0:
            self._pip_size = 0.0
            return
        ratio = 1.0 / price_step
        digits = int(round(math.log10(ratio)))
        self._pip_size = price_step * 10.0 if (digits == 3 or digits == 5) else price_step

    def OnStarted2(self, time):
        super(macd_stochastic_strategy, self).OnStarted2(time)

        self._stochastic_history = []
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev_macd = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._last_entry_bar_time = None

        self._macd_ind = MovingAverageConvergenceDivergenceSignal()
        self._macd_ind.Macd.ShortMa.Length = self.MacdFastPeriod
        self._macd_ind.Macd.LongMa.Length = self.MacdSlowPeriod
        self._macd_ind.SignalMa.Length = self.MacdSignalPeriod

        self._stoch_ind = StochasticOscillator()
        self._stoch_ind.K.Length = self.StochasticLength
        self._stoch_ind.D.Length = self.StochasticDPeriod

        self._update_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._macd_ind, self._stoch_ind, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._macd_ind)
            self.DrawIndicator(area, self._stoch_ind)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, macd_value, stochastic_value):
        if candle.State != CandleStates.Finished:
            return

        if self.Position == 0 and self._entry_price != 0:
            self._reset_position_state()

        if self._pip_size == 0:
            self._update_pip_size()

        self._manage_position(candle)

        macd_main_n = macd_value.Macd
        signal_n = macd_value.Signal
        if macd_main_n is None or signal_n is None:
            return

        macd_main = float(macd_main_n)
        signal = float(signal_n)

        current_k = None
        current_d = None
        stoch_k_n = stochastic_value.K
        stoch_d_n = stochastic_value.D
        if stoch_k_n is not None and stoch_d_n is not None:
            current_k = float(stoch_k_n)
            current_d = float(stoch_d_n)
            self._update_stochastic_history(current_k, current_d)

        allow_trading = self._macd_ind.IsFormed and self.MaxPositions > 0
        macd_cross_up = (self._has_prev_macd and self._prev_macd <= self._prev_signal
                         and macd_main > signal and macd_main < 0 and self._prev_macd < 0)
        macd_cross_down = (self._has_prev_macd and self._prev_macd >= self._prev_signal
                           and macd_main < signal and macd_main > 0 and self._prev_macd > 0)

        if (allow_trading and self.Position == 0
                and (self._last_entry_bar_time is None or candle.OpenTime > self._last_entry_bar_time)):

            if macd_cross_up and self._passes_stochastic_filter(True, current_k, current_d):
                self._enter_long(candle)
            elif macd_cross_down and self._passes_stochastic_filter(False, current_k, current_d):
                self._enter_short(candle)

        self._prev_macd = macd_main
        self._prev_signal = signal
        self._has_prev_macd = True

    def _enter_long(self, candle):
        self.BuyMarket()
        close = float(candle.ClosePrice)
        self._entry_price = close
        self._stop_price = close - self.StopLossPips * self._pip_size if self.StopLossPips > 0 and self._pip_size > 0 else 0.0
        self._take_price = close + self.TakeProfitPips * self._pip_size if self.TakeProfitPips > 0 and self._pip_size > 0 else 0.0
        self._last_entry_bar_time = candle.OpenTime

    def _enter_short(self, candle):
        self.SellMarket()
        close = float(candle.ClosePrice)
        self._entry_price = close
        self._stop_price = close + self.StopLossPips * self._pip_size if self.StopLossPips > 0 and self._pip_size > 0 else 0.0
        self._take_price = close - self.TakeProfitPips * self._pip_size if self.TakeProfitPips > 0 and self._pip_size > 0 else 0.0
        self._last_entry_bar_time = candle.OpenTime

    def _manage_position(self, candle):
        if self.Position > 0:
            self._update_long_trailing(candle)
            self._check_long_exits(candle)
        elif self.Position < 0:
            self._update_short_trailing(candle)
            self._check_short_exits(candle)

    def _check_long_exits(self, candle):
        lo = float(candle.LowPrice)
        h = float(candle.HighPrice)
        if self._stop_price > 0 and lo <= self._stop_price:
            self.SellMarket()
            self._reset_position_state()
            return
        if self._take_price > 0 and h >= self._take_price:
            self.SellMarket()
            self._reset_position_state()

    def _check_short_exits(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        if self._stop_price > 0 and h >= self._stop_price:
            self.BuyMarket()
            self._reset_position_state()
            return
        if self._take_price > 0 and lo <= self._take_price:
            self.BuyMarket()
            self._reset_position_state()

    def _update_long_trailing(self, candle):
        if self.TrailingStopPips <= 0 or self._pip_size <= 0 or self._stop_price <= 0:
            return
        close = float(candle.ClosePrice)
        profit = close - self._entry_price
        if profit <= self.WhenSetNoLossStopPips * self._pip_size:
            return
        new_stop = self._stop_price + self.TrailingStopPips * self._pip_size
        min_stop = self._entry_price + self.NoLossStopPips * self._pip_size
        max_stop = close - (self.TrailingStepPips + self.TrailingStopPips) * self._pip_size
        if new_stop <= self._stop_price:
            return
        if new_stop <= min_stop:
            return
        if new_stop >= max_stop:
            return
        self._stop_price = new_stop

    def _update_short_trailing(self, candle):
        if self.TrailingStopPips <= 0 or self._pip_size <= 0:
            return
        close = float(candle.ClosePrice)
        profit = self._entry_price - close
        if profit <= self.WhenSetNoLossStopPips * self._pip_size:
            return
        if self._stop_price > 0:
            new_stop = self._stop_price - self.TrailingStopPips * self._pip_size
            max_stop = self._entry_price - self.NoLossStopPips * self._pip_size
            min_stop = close + (self.TrailingStepPips + self.TrailingStopPips) * self._pip_size
            if new_stop >= self._stop_price:
                return
            if new_stop >= max_stop:
                return
            if new_stop <= min_stop:
                return
            self._stop_price = new_stop
        else:
            candidate = self._entry_price - self.NoLossStopPips * self._pip_size
            threshold = close + self.WhenSetNoLossStopPips * self._pip_size
            if candidate <= 0:
                return
            if candidate <= threshold:
                return
            self._stop_price = candidate

    def _passes_stochastic_filter(self, is_buy, current_k, current_d):
        if not self.UseStochastic:
            return True
        if current_k is None or current_d is None:
            return False
        bars = max(1, self.StochasticBarsToCheck)
        if len(self._stochastic_history) < bars:
            return False
        if bars <= 1:
            return current_d < current_k if is_buy else current_d > current_k
        old_k, old_d = self._stochastic_history[0]
        if is_buy:
            return current_d < current_k and old_d > old_k
        else:
            return current_d > current_k and old_d < old_k

    def _update_stochastic_history(self, k, d):
        mx = max(1, self.StochasticBarsToCheck)
        self._stochastic_history.append((k, d))
        while len(self._stochastic_history) > mx:
            self._stochastic_history.pop(0)

    def _reset_position_state(self):
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(macd_stochastic_strategy, self).OnReseted()
        self._stochastic_history = []
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._has_prev_macd = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._pip_size = 0.0
        self._last_entry_bar_time = None

    def CreateClone(self):
        return macd_stochastic_strategy()
