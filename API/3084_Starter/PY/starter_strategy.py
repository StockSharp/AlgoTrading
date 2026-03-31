import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import (
    CommodityChannelIndex, SimpleMovingAverage,
    ExponentialMovingAverage, SmoothedMovingAverage,
    WeightedMovingAverage
)
from StockSharp.Algo.Strategies import Strategy


class starter_strategy(Strategy):
    def __init__(self):
        super(starter_strategy, self).__init__()

        self._maximum_risk = self.Param("MaximumRisk", 0.02) \
            .SetDisplay("Maximum Risk", "Fraction of portfolio equity risked per trade", "Risk Management")
        self._decrease_factor = self.Param("DecreaseFactor", 3.0) \
            .SetDisplay("Decrease Factor", "Lot reduction factor after consecutive losses", "Risk Management")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "Number of bars for the Commodity Channel Index", "Indicators")
        self._cci_level = self.Param("CciLevel", 100.0) \
            .SetDisplay("CCI Level", "Threshold used for oversold/overbought detection", "Indicators")
        self._cci_current_bar = self.Param("CciCurrentBar", 0) \
            .SetDisplay("CCI Current Bar", "Shift for the current CCI value", "Indicators")
        self._cci_previous_bar = self.Param("CciPreviousBar", 1) \
            .SetDisplay("CCI Previous Bar", "Shift for the previous CCI value", "Indicators")
        self._ma_period = self.Param("MaPeriod", 120) \
            .SetDisplay("MA Period", "Number of bars for the moving average", "Indicators")
        self._ma_current_bar = self.Param("MaCurrentBar", 0) \
            .SetDisplay("MA Current Bar", "Shift for the moving average", "Indicators")
        self._ma_delta = self.Param("MaDelta", 0.001) \
            .SetDisplay("MA Delta", "Minimum slope difference between current and previous MA", "Signals")
        self._stop_loss_pips = self.Param("StopLossPips", 0.0) \
            .SetDisplay("Stop Loss (pips)", "Initial protective stop distance in pips", "Risk Management")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5.0) \
            .SetDisplay("Trailing Stop (pips)", "Base trailing distance in pips", "Risk Management")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0) \
            .SetDisplay("Trailing Step (pips)", "Minimum improvement required before moving the trailing stop", "Risk Management")

        self._cci = None
        self._moving_average = None
        self._cci_history = []
        self._ma_history = []
        self._pip_size = 0.0
        self._history_capacity = 0

        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop = None
        self._short_stop = None

        self._signed_position = 0.0
        self._last_entry_side = None
        self._last_entry_price = 0.0
        self._consecutive_losses = 0

    @property
    def maximum_risk(self):
        return self._maximum_risk.Value

    @property
    def decrease_factor(self):
        return self._decrease_factor.Value

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def cci_level(self):
        return self._cci_level.Value

    @property
    def cci_current_bar(self):
        return self._cci_current_bar.Value

    @property
    def cci_previous_bar(self):
        return self._cci_previous_bar.Value

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def ma_current_bar(self):
        return self._ma_current_bar.Value

    @property
    def ma_delta(self):
        return self._ma_delta.Value

    @property
    def stop_loss_pips(self):
        return self._stop_loss_pips.Value

    @property
    def trailing_stop_pips(self):
        return self._trailing_stop_pips.Value

    @property
    def trailing_step_pips(self):
        return self._trailing_step_pips.Value

    def OnReseted(self):
        super(starter_strategy, self).OnReseted()
        self._cci = None
        self._moving_average = None
        self._cci_history = []
        self._ma_history = []
        self._pip_size = 0.0
        self._history_capacity = 0
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop = None
        self._short_stop = None
        self._signed_position = 0.0
        self._last_entry_side = None
        self._last_entry_price = 0.0
        self._consecutive_losses = 0

    def OnStarted2(self, time):
        super(starter_strategy, self).OnStarted2(time)

        self._pip_size = self._get_pip_size()
        self._history_capacity = self._calc_history_capacity()
        self._cci_history = []
        self._ma_history = []
        self._long_entry_price = None
        self._short_entry_price = None
        self._long_stop = None
        self._short_stop = None
        self._signed_position = 0.0
        self._last_entry_side = None
        self._last_entry_price = 0.0
        self._consecutive_losses = 0

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period

        self._moving_average = SimpleMovingAverage()
        self._moving_average.Length = self.ma_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._cci, self._moving_average, self._on_process_candle)
        subscription.Start()

    def _on_process_candle(self, candle, cci_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._cci.IsFormed or not self._moving_average.IsFormed:
            return

        cci_val = float(cci_value)
        ma_val = float(ma_value)

        self._add_history(self._cci_history, cci_val)
        self._add_history(self._ma_history, ma_val)

        if self.Position != 0:
            self._update_trailing(candle)
            self._check_protective_stops(candle)

        if self.Position != 0:
            return

        ma_current = self._try_get_history(self._ma_history, self.ma_current_bar)
        ma_previous = self._try_get_history(self._ma_history, self.ma_current_bar + 1)
        if ma_current is None or ma_previous is None:
            return

        cci_current = self._try_get_history(self._cci_history, self.cci_current_bar)
        cci_previous = self._try_get_history(self._cci_history, self.cci_previous_bar)
        if cci_current is None or cci_previous is None:
            return

        ma_slope = ma_current - ma_previous
        cci_lev = float(self.cci_level)
        ma_d = float(self.ma_delta)

        if (ma_slope > ma_d and cci_current > cci_previous and
                cci_current > -cci_lev and cci_previous < -cci_lev):
            self._try_enter_long(float(candle.ClosePrice))
        elif (ma_slope < -ma_d and cci_current < cci_previous and
              cci_current < cci_lev and cci_previous > cci_lev):
            self._try_enter_short(float(candle.ClosePrice))

    def _try_enter_long(self, price):
        volume = self._calculate_trade_volume(price)
        if volume <= 0.0:
            return
        self.BuyMarket(volume)

    def _try_enter_short(self, price):
        volume = self._calculate_trade_volume(price)
        if volume <= 0.0:
            return
        self.SellMarket(volume)

    def _check_protective_stops(self, candle):
        if self.Position > 0 and self._long_stop is not None and float(candle.LowPrice) <= self._long_stop:
            vol = abs(float(self.Position))
            if vol > 0:
                self.SellMarket(vol)
            self._reset_long_protection()
            return

        if self.Position < 0 and self._short_stop is not None and float(candle.HighPrice) >= self._short_stop:
            vol = abs(float(self.Position))
            if vol > 0:
                self.BuyMarket(vol)
            self._reset_short_protection()

    def _update_trailing(self, candle):
        trail_pips = float(self.trailing_stop_pips)
        if trail_pips <= 0.0 or self._pip_size <= 0.0:
            return

        offset = trail_pips * self._pip_size
        step_pips = float(self.trailing_step_pips)
        step = step_pips * self._pip_size

        close = float(candle.ClosePrice)

        if self.Position > 0 and self._long_entry_price is not None:
            target_stop = close - offset
            threshold = close - (offset + step)
            if self._long_stop is None or self._long_stop < threshold:
                self._long_stop = target_stop

        elif self.Position < 0 and self._short_entry_price is not None:
            target_stop = close + offset
            threshold = close + (offset + step)
            if self._short_stop is None or self._short_stop > threshold:
                self._short_stop = target_stop

    def _calculate_trade_volume(self, price):
        base_volume = float(self.Volume) if self.Volume > 0 else 1.0
        if price <= 0.0:
            return self._normalize_volume(base_volume)

        equity = float(self.Portfolio.CurrentValue) if self.Portfolio is not None and self.Portfolio.CurrentValue is not None else 0.0
        max_risk = float(self.maximum_risk)
        if equity <= 0.0 or max_risk <= 0.0:
            return self._normalize_volume(base_volume)

        volume = equity * max_risk / price

        dec_factor = float(self.decrease_factor)
        if dec_factor > 0.0 and self._consecutive_losses > 1:
            reduction = volume * self._consecutive_losses / dec_factor
            volume -= reduction

        if volume <= 0.0:
            volume = base_volume

        return self._normalize_volume(volume)

    def _normalize_volume(self, volume):
        if self.Security is not None and self.Security.VolumeStep is not None:
            step = float(self.Security.VolumeStep)
            if step <= 0.0:
                step = 1.0
            if volume < step:
                volume = step
            steps = math.floor(volume / step)
            if steps < 1.0:
                steps = 1.0
            volume = steps * step

        if volume <= 0.0:
            volume = 1.0
        return volume

    def _add_history(self, history, value):
        history.append(value)
        if len(history) > self._history_capacity:
            del history[:len(history) - self._history_capacity]

    def _try_get_history(self, history, shift):
        if shift < 0:
            return None
        index = len(history) - 1 - shift
        if index < 0 or index >= len(history):
            return None
        return history[index]

    def _reset_long_protection(self):
        self._long_entry_price = None
        self._long_stop = None

    def _reset_short_protection(self):
        self._short_entry_price = None
        self._short_stop = None

    def _get_pip_size(self):
        if self.Security is None:
            return 0.0
        step = float(self.Security.PriceStep) if self.Security.PriceStep is not None else 0.0
        if step <= 0.0:
            return 0.0
        return step

    def _calc_history_capacity(self):
        cci_req = max(self.cci_current_bar, self.cci_previous_bar) + self.cci_period + 5
        ma_req = self.ma_current_bar + self.ma_period + 5
        return max(cci_req, ma_req)

    def OnOwnTradeReceived(self, trade):
        super(starter_strategy, self).OnOwnTradeReceived(trade)

        volume = float(trade.Trade.Volume)
        if volume <= 0.0:
            return

        delta = volume if trade.Order.Side == Sides.Buy else -volume
        previous_position = self._signed_position
        self._signed_position += delta

        if previous_position == 0.0 and self._signed_position != 0.0:
            self._last_entry_side = trade.Order.Side
            self._last_entry_price = float(trade.Trade.Price)

            sl_pips = float(self.stop_loss_pips)
            if self._last_entry_side == Sides.Buy:
                self._long_entry_price = self._last_entry_price
                self._long_stop = (self._last_entry_price - sl_pips * self._pip_size) if (sl_pips > 0.0 and self._pip_size > 0.0) else None
                self._reset_short_protection()
            else:
                self._short_entry_price = self._last_entry_price
                self._short_stop = (self._last_entry_price + sl_pips * self._pip_size) if (sl_pips > 0.0 and self._pip_size > 0.0) else None
                self._reset_long_protection()

        elif previous_position != 0.0 and self._signed_position == 0.0:
            exit_price = float(trade.Trade.Price)

            if self._last_entry_side is not None and self._last_entry_price != 0.0:
                if self._last_entry_side == Sides.Buy:
                    profit = exit_price - self._last_entry_price
                else:
                    profit = self._last_entry_price - exit_price

                if profit > 0.0:
                    self._consecutive_losses = 0
                elif profit < 0.0:
                    self._consecutive_losses += 1

            self._last_entry_side = None
            self._last_entry_price = 0.0
            self._reset_long_protection()
            self._reset_short_protection()

    def CreateClone(self):
        return starter_strategy()
