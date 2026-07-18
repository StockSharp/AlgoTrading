import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from System.Collections.Generic import List
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AwesomeOscillator, StochasticOscillator

# Exit reason constants
EXIT_MANUAL = 0
EXIT_TAKE_PROFIT = 1
EXIT_STOP_LOSS = 2


class oz_fx_accelerator_stochastic_strategy(Strategy):
    """OzFx strategy: stacks layered entries when AC oscillator and stochastic agree."""

    def __init__(self):
        super(oz_fx_accelerator_stochastic_strategy, self).__init__()

        self._max_layers = self.Param("MaxLayers", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Layers", "Maximum number of layered positions", "Risk")
        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume", "Order volume for each layer", "Trading")
        self._stop_loss_pips = self.Param("StopLossPips", 10.0) \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 5.0) \
            .SetDisplay("Take Profit (pips)", "Base take profit increment in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0) \
            .SetDisplay("Trailing Step (pips)", "Extra move required before advancing trailing stop", "Risk")
        self._k_period = self.Param("KPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("%K Period", "Stochastic lookback window", "Stochastic")
        self._d_period = self.Param("DPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("%D Period", "Smoothing length for %D", "Stochastic")
        self._smoothing_period = self.Param("SmoothingPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Slowing", "Final smoothing for %K", "Stochastic")
        self._stochastic_level = self.Param("StochasticLevel", 50.0) \
            .SetDisplay("Stochastic Level", "Threshold used to trigger signals", "Stochastic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary candle series", "General")

        self._last_ac = None
        self._last_exit_was_tp = False
        self._pip_size = 0.0
        self._pip_initialized = False
        self._long_entries = []
        self._short_entries = []
        self._ao_buffer = []
        self._ao_sma_length = 5

    @property
    def MaxLayers(self):
        return int(self._max_layers.Value)
    @property
    def OrderVolume(self):
        return float(self._order_volume.Value)
    @property
    def StopLossPips(self):
        return float(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return float(self._take_profit_pips.Value)
    @property
    def TrailingStopPips(self):
        return float(self._trailing_stop_pips.Value)
    @property
    def TrailingStepPips(self):
        return float(self._trailing_step_pips.Value)
    @property
    def KPeriod(self):
        return int(self._k_period.Value)
    @property
    def DPeriod(self):
        return int(self._d_period.Value)
    @property
    def SmoothingPeriod(self):
        return int(self._smoothing_period.Value)
    @property
    def StochasticLevel(self):
        return float(self._stochastic_level.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _get_pip_size(self):
        if self._pip_initialized:
            return self._pip_size
        sec = self.Security
        step = 0.0001
        if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0:
            step = float(sec.PriceStep)
        decimals = 0
        if sec is not None and sec.Decimals is not None:
            decimals = int(sec.Decimals)
        adjust = 10.0 if decimals == 3 or decimals == 5 else 1.0
        self._pip_size = step * adjust
        if self._pip_size <= 0:
            self._pip_size = step
        if self._pip_size <= 0:
            self._pip_size = 0.0001
        self._pip_initialized = True
        return self._pip_size

    def OnStarted2(self, time):
        super(oz_fx_accelerator_stochastic_strategy, self).OnStarted2(time)

        self._last_ac = None
        self._last_exit_was_tp = False
        self._pip_initialized = False
        self._pip_size = 0.0
        self._long_entries = []
        self._short_entries = []
        self._ao_buffer = []

        self._ao = AwesomeOscillator()
        self._ao.ShortMa.Length = 5
        self._ao.LongMa.Length = 34

        self._stoch = StochasticOscillator()
        self._stoch.K.Length = self.KPeriod
        self._stoch.D.Length = self.DPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._ao, self._stoch, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ao)
            self.DrawIndicator(area, self._stoch)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ao_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not ao_value.IsFinal or not stoch_value.IsFinal:
            return

        # Get stochastic K value
        stoch_k_raw = stoch_value.K if hasattr(stoch_value, 'K') else None
        if stoch_k_raw is None:
            return
        stoch_k = float(stoch_k_raw)

        ao_val = float(ao_value)

        # Compute AC = AO - SMA(AO, 5) manually
        self._ao_buffer.append(ao_val)
        if len(self._ao_buffer) > self._ao_sma_length:
            self._ao_buffer = self._ao_buffer[-self._ao_sma_length:]

        if len(self._ao_buffer) < self._ao_sma_length:
            return

        ao_sma = sum(self._ao_buffer) / self._ao_sma_length
        ac = ao_val - ao_sma

        prev_ac = self._last_ac
        if prev_ac is None:
            self._last_ac = ac
            return

        if not self._ao.IsFormed or not self._stoch.IsFormed:
            self._last_ac = ac
            return

        pip = self._get_pip_size()
        stop_dist = self.StopLossPips * pip if self.StopLossPips > 0 else 0.0
        take_dist = self.TakeProfitPips * pip if self.TakeProfitPips > 0 else 0.0
        trail_dist = self.TrailingStopPips * pip if self.TrailingStopPips > 0 else 0.0
        trail_step = self.TrailingStepPips * pip if self.TrailingStepPips > 0 else 0.0
        use_trailing = self.TrailingStopPips > 0

        self._try_enter_long(candle, stoch_k, ac, prev_ac, stop_dist, take_dist)
        self._try_enter_short(candle, stoch_k, ac, prev_ac, stop_dist, take_dist)
        self._manage_longs(candle, stoch_k, ac, prev_ac, trail_dist, trail_step, use_trailing)
        self._manage_shorts(candle, stoch_k, ac, prev_ac, trail_dist, trail_step, use_trailing)

        self._last_ac = ac

    def _try_enter_long(self, candle, stoch_k, ac, prev_ac, stop_dist, take_dist):
        if len(self._long_entries) != 0 or len(self._short_entries) != 0:
            return
        if not (stoch_k > self.StochasticLevel and ac > prev_ac):
            return

        volume = self.OrderVolume
        if volume <= 0:
            return

        entry_price = float(candle.ClosePrice)

        self.BuyMarket()
        self._long_entries.append({"volume": volume, "entry": entry_price, "stop": None, "take": None, "layer": 0})

        for i in range(1, self.MaxLayers):
            self.BuyMarket()
            sp = entry_price - stop_dist if stop_dist > 0 else None
            tp = entry_price + take_dist * i if take_dist > 0 else None
            self._long_entries.append({"volume": volume, "entry": entry_price, "stop": sp, "take": tp, "layer": i})

    def _try_enter_short(self, candle, stoch_k, ac, prev_ac, stop_dist, take_dist):
        if len(self._short_entries) != 0 or len(self._long_entries) != 0:
            return
        if not (stoch_k < self.StochasticLevel and ac < prev_ac):
            return

        volume = self.OrderVolume
        if volume <= 0:
            return

        entry_price = float(candle.ClosePrice)

        self.SellMarket()
        self._short_entries.append({"volume": volume, "entry": entry_price, "stop": None, "take": None, "layer": 0})

        for i in range(1, self.MaxLayers):
            self.SellMarket()
            sp = entry_price + stop_dist if stop_dist > 0 else None
            tp = entry_price - take_dist * i if take_dist > 0 else None
            self._short_entries.append({"volume": volume, "entry": entry_price, "stop": sp, "take": tp, "layer": i})

    def _manage_longs(self, candle, stoch_k, ac, prev_ac, trail_dist, trail_step, use_trailing):
        if len(self._long_entries) == 0:
            return
        if self.Position <= 0:
            self._long_entries = []
            return

        close_price = float(candle.ClosePrice)
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        exit_signal = stoch_k < 50.0 and ac < prev_ac

        if use_trailing:
            if exit_signal:
                self._close_all_long(EXIT_MANUAL)
                return
            if trail_dist > 0:
                for e in self._long_entries:
                    profit = close_price - e["entry"]
                    if profit > trail_dist + trail_step:
                        new_stop = close_price - trail_dist
                        if e["stop"] is None or new_stop > e["stop"]:
                            e["stop"] = new_stop
        elif self._last_exit_was_tp:
            if exit_signal:
                self._close_all_long(EXIT_MANUAL)
                return
            for e in self._long_entries:
                if e["stop"] is None and close_price > e["entry"]:
                    e["stop"] = e["entry"]

        for e in self._long_entries:
            if e["stop"] is not None and low_price <= e["stop"]:
                self._close_all_long(EXIT_STOP_LOSS)
                return

        any_tp = False
        i = len(self._long_entries) - 1
        while i >= 0:
            e = self._long_entries[i]
            if e["take"] is not None and high_price >= e["take"]:
                self.SellMarket()
                self._long_entries.pop(i)
                any_tp = True
            i -= 1

        if any_tp:
            self._last_exit_was_tp = True

    def _manage_shorts(self, candle, stoch_k, ac, prev_ac, trail_dist, trail_step, use_trailing):
        if len(self._short_entries) == 0:
            return
        if self.Position >= 0:
            self._short_entries = []
            return

        close_price = float(candle.ClosePrice)
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        exit_signal = stoch_k > 50.0 and ac > prev_ac

        if use_trailing:
            if exit_signal:
                self._close_all_short(EXIT_MANUAL)
                return
            if trail_dist > 0:
                for e in self._short_entries:
                    profit = e["entry"] - close_price
                    if profit > trail_dist + trail_step:
                        new_stop = close_price + trail_dist
                        if e["stop"] is None or new_stop < e["stop"]:
                            e["stop"] = new_stop
        elif self._last_exit_was_tp:
            if exit_signal:
                self._close_all_short(EXIT_MANUAL)
                return
            for e in self._short_entries:
                if e["stop"] is None and close_price < e["entry"]:
                    e["stop"] = e["entry"]

        for e in self._short_entries:
            if e["stop"] is not None and high_price >= e["stop"]:
                self._close_all_short(EXIT_STOP_LOSS)
                return

        any_tp = False
        i = len(self._short_entries) - 1
        while i >= 0:
            e = self._short_entries[i]
            if e["take"] is not None and low_price <= e["take"]:
                self.BuyMarket()
                self._short_entries.pop(i)
                any_tp = True
            i -= 1

        if any_tp:
            self._last_exit_was_tp = True

    def _close_all_long(self, reason):
        volume = 0.0
        for e in self._long_entries:
            volume += e["volume"]
        if volume > 0 and self.Position > 0:
            self.SellMarket()
        self._long_entries = []
        if reason == EXIT_TAKE_PROFIT:
            self._last_exit_was_tp = True
        elif reason == EXIT_STOP_LOSS:
            self._last_exit_was_tp = False

    def _close_all_short(self, reason):
        volume = 0.0
        for e in self._short_entries:
            volume += e["volume"]
        if volume > 0 and self.Position < 0:
            self.BuyMarket()
        self._short_entries = []
        if reason == EXIT_TAKE_PROFIT:
            self._last_exit_was_tp = True
        elif reason == EXIT_STOP_LOSS:
            self._last_exit_was_tp = False

    def OnReseted(self):
        super(oz_fx_accelerator_stochastic_strategy, self).OnReseted()
        self._last_ac = None
        self._last_exit_was_tp = False
        self._pip_initialized = False
        self._pip_size = 0.0
        self._long_entries = []
        self._short_entries = []
        self._ao_buffer = []

    def CreateClone(self):
        return oz_fx_accelerator_stochastic_strategy()
