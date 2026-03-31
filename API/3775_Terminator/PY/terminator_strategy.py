import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


# Direction constants
SIDE_BUY = 0
SIDE_SELL = 1


class terminator_strategy(Strategy):
    """Grid-based martingale strategy using MACD slope for direction.
    Manages averaging entries with increasing lot sizes and protective stops."""

    def __init__(self):
        super(terminator_strategy, self).__init__()

        self._take_profit_pips = self.Param("TakeProfitPips", 38.0) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._lot_size = self.Param("LotSize", 0.1) \
            .SetDisplay("Base Lot Size", "Fixed lot size when money management is disabled", "Risk")
        self._initial_stop_pips = self.Param("InitialStopPips", 0.0) \
            .SetDisplay("Initial Stop (pips)", "Initial protective stop distance in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
        self._max_trades = self.Param("MaxTrades", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Trades", "Maximum simultaneous martingale trades", "General")
        self._entry_distance_pips = self.Param("EntryDistancePips", 18.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Entry Distance (pips)", "Adverse move required before adding a position", "General")
        self._reverse_signals = self.Param("ReverseSignals", False) \
            .SetDisplay("Reverse Signals", "Reverse the MACD slope interpretation", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for signal generation", "General")
        self._macd_fast_length = self.Param("MacdFastLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast", "Fast EMA period used in MACD", "Filters")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow", "Slow EMA period used in MACD", "Filters")
        self._macd_signal_length = self.Param("MacdSignalLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "Signal EMA period used in MACD", "Filters")

        self._previous_macd = None
        self._previous_previous_macd = None
        self._open_trades = 0
        self._is_long_position = False
        self._last_entry_price = 0.0
        self._average_price = 0.0
        self._open_volume = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._pip_size = 0.0
        self._current_direction = None
        self._continue_opening = True

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def LotSize(self):
        return self._lot_size.Value

    @property
    def InitialStopPips(self):
        return self._initial_stop_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def MaxTrades(self):
        return self._max_trades.Value

    @property
    def EntryDistancePips(self):
        return self._entry_distance_pips.Value

    @property
    def ReverseSignals(self):
        return self._reverse_signals.Value

    @property
    def MacdFastLength(self):
        return self._macd_fast_length.Value

    @property
    def MacdSlowLength(self):
        return self._macd_slow_length.Value

    @property
    def MacdSignalLength(self):
        return self._macd_signal_length.Value

    def OnReseted(self):
        super(terminator_strategy, self).OnReseted()
        self._previous_macd = None
        self._previous_previous_macd = None
        self._open_trades = 0
        self._is_long_position = False
        self._last_entry_price = 0.0
        self._average_price = 0.0
        self._open_volume = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._pip_size = 0.0
        self._current_direction = None
        self._continue_opening = True

    def _to_price(self, pips):
        return float(pips) * self._pip_size

    def OnStarted2(self, time):
        super(terminator_strategy, self).OnStarted2(time)

        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is None or float(step) <= 0:
            step = 0.0001
        self._pip_size = float(step)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.MacdFastLength
        macd.Macd.LongMa.Length = self.MacdSlowLength
        macd.SignalMa.Length = self.MacdSignalLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, self._process_candle).Start()

    def _process_candle(self, candle, indicator_value):
        if candle.State != CandleStates.Finished:
            return

        macd_raw = indicator_value.Macd if hasattr(indicator_value, 'Macd') else None
        if macd_raw is None:
            return

        macd_main = float(macd_raw)
        prev_macd = self._previous_macd
        prev_prev_macd = self._previous_previous_macd

        self._previous_previous_macd = prev_macd
        self._previous_macd = macd_main

        current_price = float(candle.ClosePrice)

        # Manage existing basket
        if self._open_trades > 0:
            self._manage_open_position(current_price)
            if self._open_trades == 0:
                return

        self._continue_opening = self._open_trades < self.MaxTrades
        if not self._continue_opening:
            return

        if self._open_trades == 0:
            direction = self._determine_direction(prev_macd, prev_prev_macd)
            if direction is not None:
                self._current_direction = direction
                self._try_open_position(direction, current_price)
        elif self._current_direction is not None:
            self._try_add_position(self._current_direction, current_price)

    def _determine_direction(self, macd_prev, macd_prev_prev):
        if macd_prev is None or macd_prev_prev is None:
            return None
        is_bullish = macd_prev > macd_prev_prev
        is_bearish = macd_prev < macd_prev_prev
        if not is_bullish and not is_bearish:
            return None
        if self.ReverseSignals:
            return SIDE_SELL if is_bullish else SIDE_BUY
        return SIDE_BUY if is_bullish else SIDE_SELL

    def _try_open_position(self, direction, current_price):
        if direction == SIDE_BUY:
            self.BuyMarket()
            self._record_entry(True, current_price)
        elif direction == SIDE_SELL:
            self.SellMarket()
            self._record_entry(False, current_price)

    def _record_entry(self, is_long, price):
        vol = float(self.LotSize)
        new_volume = self._open_volume + vol
        if new_volume > 0:
            self._average_price = (self._average_price * self._open_volume + price * vol) / new_volume
        self._open_volume = new_volume
        self._is_long_position = is_long
        self._open_trades += 1
        self._last_entry_price = price

        # Update stop
        if float(self.InitialStopPips) > 0:
            stop_offset = self._to_price(self.InitialStopPips)
            if is_long:
                candidate = price - stop_offset
                if self._stop_loss_price is None or candidate < self._stop_loss_price:
                    self._stop_loss_price = candidate
            else:
                candidate = price + stop_offset
                if self._stop_loss_price is None or candidate > self._stop_loss_price:
                    self._stop_loss_price = candidate

        # Update take profit
        if float(self.TakeProfitPips) > 0:
            tp_offset = self._to_price(self.TakeProfitPips)
            if is_long:
                candidate = price + tp_offset
                if self._take_profit_price is None or candidate > self._take_profit_price:
                    self._take_profit_price = candidate
            else:
                candidate = price - tp_offset
                if self._take_profit_price is None or candidate < self._take_profit_price:
                    self._take_profit_price = candidate

        self._continue_opening = self._open_trades < self.MaxTrades

    def _try_add_position(self, direction, current_price):
        distance = self._to_price(self.EntryDistancePips)
        if direction == SIDE_BUY:
            can_add = (self._last_entry_price - current_price) >= distance
        else:
            can_add = (current_price - self._last_entry_price) >= distance
        if not can_add:
            return
        self._try_open_position(direction, current_price)

    def _manage_open_position(self, current_price):
        if self._open_volume <= 0:
            return

        # Check stop loss
        if self._stop_loss_price is not None:
            if self._is_long_position and current_price <= self._stop_loss_price:
                self.SellMarket()
                self._reset_position_state()
                return
            if not self._is_long_position and current_price >= self._stop_loss_price:
                self.BuyMarket()
                self._reset_position_state()
                return

        # Check take profit
        if self._take_profit_price is not None:
            if self._is_long_position and current_price >= self._take_profit_price:
                self.SellMarket()
                self._reset_position_state()
                return
            if not self._is_long_position and current_price <= self._take_profit_price:
                self.BuyMarket()
                self._reset_position_state()
                return

        # Trailing stop
        if float(self.TrailingStopPips) > 0:
            self._update_trailing_stop(current_price)

    def _update_trailing_stop(self, current_price):
        trailing_distance = self._to_price(self.TrailingStopPips)
        threshold = trailing_distance + self._to_price(self.EntryDistancePips)

        if self._is_long_position:
            profit = current_price - self._average_price
            if profit >= threshold:
                new_stop = current_price - trailing_distance
                if self._stop_loss_price is None or new_stop > self._stop_loss_price:
                    self._stop_loss_price = new_stop
        else:
            profit = self._average_price - current_price
            if profit >= threshold:
                new_stop = current_price + trailing_distance
                if self._stop_loss_price is None or new_stop < self._stop_loss_price:
                    self._stop_loss_price = new_stop

    def _reset_position_state(self):
        self._open_volume = 0.0
        self._average_price = 0.0
        self._open_trades = 0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._last_entry_price = 0.0
        self._continue_opening = True
        self._current_direction = None

    def CreateClone(self):
        return terminator_strategy()
