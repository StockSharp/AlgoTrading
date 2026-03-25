import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class master_mm_droid_strategy(Strategy):
    def __init__(self):
        super(master_mm_droid_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._rsi_lower_level = self.Param("RsiLowerLevel", 25.0)
        self._rsi_upper_level = self.Param("RsiUpperLevel", 75.0)
        self._rsi_max_entries = self.Param("RsiMaxEntries", 2)
        self._rsi_pyramid_steps = self.Param("RsiPyramidSteps", 250.0)
        self._stop_loss_steps = self.Param("StopLossSteps", 500.0)
        self._trailing_steps = self.Param("TrailingSteps", 700.0)
        self._box_lookback = self.Param("BoxLookback", 16)
        self._box_entry_steps = self.Param("BoxEntrySteps", 180.0)

        self._previous_rsi = 0.0
        self._has_previous_rsi = False
        self._last_entry_price = None
        self._entry_count = 0
        self._active_stop_price = None
        self._best_price = 0.0
        self._box_high = 0.0
        self._box_low = 999999999.0
        self._box_bars_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(master_mm_droid_strategy, self).OnStarted(time)

        self._previous_rsi = 0.0
        self._has_previous_rsi = False
        self._last_entry_price = None
        self._entry_count = 0
        self._active_stop_price = None
        self._best_price = 0.0
        self._box_high = 0.0
        self._box_low = 999999999.0
        self._box_bars_count = 0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_period.Value)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0

        entered_this_candle = False

        self._update_box(candle)
        self._manage_trailing(candle, step)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_rsi = rsi_val
            self._has_previous_rsi = True
            return

        vol = float(self.Volume)
        box_lookback = int(self._box_lookback.Value)

        pos = float(self.Position)
        if pos == 0 and self._box_bars_count >= box_lookback:
            box_offset = float(self._box_entry_steps.Value) * step
            if close > self._box_high + box_offset:
                self.BuyMarket(vol)
                self._last_entry_price = close
                self._entry_count = 1
                self._active_stop_price = close - float(self._stop_loss_steps.Value) * step
                self._best_price = close
                entered_this_candle = True
            elif close < self._box_low - box_offset:
                self.SellMarket(vol)
                self._last_entry_price = close
                self._entry_count = 1
                self._active_stop_price = close + float(self._stop_loss_steps.Value) * step
                self._best_price = close
                entered_this_candle = True

        if not entered_this_candle and self._has_previous_rsi and self._rsi.IsFormed:
            rsi_lower = float(self._rsi_lower_level.Value)
            rsi_upper = float(self._rsi_upper_level.Value)
            rsi_cross_up = self._previous_rsi <= rsi_lower and rsi_val > rsi_lower
            rsi_cross_down = self._previous_rsi >= rsi_upper and rsi_val < rsi_upper

            pos = float(self.Position)
            if rsi_cross_up and pos <= 0:
                entry_vol = vol + (abs(pos) if pos < 0 else 0.0)
                self.BuyMarket(entry_vol)
                self._last_entry_price = close
                self._entry_count = 1
                self._active_stop_price = close - float(self._stop_loss_steps.Value) * step
                self._best_price = close
            elif rsi_cross_down and pos >= 0:
                entry_vol = vol + (pos if pos > 0 else 0.0)
                self.SellMarket(entry_vol)
                self._last_entry_price = close
                self._entry_count = 1
                self._active_stop_price = close + float(self._stop_loss_steps.Value) * step
                self._best_price = close

            pyramid_dist = float(self._rsi_pyramid_steps.Value) * step
            max_entries = int(self._rsi_max_entries.Value)
            pos = float(self.Position)
            if pos > 0 and self._entry_count < max_entries and self._last_entry_price is not None:
                if close >= self._last_entry_price + pyramid_dist:
                    self.BuyMarket(vol)
                    self._last_entry_price = close
                    self._entry_count += 1
            elif pos < 0 and self._entry_count < max_entries and self._last_entry_price is not None:
                if close <= self._last_entry_price - pyramid_dist:
                    self.SellMarket(vol)
                    self._last_entry_price = close
                    self._entry_count += 1

        self._previous_rsi = rsi_val
        self._has_previous_rsi = True

    def _update_box(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        self._box_bars_count += 1
        if high > self._box_high:
            self._box_high = high
        if low < self._box_low:
            self._box_low = low

    def _manage_trailing(self, candle, step):
        pos = float(self.Position)
        if pos == 0:
            self._active_stop_price = None
            return

        if self._active_stop_price is None:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        trail_dist = float(self._trailing_steps.Value) * step

        if pos > 0:
            if close > self._best_price:
                self._best_price = close
            trail_stop = self._best_price - trail_dist
            if trail_stop > self._active_stop_price:
                self._active_stop_price = trail_stop
            if low <= self._active_stop_price:
                self.SellMarket(pos)
                self._active_stop_price = None
                self._last_entry_price = None
                self._entry_count = 0
        else:
            if close < self._best_price or self._best_price == 0.0:
                self._best_price = close
            trail_stop = self._best_price + trail_dist
            if trail_stop < self._active_stop_price:
                self._active_stop_price = trail_stop
            if high >= self._active_stop_price:
                self.BuyMarket(abs(pos))
                self._active_stop_price = None
                self._last_entry_price = None
                self._entry_count = 0

    def OnReseted(self):
        super(master_mm_droid_strategy, self).OnReseted()
        self._previous_rsi = 0.0
        self._has_previous_rsi = False
        self._last_entry_price = None
        self._entry_count = 0
        self._active_stop_price = None
        self._best_price = 0.0
        self._box_high = 0.0
        self._box_low = 999999999.0
        self._box_bars_count = 0

    def CreateClone(self):
        return master_mm_droid_strategy()
