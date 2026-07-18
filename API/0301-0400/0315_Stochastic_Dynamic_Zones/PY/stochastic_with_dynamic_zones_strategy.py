import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class stochastic_with_dynamic_zones_strategy(Strategy):
    """
    Strategy based on Stochastic Oscillator with dynamic overbought and oversold zones.
    """

    def __init__(self):
        super(stochastic_with_dynamic_zones_strategy, self).__init__()

        self._stoch_k_period = self.Param("StochKPeriod", 14) \
            .SetDisplay("Stoch %K Period", "Smoothing period for %K", "Indicators")

        self._stoch_d_period = self.Param("StochDPeriod", 3) \
            .SetDisplay("Stoch %D Period", "Smoothing period for %D", "Indicators")

        self._lookback_period = self.Param("LookbackPeriod", 40) \
            .SetDisplay("Lookback Period", "Period for dynamic zones", "Indicators")

        self._std_dev_factor = self.Param("StdDevFactor", 3.0) \
            .SetDisplay("StdDev Factor", "Factor for dynamic zones", "Indicators")

        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 240) \
            .SetDisplay("Signal Cooldown", "Bars to wait between signals", "Trading") \
            .SetGreaterThanZero()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_stoch_k = 50.0
        self._stoch_sum = 0.0
        self._stoch_sq_sum = 0.0
        self._stoch_count = 0
        self._cooldown_remaining = 0
        self._last_entry_time = None
        self._was_below_oversold = False
        self._stoch_queue = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_with_dynamic_zones_strategy, self).OnReseted()
        self._prev_stoch_k = 50.0
        self._stoch_sum = 0.0
        self._stoch_sq_sum = 0.0
        self._stoch_count = 0
        self._cooldown_remaining = 0
        self._last_entry_time = None
        self._was_below_oversold = False
        self._stoch_queue = []

    def OnStarted2(self, time):
        super(stochastic_with_dynamic_zones_strategy, self).OnStarted2(time)

        self._prev_stoch_k = 50.0
        self._stoch_sum = 0.0
        self._stoch_sq_sum = 0.0
        self._stoch_count = 0
        self._cooldown_remaining = 0
        self._last_entry_time = None
        self._was_below_oversold = False
        self._stoch_queue = []

        stochastic = StochasticOscillator()
        stochastic.K.Length = int(self._stoch_k_period.Value)
        stochastic.D.Length = int(self._stoch_d_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not stoch_value.IsFormed:
            return

        stoch_k_val = stoch_value.K
        if stoch_k_val is None:
            return

        stoch_k = float(stoch_k_val)
        lookback = int(self._lookback_period.Value)

        self._stoch_queue.append(stoch_k)
        self._stoch_sum += stoch_k
        self._stoch_sq_sum += stoch_k * stoch_k
        self._stoch_count += 1

        if self._stoch_count > lookback:
            removed = self._stoch_queue.pop(0)
            self._stoch_sum -= removed
            self._stoch_sq_sum -= removed * removed
            self._stoch_count = lookback

        if self._stoch_count < lookback:
            self._prev_stoch_k = stoch_k
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        average = self._stoch_sum / self._stoch_count
        variance = (self._stoch_sq_sum / self._stoch_count) - (average * average)
        std_dev = 0.0 if variance <= 0 else math.sqrt(variance)

        sdf = float(self._std_dev_factor.Value)
        dynamic_oversold = max(10.0, average - sdf * std_dev)
        entry_oversold = min(dynamic_oversold, 10.0)
        is_reversing_up = stoch_k > self._prev_stoch_k

        cd = int(self._signal_cooldown_bars.Value)

        if self.Position > 0 and stoch_k >= 50.0:
            self.SellMarket()
            self._cooldown_remaining = cd
        elif self._cooldown_remaining == 0 and not self._has_entry_today(candle) and self._was_below_oversold and stoch_k >= entry_oversold and is_reversing_up and self.Position == 0:
            self.BuyMarket()
            self._cooldown_remaining = cd
            close_time = candle.CloseTime
            open_time = candle.OpenTime
            try:
                if close_time is not None and str(close_time) != "01/01/0001 00:00:00 +00:00":
                    self._last_entry_time = close_time
                else:
                    self._last_entry_time = open_time
            except:
                self._last_entry_time = open_time

        self._was_below_oversold = stoch_k < entry_oversold
        self._prev_stoch_k = stoch_k

    def _has_entry_today(self, candle):
        if self._last_entry_time is None:
            return False

        close_time = candle.CloseTime
        open_time = candle.OpenTime
        try:
            if close_time is not None and str(close_time) != "01/01/0001 00:00:00 +00:00":
                candle_time = close_time
            else:
                candle_time = open_time
        except:
            candle_time = open_time

        try:
            diff_days = (candle_time.Date - self._last_entry_time.Date).TotalDays
            return diff_days < 3
        except:
            return False

    def CreateClone(self):
        return stochastic_with_dynamic_zones_strategy()
