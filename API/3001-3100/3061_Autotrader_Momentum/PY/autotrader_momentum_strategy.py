import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class autotrader_momentum_strategy(Strategy):
    def __init__(self):
        super(autotrader_momentum_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe for price comparisons", "Data")
        self._trade_volume = self.Param("TradeVolume", Decimal(1)) \
            .SetDisplay("Trade Volume", "Base order volume used for market entries", "Trading") \
            .SetGreaterThanZero()
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk") \
            .SetNotNegative()
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Take Profit (pips)", "Profit target distance expressed in pips", "Risk") \
            .SetNotNegative()
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0) \
            .SetDisplay("Trailing Stop (pips)", "Distance maintained by trailing stop", "Risk") \
            .SetNotNegative()
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Trailing Step (pips)", "Minimum progress before trailing stop advances", "Risk") \
            .SetNotNegative()
        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetDisplay("Cooldown Bars", "Bars to wait after entries and exits", "Risk") \
            .SetNotNegative()
        self._current_bar_index = self.Param("CurrentBarIndex", 0) \
            .SetDisplay("Current Bar Index", "Index of signal source candle", "Logic") \
            .SetNotNegative()
        self._comparable_bar_index = self.Param("ComparableBarIndex", 8) \
            .SetDisplay("Comparable Bar Index", "Historical candle index for comparison", "Logic") \
            .SetNotNegative()

        self._close_history = []
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._is_long_position = False
        self._pip_value = Decimal(0)
        self._stop_loss_offset = Decimal(0)
        self._take_profit_offset = Decimal(0)
        self._trailing_stop_offset = Decimal(0)
        self._trailing_step_offset = Decimal(0)
        self._cooldown_left = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def TradeVolume(self):
        return self._trade_volume.Value
    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value
    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value
    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value
    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value
    @property
    def CurrentBarIndex(self):
        return self._current_bar_index.Value
    @property
    def ComparableBarIndex(self):
        return self._comparable_bar_index.Value

    def OnReseted(self):
        super(autotrader_momentum_strategy, self).OnReseted()
        self._close_history = []
        self._reset_position_state()
        self._pip_value = Decimal(0)
        self._stop_loss_offset = Decimal(0)
        self._take_profit_offset = Decimal(0)
        self._trailing_stop_offset = Decimal(0)
        self._trailing_step_offset = Decimal(0)
        self._cooldown_left = 0

    def OnStarted2(self, time):
        super(autotrader_momentum_strategy, self).OnStarted2(time)
        self._close_history = []
        self._reset_position_state()
        self.Volume = self.TradeVolume
        self._pip_value = self._calculate_pip_value()
        sl = self.StopLossPips
        tp = self.TakeProfitPips
        ts = self.TrailingStopPips
        tstep = self.TrailingStepPips
        self._stop_loss_offset = Decimal(sl) * self._pip_value if sl > 0 else Decimal(0)
        self._take_profit_offset = Decimal(tp) * self._pip_value if tp > 0 else Decimal(0)
        self._trailing_stop_offset = Decimal(ts) * self._pip_value if ts > 0 else Decimal(0)
        self._trailing_step_offset = Decimal(tstep) * self._pip_value if tstep > 0 else Decimal(0)
        self._cooldown_left = 0
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_left > 0:
            self._cooldown_left -= 1

        self._update_trailing_stop(candle)
        exit_triggered = self._manage_protective_exits(candle)

        self._update_close_history(candle.ClosePrice)

        if exit_triggered:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_left > 0:
            return

        required_history = max(self.CurrentBarIndex, self.ComparableBarIndex) + 1
        if len(self._close_history) < required_history:
            return

        current_close = self._get_close_at_index(self.CurrentBarIndex)
        comparable_close = self._get_close_at_index(self.ComparableBarIndex)
        if current_close is None or comparable_close is None:
            return

        if current_close > comparable_close and self.Position <= 0:
            self._enter_position(True, candle)
        elif current_close < comparable_close and self.Position >= 0:
            self._enter_position(False, candle)

    def _update_close_history(self, close_price):
        max_count = max(self.CurrentBarIndex, self.ComparableBarIndex) + 1
        if max_count <= 0:
            max_count = 1
        self._close_history.append(close_price)
        if len(self._close_history) > max_count:
            self._close_history.pop(0)

    def _get_close_at_index(self, index_from_current):
        if index_from_current < 0:
            return None
        target = len(self._close_history) - 1 - index_from_current
        if target < 0 or target >= len(self._close_history):
            return None
        return self._close_history[target]

    def _enter_position(self, is_long, candle):
        base_volume = self.TradeVolume
        if base_volume <= Decimal(0):
            return

        previous_position = self.Position

        if is_long:
            volume = base_volume
            if previous_position < Decimal(0):
                volume = volume + Math.Abs(previous_position)
            if volume <= Decimal(0):
                return
            self.BuyMarket(volume)
            if previous_position <= Decimal(0):
                self._entry_price = candle.ClosePrice
            else:
                existing_volume = previous_position
                total_volume = existing_volume + base_volume
                if total_volume > Decimal(0):
                    existing_entry = self._entry_price if self._entry_price is not None else candle.ClosePrice
                    self._entry_price = (existing_entry * existing_volume + candle.ClosePrice * base_volume) / total_volume
            self._is_long_position = True
        else:
            volume = base_volume
            if previous_position > Decimal(0):
                volume = volume + previous_position
            if volume <= Decimal(0):
                return
            self.SellMarket(volume)
            if previous_position >= Decimal(0):
                self._entry_price = candle.ClosePrice
            else:
                existing_volume = Math.Abs(previous_position)
                total_volume = existing_volume + base_volume
                if total_volume > Decimal(0):
                    existing_entry = self._entry_price if self._entry_price is not None else candle.ClosePrice
                    self._entry_price = (existing_entry * existing_volume + candle.ClosePrice * base_volume) / total_volume
            self._is_long_position = False

        self._stop_price = self._calc_stop_price(self._is_long_position, self._entry_price)
        self._take_profit_price = self._calc_take_profit(self._is_long_position, self._entry_price)
        self._cooldown_left = self.CooldownBars

    def _calc_stop_price(self, is_long, entry_price):
        if entry_price is None or self._stop_loss_offset <= Decimal(0):
            return None
        if is_long:
            return entry_price - self._stop_loss_offset
        else:
            return entry_price + self._stop_loss_offset

    def _calc_take_profit(self, is_long, entry_price):
        if entry_price is None or self._take_profit_offset <= Decimal(0):
            return None
        if is_long:
            return entry_price + self._take_profit_offset
        else:
            return entry_price - self._take_profit_offset

    def _update_trailing_stop(self, candle):
        if self._trailing_stop_offset <= Decimal(0) or self._trailing_step_offset <= Decimal(0) or self._entry_price is None:
            return

        if self.Position > Decimal(0):
            progress = candle.HighPrice - self._entry_price
            if progress <= self._trailing_stop_offset + self._trailing_step_offset:
                return
            desired_stop = candle.ClosePrice - self._trailing_stop_offset
            if self._stop_price is not None:
                if desired_stop - self._stop_price >= self._trailing_step_offset:
                    self._stop_price = desired_stop
            else:
                self._stop_price = desired_stop
        elif self.Position < Decimal(0):
            progress = self._entry_price - candle.LowPrice
            if progress <= self._trailing_stop_offset + self._trailing_step_offset:
                return
            desired_stop = candle.ClosePrice + self._trailing_stop_offset
            if self._stop_price is not None:
                if self._stop_price - desired_stop >= self._trailing_step_offset:
                    self._stop_price = desired_stop
            else:
                self._stop_price = desired_stop

    def _manage_protective_exits(self, candle):
        if self.Position > Decimal(0):
            if self._stop_price is not None and candle.LowPrice <= self._stop_price:
                self.SellMarket(self.Position)
                self._reset_position_state()
                self._cooldown_left = self.CooldownBars
                return True
            if self._take_profit_price is not None and candle.HighPrice >= self._take_profit_price:
                self.SellMarket(self.Position)
                self._reset_position_state()
                self._cooldown_left = self.CooldownBars
                return True
        elif self.Position < Decimal(0):
            volume = Math.Abs(self.Position)
            if self._stop_price is not None and candle.HighPrice >= self._stop_price:
                self.BuyMarket(volume)
                self._reset_position_state()
                self._cooldown_left = self.CooldownBars
                return True
            if self._take_profit_price is not None and candle.LowPrice <= self._take_profit_price:
                self.BuyMarket(volume)
                self._reset_position_state()
                self._cooldown_left = self.CooldownBars
                return True
        else:
            self._reset_position_state()
        return False

    def _reset_position_state(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._is_long_position = False

    def _calculate_pip_value(self):
        sec = self.Security
        step = sec.PriceStep if sec is not None and sec.PriceStep is not None else Decimal(0)
        if step <= Decimal(0):
            return Decimal(1)
        scaled = step
        digits = 0
        while scaled < Decimal(1) and digits < 10:
            scaled = scaled * Decimal(10)
            digits += 1
        adjust = Decimal(10) if (digits == 3 or digits == 5) else Decimal(1)
        return step * adjust

    def CreateClone(self):
        return autotrader_momentum_strategy()
