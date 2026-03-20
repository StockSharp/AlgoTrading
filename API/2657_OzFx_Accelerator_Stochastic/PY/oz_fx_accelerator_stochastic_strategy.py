import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    AwesomeOscillator, SimpleMovingAverage, StochasticOscillator, DecimalIndicatorValue
)


class oz_fx_accelerator_stochastic_strategy(Strategy):
    """OzFx strategy: stacks layered entries when AC oscillator and stochastic agree."""

    def __init__(self):
        super(oz_fx_accelerator_stochastic_strategy, self).__init__()

        self._max_layers = self.Param("MaxLayers", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Layers", "Maximum number of layered positions", "Risk")
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

    @property
    def MaxLayers(self):
        return int(self._max_layers.Value)
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
    def StochasticLevel(self):
        return float(self._stochastic_level.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _get_pip_size(self):
        if self._pip_initialized:
            return self._pip_size
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0001
        decimals = sec.Decimals if sec is not None and sec.Decimals is not None else 0
        adjust = 10.0 if decimals == 3 or decimals == 5 else 1.0
        self._pip_size = step * adjust
        if self._pip_size <= 0:
            self._pip_size = step
        if self._pip_size <= 0:
            self._pip_size = 0.0001
        self._pip_initialized = True
        return self._pip_size

    def OnStarted(self, time):
        super(oz_fx_accelerator_stochastic_strategy, self).OnStarted(time)

        self._last_ac = None
        self._last_exit_was_tp = False
        self._pip_initialized = False
        self._long_entries = []
        self._short_entries = []

        self._ao = AwesomeOscillator()
        self._ao.ShortMa.Length = 5
        self._ao.LongMa.Length = 34
        self._ao_sma = SimpleMovingAverage()
        self._ao_sma.Length = 5
        self._stoch = StochasticOscillator()
        self._stoch.K.Length = self.KPeriod
        self._stoch.D.Length = self.DPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._ao, self._stoch, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ao)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ao_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not ao_value.IsFinal or not stoch_value.IsFinal:
            return

        stoch_k = stoch_value.K
        if stoch_k is None:
            return

        sk = float(stoch_k)
        ao = float(ao_value.GetValue[float]())

        ao_sma_result = self._ao_sma.Process(DecimalIndicatorValue(self._ao_sma, ao, candle.OpenTime))
        if not self._ao_sma.IsFormed:
            return

        ac = ao - float(ao_sma_result.GetValue[float]())
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

        self._try_enter_long(candle, sk, ac, prev_ac, stop_dist, take_dist)
        self._try_enter_short(candle, sk, ac, prev_ac, stop_dist, take_dist)
        self._manage_longs(candle, sk, ac, prev_ac, trail_dist, trail_step, use_trailing)
        self._manage_shorts(candle, sk, ac, prev_ac, trail_dist, trail_step, use_trailing)

        self._last_ac = ac

    def _try_enter_long(self, candle, sk, ac, prev_ac, stop_dist, take_dist):
        if len(self._long_entries) != 0 or len(self._short_entries) != 0:
            return
        if not (sk > self.StochasticLevel and ac > prev_ac):
            return

        entry = float(candle.ClosePrice)
        self.BuyMarket()
        self._long_entries.append({"entry": entry, "stop": None, "take": None, "layer": 0})

        for i in range(1, self.MaxLayers):
            self.BuyMarket()
            sp = entry - stop_dist if stop_dist > 0 else None
            tp = entry + take_dist * i if take_dist > 0 else None
            self._long_entries.append({"entry": entry, "stop": sp, "take": tp, "layer": i})

    def _try_enter_short(self, candle, sk, ac, prev_ac, stop_dist, take_dist):
        if len(self._short_entries) != 0 or len(self._long_entries) != 0:
            return
        if not (sk < self.StochasticLevel and ac < prev_ac):
            return

        entry = float(candle.ClosePrice)
        self.SellMarket()
        self._short_entries.append({"entry": entry, "stop": None, "take": None, "layer": 0})

        for i in range(1, self.MaxLayers):
            self.SellMarket()
            sp = entry + stop_dist if stop_dist > 0 else None
            tp = entry - take_dist * i if take_dist > 0 else None
            self._short_entries.append({"entry": entry, "stop": sp, "take": tp, "layer": i})

    def _manage_longs(self, candle, sk, ac, prev_ac, trail_dist, trail_step, use_trailing):
        if len(self._long_entries) == 0:
            return
        if self.Position <= 0:
            self._long_entries = []
            return

        close = float(candle.ClosePrice)
        lo = float(candle.LowPrice)
        h = float(candle.HighPrice)
        exit_signal = sk < 50.0 and ac < prev_ac

        if use_trailing:
            if exit_signal:
                self._close_all_long()
                return
            if trail_dist > 0:
                for e in self._long_entries:
                    profit = close - e["entry"]
                    if profit > trail_dist + trail_step:
                        new_stop = close - trail_dist
                        if e["stop"] is None or new_stop > e["stop"]:
                            e["stop"] = new_stop
        elif self._last_exit_was_tp:
            if exit_signal:
                self._close_all_long()
                return
            for e in self._long_entries:
                if e["stop"] is None and close > e["entry"]:
                    e["stop"] = e["entry"]

        for e in self._long_entries:
            if e["stop"] is not None and lo <= e["stop"]:
                self._close_all_long()
                return

        any_tp = False
        i = len(self._long_entries) - 1
        while i >= 0:
            e = self._long_entries[i]
            if e["take"] is not None and h >= e["take"]:
                self.SellMarket()
                self._long_entries.pop(i)
                any_tp = True
            i -= 1

        if any_tp:
            self._last_exit_was_tp = True

    def _manage_shorts(self, candle, sk, ac, prev_ac, trail_dist, trail_step, use_trailing):
        if len(self._short_entries) == 0:
            return
        if self.Position >= 0:
            self._short_entries = []
            return

        close = float(candle.ClosePrice)
        lo = float(candle.LowPrice)
        h = float(candle.HighPrice)
        exit_signal = sk > 50.0 and ac > prev_ac

        if use_trailing:
            if exit_signal:
                self._close_all_short()
                return
            if trail_dist > 0:
                for e in self._short_entries:
                    profit = e["entry"] - close
                    if profit > trail_dist + trail_step:
                        new_stop = close + trail_dist
                        if e["stop"] is None or new_stop < e["stop"]:
                            e["stop"] = new_stop
        elif self._last_exit_was_tp:
            if exit_signal:
                self._close_all_short()
                return
            for e in self._short_entries:
                if e["stop"] is None and close < e["entry"]:
                    e["stop"] = e["entry"]

        for e in self._short_entries:
            if e["stop"] is not None and h >= e["stop"]:
                self._close_all_short()
                return

        any_tp = False
        i = len(self._short_entries) - 1
        while i >= 0:
            e = self._short_entries[i]
            if e["take"] is not None and lo <= e["take"]:
                self.BuyMarket()
                self._short_entries.pop(i)
                any_tp = True
            i -= 1

        if any_tp:
            self._last_exit_was_tp = True

    def _close_all_long(self):
        if self.Position > 0:
            self.SellMarket()
        self._long_entries = []
        self._last_exit_was_tp = False

    def _close_all_short(self):
        if self.Position < 0:
            self.BuyMarket()
        self._short_entries = []
        self._last_exit_was_tp = False

    def OnReseted(self):
        super(oz_fx_accelerator_stochastic_strategy, self).OnReseted()
        self._last_ac = None
        self._last_exit_was_tp = False
        self._pip_initialized = False
        self._pip_size = 0.0
        self._long_entries = []
        self._short_entries = []

    def CreateClone(self):
        return oz_fx_accelerator_stochastic_strategy()
