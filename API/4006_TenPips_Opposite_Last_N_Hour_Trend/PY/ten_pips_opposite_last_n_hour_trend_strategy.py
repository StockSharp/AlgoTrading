import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy

class ten_pips_opposite_last_n_hour_trend_strategy(Strategy):
    def __init__(self):
        super(ten_pips_opposite_last_n_hour_trend_strategy, self).__init__()

        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("Fixed Volume", "Fixed volume for entries", "Risk")
        self._minimum_volume = self.Param("MinimumVolume", 0.1) \
            .SetDisplay("Minimum Volume", "Minimum allowed volume", "Risk")
        self._maximum_volume = self.Param("MaximumVolume", 5.0) \
            .SetDisplay("Maximum Volume", "Maximum allowed volume", "Risk")
        self._maximum_risk = self.Param("MaximumRisk", 0.05) \
            .SetDisplay("Maximum Risk", "Risk fraction when Fixed Volume is zero", "Risk")
        self._trading_hour = self.Param("TradingHour", 7) \
            .SetDisplay("Trading Hour", "Hour when entries are allowed", "Trading")
        self._hours_to_check_trend = self.Param("HoursToCheckTrend", 30) \
            .SetDisplay("Hours To Check Trend", "Look-back hours for trend detection", "Trading")
        self._stop_loss_pips = self.Param("StopLossPips", 50.0) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 10.0) \
            .SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing-stop distance in pips", "Risk")
        self._first_multiplier = self.Param("FirstMultiplier", 4.0) \
            .SetDisplay("First Multiplier", "Multiplier after the last loss", "Money Management")
        self._second_multiplier = self.Param("SecondMultiplier", 2.0) \
            .SetDisplay("Second Multiplier", "Multiplier if only the previous trade lost", "Money Management")
        self._third_multiplier = self.Param("ThirdMultiplier", 5.0) \
            .SetDisplay("Third Multiplier", "Multiplier if only the third trade lost", "Money Management")
        self._fourth_multiplier = self.Param("FourthMultiplier", 5.0) \
            .SetDisplay("Fourth Multiplier", "Multiplier if only the fourth trade lost", "Money Management")
        self._fifth_multiplier = self.Param("FifthMultiplier", 1.0) \
            .SetDisplay("Fifth Multiplier", "Multiplier if only the fifth trade lost", "Money Management")
        self._order_max_age_seconds = self.Param("OrderMaxAgeSeconds", 75600) \
            .SetDisplay("Max Position Age (s)", "Maximum holding time in seconds", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "Trading")

        self._close_history = []
        self._closed_trade_profits = []
        self._pip_size = 0.0
        self._last_bar_traded = None
        self._entry_side = None
        self._entry_volume = 0.0
        self._entry_price = None
        self._entry_time = None
        self._trailing_stop_price = None

    @property
    def FixedVolume(self):
        return self._fixed_volume.Value

    @property
    def MinimumVolume(self):
        return self._minimum_volume.Value

    @property
    def MaximumVolume(self):
        return self._maximum_volume.Value

    @property
    def MaximumRisk(self):
        return self._maximum_risk.Value

    @property
    def TradingHour(self):
        return self._trading_hour.Value

    @property
    def HoursToCheckTrend(self):
        return self._hours_to_check_trend.Value

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
    def FirstMultiplier(self):
        return self._first_multiplier.Value

    @property
    def SecondMultiplier(self):
        return self._second_multiplier.Value

    @property
    def ThirdMultiplier(self):
        return self._third_multiplier.Value

    @property
    def FourthMultiplier(self):
        return self._fourth_multiplier.Value

    @property
    def FifthMultiplier(self):
        return self._fifth_multiplier.Value

    @property
    def OrderMaxAgeSeconds(self):
        return self._order_max_age_seconds.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(ten_pips_opposite_last_n_hour_trend_strategy, self).OnStarted(time)

        self._pip_size = self._calculate_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_close_history(float(candle.ClosePrice))

        if self.Position != 0 and self._update_protective_logic(candle):
            return

        if self.Position != 0 and self._close_expired_position(candle.CloseTime):
            return

        if not self._is_trading_hour(candle.CloseTime):
            self._flatten()
            return

        if not self._has_trend_sample():
            return

        if not self._can_open_on_bar(candle.OpenTime):
            return

        if self.Position != 0:
            return

        direction = self._determine_direction()
        if direction == 0:
            return

        volume = self._calculate_order_volume(float(candle.ClosePrice))
        if volume <= 0:
            return

        if direction > 0:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

        self._last_bar_traded = candle.OpenTime

    def _update_protective_logic(self, candle):
        if self._entry_side is None or self._entry_price is None or self._entry_volume <= 0:
            return False

        pip = self._ensure_pip_size()
        if pip <= 0:
            return False

        sl_dist = float(self.StopLossPips) * pip
        tp_dist = float(self.TakeProfitPips) * pip
        trail_dist = float(self.TrailingStopPips) * pip
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        entry = self._entry_price

        if self._entry_side == Sides.Buy:
            if float(self.StopLossPips) > 0 and low_price <= entry - sl_dist:
                self.SellMarket(abs(self.Position))
                return True
            if float(self.TakeProfitPips) > 0 and high_price >= entry + tp_dist:
                self.SellMarket(abs(self.Position))
                return True
            if float(self.TrailingStopPips) > 0 and trail_dist > 0:
                candidate = high_price - trail_dist
                if high_price - entry > trail_dist:
                    if self._trailing_stop_price is None or candidate > self._trailing_stop_price:
                        self._trailing_stop_price = candidate
                if self._trailing_stop_price is not None and low_price <= self._trailing_stop_price:
                    self.SellMarket(abs(self.Position))
                    return True
        elif self._entry_side == Sides.Sell:
            if float(self.StopLossPips) > 0 and high_price >= entry + sl_dist:
                self.BuyMarket(abs(self.Position))
                return True
            if float(self.TakeProfitPips) > 0 and low_price <= entry - tp_dist:
                self.BuyMarket(abs(self.Position))
                return True
            if float(self.TrailingStopPips) > 0 and trail_dist > 0:
                candidate = low_price + trail_dist
                if self._trailing_stop_price is None or candidate < self._trailing_stop_price:
                    self._trailing_stop_price = candidate
                if self._trailing_stop_price is not None and high_price >= self._trailing_stop_price:
                    self.BuyMarket(abs(self.Position))
                    return True
        return False

    def _close_expired_position(self, time):
        max_age = self.OrderMaxAgeSeconds
        if max_age <= 0 or self._entry_time is None:
            return False
        age = time - self._entry_time
        if age.TotalSeconds < max_age:
            return False
        if self.Position > 0:
            self.SellMarket(abs(self.Position))
            return True
        if self.Position < 0:
            self.BuyMarket(abs(self.Position))
            return True
        return False

    def _is_trading_hour(self, time):
        hour = time.Hour
        return hour == self.TradingHour

    def _can_open_on_bar(self, bar_open_time):
        if self._last_bar_traded is not None and self._last_bar_traded == bar_open_time:
            return False
        return True

    def _flatten(self):
        if self.Position > 0:
            self.SellMarket(abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(abs(self.Position))

    def _has_trend_sample(self):
        return self.HoursToCheckTrend > 0 and len(self._close_history) >= self.HoursToCheckTrend

    def _determine_direction(self):
        if len(self._close_history) == 0:
            return 0
        recent_close = self._close_history[-1]
        older_index = len(self._close_history) - self.HoursToCheckTrend
        if older_index < 0 or older_index >= len(self._close_history):
            return 0
        older_close = self._close_history[older_index]
        return 1 if older_close > recent_close else -1

    def _calculate_order_volume(self, price):
        fv = float(self.FixedVolume)
        if fv > 0:
            base_volume = fv
        else:
            equity = 0.0
            if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
                equity = float(self.Portfolio.CurrentValue)
            max_risk = float(self.MaximumRisk)
            if equity > 0 and max_risk > 0:
                base_volume = round(equity * max_risk / 1000.0, 1)
            else:
                base_volume = float(self.Volume) if self.Volume > 0 else 1.0

        base_volume = self._apply_loss_multipliers(base_volume)

        min_vol = float(self.MinimumVolume)
        max_vol = float(self.MaximumVolume)
        if base_volume < min_vol:
            base_volume = min_vol
        elif base_volume > max_vol:
            base_volume = max_vol

        return base_volume

    def _apply_loss_multipliers(self, volume):
        if len(self._closed_trade_profits) == 0:
            return volume
        multipliers = [
            float(self.FirstMultiplier),
            float(self.SecondMultiplier),
            float(self.ThirdMultiplier),
            float(self.FourthMultiplier),
            float(self.FifthMultiplier),
        ]
        count = len(self._closed_trade_profits)
        for i in range(min(len(multipliers), count)):
            profit = self._closed_trade_profits[count - 1 - i]
            if profit < 0:
                volume *= multipliers[i]
                break
            if profit > 0:
                break
        return volume

    def _update_close_history(self, close):
        if close <= 0:
            return
        self._close_history.append(close)
        max_len = max(self.HoursToCheckTrend + 2, 64)
        while len(self._close_history) > max_len:
            self._close_history.pop(0)

    def _add_closed_trade_profit(self, profit):
        self._closed_trade_profits.append(profit)
        while len(self._closed_trade_profits) > 5:
            self._closed_trade_profits.pop(0)

    def _calculate_pip_size(self):
        if self.Security is None:
            return 0.0001
        step = float(self.Security.PriceStep) if self.Security.PriceStep is not None else 0.0
        if step <= 0:
            step = 0.0001
        return step

    def _ensure_pip_size(self):
        if self._pip_size <= 0:
            self._pip_size = self._calculate_pip_size()
        return self._pip_size

    def _reset_entry_state(self):
        self._entry_side = None
        self._entry_volume = 0.0
        self._entry_price = None
        self._entry_time = None
        self._trailing_stop_price = None

    def OnOwnTradeReceived(self, trade):
        super(ten_pips_opposite_last_n_hour_trend_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Order is None or trade.Trade is None:
            return

        price = float(trade.Trade.Price)
        volume = float(trade.Trade.Volume)
        time = trade.Trade.ServerTime

        if volume <= 0 or price <= 0:
            return

        if self._entry_side is None or self._entry_side == trade.Order.Side:
            total_volume = self._entry_volume + volume
            if total_volume <= 0:
                self._reset_entry_state()
                return
            if self._entry_volume > 0 and self._entry_price is not None:
                self._entry_price = (self._entry_price * self._entry_volume + price * volume) / total_volume
            else:
                self._entry_price = price
            self._entry_volume = total_volume
            self._entry_side = trade.Order.Side
            if self._entry_time is None:
                self._entry_time = time
        else:
            if self._entry_side is None or self._entry_price is None or self._entry_volume <= 0:
                return
            remaining = self._entry_volume - volume
            if remaining < 0:
                remaining = 0
            profit = 0.0
            if self._entry_side == Sides.Buy:
                profit = (price - self._entry_price) * volume
            elif self._entry_side == Sides.Sell:
                profit = (self._entry_price - price) * volume
            self._add_closed_trade_profit(profit)
            if remaining == 0:
                self._reset_entry_state()
            else:
                self._entry_volume = remaining
                self._entry_time = time

    def OnReseted(self):
        super(ten_pips_opposite_last_n_hour_trend_strategy, self).OnReseted()
        self._close_history = []
        self._closed_trade_profits = []
        self._last_bar_traded = None
        self._entry_side = None
        self._entry_volume = 0.0
        self._entry_price = None
        self._entry_time = None
        self._trailing_stop_price = None
        self._pip_size = 0.0

    def CreateClone(self):
        return ten_pips_opposite_last_n_hour_trend_strategy()
