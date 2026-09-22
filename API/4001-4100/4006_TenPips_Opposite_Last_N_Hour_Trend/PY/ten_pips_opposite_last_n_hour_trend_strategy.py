import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class _trade_episode:
    def __init__(self):
        self.reset()

    @property
    def is_open(self):
        return self.side is not None and self.entry_price is not None and self.volume > 0

    def register_entry(self, price, volume, side, time):
        if price <= 0 or volume <= 0:
            return
        if self.is_open and self.side != side:
            raise RuntimeError("An opposite fill must close the active trade episode.")

        total_volume = self.volume + volume
        if self.is_open:
            self.entry_price = (self.entry_price * self.volume + price * volume) / total_volume
        else:
            self.entry_price = price
        self.volume = total_volume
        self.side = side
        if self.entry_time is None:
            self.entry_time = time

    def register_exit(self, price, volume):
        if not self.is_open or price <= 0 or volume <= 0:
            return None

        closed_volume = min(volume, self.volume)
        if self.side == Sides.Buy:
            self.realized_profit += (price - self.entry_price) * closed_volume
        else:
            self.realized_profit += (self.entry_price - price) * closed_volume
        self.volume -= closed_volume

        if self.volume > 0:
            return None

        closed_profit = self.realized_profit
        self.reset()
        return closed_profit

    def reset(self):
        self.side = None
        self.volume = 0.0
        self.entry_price = None
        self.entry_time = None
        self.realized_profit = 0.0

    def __eq__(self, other):
        return isinstance(other, _trade_episode) \
            and self.side == other.side \
            and self.volume == other.volume \
            and self.entry_price == other.entry_price \
            and self.entry_time == other.entry_time \
            and self.realized_profit == other.realized_profit


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
        self._last_trade_date = None
        self._episode = _trade_episode()
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

    def OnStarted2(self, time):
        super(ten_pips_opposite_last_n_hour_trend_strategy, self).OnStarted2(time)

        self._pip_size = self._calculate_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
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

        if not self._can_open_on_day(candle.CloseTime):
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

        self._last_trade_date = candle.CloseTime.Date

    def _update_protective_logic(self, candle):
        if not self._episode.is_open:
            return False

        pip = self._ensure_pip_size()
        if pip <= 0:
            return False

        sl_dist = float(self.StopLossPips) * pip
        tp_dist = float(self.TakeProfitPips) * pip
        trail_dist = float(self.TrailingStopPips) * pip
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        entry = self._episode.entry_price

        if self._episode.side == Sides.Buy:
            if float(self.StopLossPips) > 0 and low_price <= entry - sl_dist:
                self.SellMarket(Math.Abs(self.Position))
                return True
            if float(self.TakeProfitPips) > 0 and high_price >= entry + tp_dist:
                self.SellMarket(Math.Abs(self.Position))
                return True
            if float(self.TrailingStopPips) > 0 and trail_dist > 0:
                # The candidate derived below becomes active on the next candle.
                if self._trailing_stop_price is not None and low_price <= self._trailing_stop_price:
                    self.SellMarket(Math.Abs(self.Position))
                    return True
                candidate = high_price - trail_dist
                if high_price - entry > trail_dist:
                    if self._trailing_stop_price is None or candidate > self._trailing_stop_price:
                        self._trailing_stop_price = candidate
        elif self._episode.side == Sides.Sell:
            if float(self.StopLossPips) > 0 and high_price >= entry + sl_dist:
                self.BuyMarket(Math.Abs(self.Position))
                return True
            if float(self.TakeProfitPips) > 0 and low_price <= entry - tp_dist:
                self.BuyMarket(Math.Abs(self.Position))
                return True
            if float(self.TrailingStopPips) > 0 and trail_dist > 0:
                # The candidate derived below becomes active on the next candle.
                if self._trailing_stop_price is not None and high_price >= self._trailing_stop_price:
                    self.BuyMarket(Math.Abs(self.Position))
                    return True
                candidate = low_price + trail_dist
                if self._trailing_stop_price is None or candidate < self._trailing_stop_price:
                    self._trailing_stop_price = candidate
        return False

    def _close_expired_position(self, time):
        max_age = self.OrderMaxAgeSeconds
        if max_age <= 0 or self._episode.entry_time is None:
            return False
        age = time - self._episode.entry_time
        if age.TotalSeconds < max_age:
            return False
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
            return True
        if self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            return True
        return False

    def _is_trading_hour(self, time):
        hour = time.Hour
        return hour == self.TradingHour

    def _can_open_on_day(self, trading_time):
        if self._last_trade_date is not None and self._last_trade_date == trading_time.Date:
            return False
        return True

    def _flatten(self):
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))

    def _has_trend_sample(self):
        return self.HoursToCheckTrend > 0 and len(self._close_history) > self.HoursToCheckTrend

    def _determine_direction(self):
        if len(self._close_history) == 0:
            return 0
        recent_close = self._close_history[-1]
        older_index = len(self._close_history) - 1 - self.HoursToCheckTrend
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
        if self.Security.Decimals in (3, 5):
            step *= 10.0
        return step

    def _ensure_pip_size(self):
        if self._pip_size <= 0:
            self._pip_size = self._calculate_pip_size()
        return self._pip_size

    def OnOwnTradeReceived(self, trade):
        super(ten_pips_opposite_last_n_hour_trend_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Order is None or trade.Trade is None:
            return

        price = float(trade.Trade.TradePrice)
        volume = float(trade.Trade.TradeVolume)
        time = trade.Trade.ServerTime

        if volume <= 0 or price <= 0:
            return

        if not self._episode.is_open or self._episode.side == trade.Order.Side:
            self._episode.register_entry(price, volume, trade.Order.Side, time)
            trailing_distance = float(self.TrailingStopPips) * self._ensure_pip_size()
            if float(self.TrailingStopPips) > 0 and trailing_distance > 0:
                self._trailing_stop_price = self._episode.entry_price - trailing_distance \
                    if self._episode.side == Sides.Buy else self._episode.entry_price + trailing_distance
        else:
            closed_profit = self._episode.register_exit(price, volume)
            if closed_profit is not None:
                self._add_closed_trade_profit(closed_profit)
                self._trailing_stop_price = None

    def OnReseted(self):
        super(ten_pips_opposite_last_n_hour_trend_strategy, self).OnReseted()
        self._close_history = []
        self._closed_trade_profits = []
        self._episode.reset()
        self._last_trade_date = None
        self._trailing_stop_price = None
        self._pip_size = 0.0

    def CreateClone(self):
        return ten_pips_opposite_last_n_hour_trend_strategy()
