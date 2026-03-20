import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage

class twenty_pips_once_a_day_strategy(Strategy):
    def __init__(self):
        super(twenty_pips_once_a_day_strategy, self).__init__()

        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("Fixed Volume", "Fixed trading volume (set to 0 to use risk based sizing)", "Risk")
        self._min_volume = self.Param("MinVolume", 0.1) \
            .SetDisplay("Min Volume", "Lower volume bound applied after sizing", "Risk")
        self._max_volume = self.Param("MaxVolume", 5.0) \
            .SetDisplay("Max Volume", "Upper volume bound applied after sizing", "Risk")
        self._risk_percent = self.Param("RiskPercent", 5.0) \
            .SetDisplay("Risk Percent", "Percentage of portfolio value converted into volume", "Risk")
        self._max_orders = self.Param("MaxOrders", 1) \
            .SetDisplay("Max Orders", "Maximum number of simultaneously open positions", "Trading")
        self._trading_hour = self.Param("TradingHour", 7) \
            .SetDisplay("Trading Hour", "Hour of day when the strategy evaluates signals", "Schedule")
        self._trading_day_hours = self.Param("TradingDayHours", "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23") \
            .SetDisplay("Trading Day Hours", "Comma separated list of allowed session hours", "Schedule")
        self._hours_to_check_trend = self.Param("HoursToCheckTrend", 30) \
            .SetDisplay("Hours To Check", "Number of historical hourly closes used for the contrarian check", "Signals")
        self._order_max_age_seconds = self.Param("OrderMaxAgeSeconds", 75600) \
            .SetDisplay("Max Position Age (s)", "Maximum holding time in seconds before forcing an exit", "Risk")
        self._first_multiplier = self.Param("FirstMultiplier", 4) \
            .SetDisplay("First Multiplier", "Multiplier applied after the most recent loss", "Money Management")
        self._second_multiplier = self.Param("SecondMultiplier", 2) \
            .SetDisplay("Second Multiplier", "Multiplier applied when the last win was preceded by a loss", "Money Management")
        self._third_multiplier = self.Param("ThirdMultiplier", 5) \
            .SetDisplay("Third Multiplier", "Multiplier applied when the third latest trade was a loss", "Money Management")
        self._fourth_multiplier = self.Param("FourthMultiplier", 5) \
            .SetDisplay("Fourth Multiplier", "Multiplier applied when the fourth latest trade was a loss", "Money Management")
        self._fifth_multiplier = self.Param("FifthMultiplier", 1) \
            .SetDisplay("Fifth Multiplier", "Multiplier applied when the fifth latest trade was a loss", "Money Management")
        self._stop_loss_pips = self.Param("StopLossPips", 50.0) \
            .SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance expressed in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 10.0) \
            .SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe used for signal calculations", "Market Data")

        self._close_history = []
        self._recent_losses = []
        self._allowed_hours = set()
        self._sma = None
        self._last_trade_bar_time = None
        self._entry_time = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._entry_volume = 0.0
        self._position_direction = 0
        self._pip_size = 0.0

    @property
    def FixedVolume(self):
        return self._fixed_volume.Value

    @property
    def MinVolume(self):
        return self._min_volume.Value

    @property
    def MaxVolume(self):
        return self._max_volume.Value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @property
    def MaxOrders(self):
        return self._max_orders.Value

    @property
    def TradingHour(self):
        return self._trading_hour.Value

    @property
    def TradingDayHours(self):
        return self._trading_day_hours.Value

    @property
    def HoursToCheckTrend(self):
        return self._hours_to_check_trend.Value

    @property
    def OrderMaxAgeSeconds(self):
        return self._order_max_age_seconds.Value

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
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calculate_pip_size(self):
        if self.Security is None:
            return 0.0001
        ps = self.Security.PriceStep
        step = float(ps) if ps is not None else 0.0001
        decimals = self.Security.Decimals if self.Security.Decimals is not None else 0
        if (decimals == 3 or decimals == 5) and step > 0:
            return step * 10.0
        return step if step > 0 else 0.0001

    def _update_trading_hours(self):
        self._allowed_hours = set()
        raw = str(self.TradingDayHours) if self.TradingDayHours is not None else ""
        if raw.strip() == "":
            for h in range(24):
                self._allowed_hours.add(h)
            return

        parts = raw.split(",")
        for part in parts:
            trimmed = part.strip()
            if len(trimmed) == 0:
                continue
            if "-" in trimmed:
                range_parts = trimmed.split("-")
                if len(range_parts) != 2:
                    continue
                try:
                    start = int(range_parts[0].strip())
                    end = int(range_parts[1].strip())
                except ValueError:
                    continue
                if start < 0 or start > 23 or end < 0 or end > 23:
                    continue
                if end < start:
                    start, end = end, start
                for h in range(start, end + 1):
                    self._allowed_hours.add(h)
            else:
                try:
                    val = int(trimmed)
                except ValueError:
                    continue
                if 0 <= val <= 23:
                    self._allowed_hours.add(val)

        if len(self._allowed_hours) == 0:
            for h in range(24):
                self._allowed_hours.add(h)

    def _is_hour_allowed(self, hour):
        if len(self._allowed_hours) == 0:
            return True
        return hour in self._allowed_hours

    def OnStarted(self, time):
        super(twenty_pips_once_a_day_strategy, self).OnStarted(time)

        self._pip_size = self._calculate_pip_size()
        self._update_trading_hours()

        self._sma = SimpleMovingAverage()
        self._sma.Length = 2

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._sma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        self._add_close_to_history(float(candle.ClosePrice))

        if self._position_direction != 0:
            self._manage_open_position(candle)
            if self._position_direction != 0:
                self._enforce_session_limits(candle)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._try_open_position(candle)

    def _add_close_to_history(self, close_price):
        hours = self.HoursToCheckTrend
        if hours <= 0:
            return
        self._close_history.insert(0, close_price)
        required = max(hours, 5)
        while len(self._close_history) > required:
            self._close_history.pop()

    def _manage_open_position(self, candle):
        if self._position_direction == 0 or self._entry_price is None:
            return

        direction = self._position_direction
        close_price = float(candle.ClosePrice)
        entry_price = self._entry_price
        pip = self._pip_size

        stop_distance = float(self.StopLossPips) * pip
        if self._stop_price is None and stop_distance > 0:
            if direction > 0:
                self._stop_price = entry_price - stop_distance
            else:
                self._stop_price = entry_price + stop_distance

        trailing_distance = float(self.TrailingStopPips) * pip
        if trailing_distance > 0:
            if direction > 0:
                profit = close_price - entry_price
                if profit > trailing_distance:
                    candidate = close_price - trailing_distance
                    if self._stop_price is None or candidate > self._stop_price:
                        self._stop_price = candidate
            else:
                profit = entry_price - close_price
                if profit > trailing_distance:
                    candidate = close_price + trailing_distance
                    if self._stop_price is None or candidate < self._stop_price:
                        self._stop_price = candidate

        if self._take_profit_price is not None:
            if direction > 0:
                hit_target = float(candle.HighPrice) >= self._take_profit_price
            else:
                hit_target = float(candle.LowPrice) <= self._take_profit_price
            if hit_target:
                self._exit_position(self._take_profit_price)
                return

        if self._stop_price is not None:
            if direction > 0:
                hit_stop = float(candle.LowPrice) <= self._stop_price
            else:
                hit_stop = float(candle.HighPrice) >= self._stop_price
            if hit_stop:
                self._exit_position(self._stop_price)
                return

        max_age = self.OrderMaxAgeSeconds
        if max_age > 0 and self._entry_time is not None:
            age = candle.CloseTime - self._entry_time
            if age.TotalSeconds >= max_age:
                self._exit_position(close_price)

    def _enforce_session_limits(self, candle):
        if self._position_direction == 0:
            return
        next_hour = candle.CloseTime.Hour
        if not self._is_hour_allowed(next_hour):
            self._exit_position(float(candle.ClosePrice))

    def _try_open_position(self, candle):
        if self.MaxOrders <= 0 or self._position_direction != 0:
            return

        next_hour = candle.CloseTime.Hour
        if next_hour != self.TradingHour or not self._is_hour_allowed(next_hour):
            return

        if self._last_trade_bar_time is not None and self._last_trade_bar_time == candle.CloseTime:
            return

        hours_check = self.HoursToCheckTrend
        if len(self._close_history) < hours_check:
            return

        last_close = self._close_history[0]
        index = hours_check - 1
        if index < 0 or index >= len(self._close_history):
            return

        reference_close = self._close_history[index]
        if last_close == reference_close:
            return

        go_long = reference_close > last_close

        volume = self._calculate_order_volume()
        if volume <= 0:
            return

        entry_price = float(candle.ClosePrice)
        pip = self._pip_size

        if go_long:
            self.BuyMarket(volume)
            self._position_direction = 1
        else:
            self.SellMarket(volume)
            self._position_direction = -1

        self._entry_price = entry_price
        self._entry_time = candle.CloseTime
        self._entry_volume = volume
        self._last_trade_bar_time = candle.CloseTime

        stop_distance = float(self.StopLossPips) * pip
        if stop_distance > 0:
            if self._position_direction > 0:
                self._stop_price = entry_price - stop_distance
            else:
                self._stop_price = entry_price + stop_distance
        else:
            self._stop_price = None

        take_distance = float(self.TakeProfitPips) * pip
        if take_distance > 0:
            if self._position_direction > 0:
                self._take_profit_price = entry_price + take_distance
            else:
                self._take_profit_price = entry_price - take_distance
        else:
            self._take_profit_price = None

    def _exit_position(self, exit_price):
        direction = self._position_direction
        if direction == 0:
            return

        volume = abs(self.Position)
        if volume <= 0:
            volume = abs(self._entry_volume)

        if volume <= 0:
            self._reset_position_state()
            return

        if direction > 0:
            self.SellMarket(volume)
        else:
            self.BuyMarket(volume)

        if self._entry_price is not None:
            if direction > 0:
                is_loss = exit_price < self._entry_price
            else:
                is_loss = exit_price > self._entry_price
            self._register_trade_result(is_loss)
        else:
            self._reset_position_state()

    def _register_trade_result(self, is_loss):
        self._recent_losses.insert(0, is_loss)
        while len(self._recent_losses) > 5:
            self._recent_losses.pop()
        self._reset_position_state()

    def _reset_position_state(self):
        self._position_direction = 0
        self._entry_price = None
        self._entry_time = None
        self._entry_volume = 0.0
        self._stop_price = None
        self._take_profit_price = None

    def _calculate_order_volume(self):
        base_volume = float(self.FixedVolume)

        if base_volume <= 0:
            base_volume = self._calculate_risk_volume()

        if base_volume <= 0:
            return 0.0

        multiplier = self._get_multiplier_from_history()
        desired = self._align_volume(base_volume * multiplier)
        return desired

    def _calculate_risk_volume(self):
        risk_pct = float(self.RiskPercent)
        min_vol = float(self.MinVolume)
        if risk_pct <= 0:
            return min_vol if min_vol > 0 else 0.0

        balance = 0.0
        if self.Portfolio is not None:
            cv = self.Portfolio.CurrentValue
            if cv is not None and float(cv) > 0:
                balance = float(cv)
            elif self.Portfolio.BeginValue is not None:
                balance = float(self.Portfolio.BeginValue)

        if balance <= 0:
            return min_vol if min_vol > 0 else 0.0

        raw = balance * risk_pct / 1000.0
        return raw

    def _get_multiplier_from_history(self):
        multipliers = [
            float(self.FirstMultiplier),
            float(self.SecondMultiplier),
            float(self.ThirdMultiplier),
            float(self.FourthMultiplier),
            float(self.FifthMultiplier),
        ]
        for index in range(min(len(self._recent_losses), 5)):
            if not self._recent_losses[index]:
                continue
            if index < len(multipliers):
                return multipliers[index]
            return 1.0
        return 1.0

    def _align_volume(self, volume):
        if self.Security is not None:
            min_sec = self.Security.MinVolume
            max_sec = self.Security.MaxVolume
            step_sec = self.Security.VolumeStep

            min_val = float(min_sec) if min_sec is not None else 0.0
            max_val = float(max_sec) if max_sec is not None else 0.0
            step = float(step_sec) if step_sec is not None else 0.0

            if step > 0:
                volume = round(volume / step) * step

            if min_val > 0 and volume < min_val:
                volume = min_val

            if max_val > 0 and volume > max_val:
                volume = max_val

        min_vol = float(self.MinVolume)
        max_vol = float(self.MaxVolume)

        if min_vol > 0 and volume < min_vol:
            volume = min_vol

        if max_vol > 0 and volume > max_vol:
            volume = max_vol

        return volume

    def OnReseted(self):
        super(twenty_pips_once_a_day_strategy, self).OnReseted()
        self._close_history = []
        self._recent_losses = []
        self._allowed_hours = set()
        self._sma = None
        self._last_trade_bar_time = None
        self._entry_time = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._entry_volume = 0.0
        self._position_direction = 0
        self._pip_size = 0.0
        self._update_trading_hours()

    def CreateClone(self):
        return twenty_pips_once_a_day_strategy()
