import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class twenty_pips_opposite_last_n_hour_trend_strategy(Strategy):
    """20 Pips Opposite Last N Hour Trend: counter-trend martingale strategy."""

    def __init__(self):
        super(twenty_pips_opposite_last_n_hour_trend_strategy, self).__init__()

        self._max_positions = self.Param("MaxPositions", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Positions", "Maximum trades per day", "Trading")
        self._max_volume = self.Param("MaxVolume", 5.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Volume", "Maximum allowed volume", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 20.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Trading")
        self._trading_hour = self.Param("TradingHour", 8) \
            .SetDisplay("Trading Hour", "Hour (0-23) when entries are allowed", "Timing")
        self._hours_to_check_trend = self.Param("HoursToCheckTrend", 6) \
            .SetDisplay("Hours To Check", "Lookback hours for trend calculation", "Signals")
        self._first_multiplier = self.Param("FirstMultiplier", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("First Multiplier", "Multiplier after first loss", "Money Management")
        self._second_multiplier = self.Param("SecondMultiplier", 4) \
            .SetGreaterThanZero() \
            .SetDisplay("Second Multiplier", "Multiplier after second loss", "Money Management")
        self._third_multiplier = self.Param("ThirdMultiplier", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Third Multiplier", "Multiplier after third loss", "Money Management")
        self._fourth_multiplier = self.Param("FourthMultiplier", 16) \
            .SetGreaterThanZero() \
            .SetDisplay("Fourth Multiplier", "Multiplier after fourth loss", "Money Management")
        self._fifth_multiplier = self.Param("FifthMultiplier", 32) \
            .SetGreaterThanZero() \
            .SetDisplay("Fifth Multiplier", "Multiplier after fifth loss", "Money Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe to process", "Market Data")

        self._close_history = []
        self._entry_price = None
        self._take_profit_level = None
        self._entry_volume = 0.0
        self._position_direction = 0
        self._consecutive_losses = 0
        self._current_day = None
        self._trades_today = 0

    @property
    def MaxPositions(self):
        return int(self._max_positions.Value)
    @property
    def MaxVolume(self):
        return float(self._max_volume.Value)
    @property
    def TakeProfitPips(self):
        return float(self._take_profit_pips.Value)
    @property
    def TradingHour(self):
        return int(self._trading_hour.Value)
    @property
    def HoursToCheckTrend(self):
        return int(self._hours_to_check_trend.Value)
    @property
    def FirstMultiplier(self):
        return int(self._first_multiplier.Value)
    @property
    def SecondMultiplier(self):
        return int(self._second_multiplier.Value)
    @property
    def ThirdMultiplier(self):
        return int(self._third_multiplier.Value)
    @property
    def FourthMultiplier(self):
        return int(self._fourth_multiplier.Value)
    @property
    def FifthMultiplier(self):
        return int(self._fifth_multiplier.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _get_pip_size(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None:
            return 0.0001
        step = float(sec.PriceStep)
        if step <= 0:
            return 0.0001
        decimals = 0
        if sec.Decimals is not None:
            decimals = int(sec.Decimals)
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def OnStarted2(self, time):
        super(twenty_pips_opposite_last_n_hour_trend_strategy, self).OnStarted2(time)

        self._close_history = []
        self._entry_price = None
        self._take_profit_level = None
        self._entry_volume = 0.0
        self._position_direction = 0
        self._consecutive_losses = 0
        self._current_day = None
        self._trades_today = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        candle_day = candle.OpenTime.Date
        if self._current_day is None or self._current_day != candle_day:
            self._current_day = candle_day
            self._trades_today = 0

        if self._position_direction != 0:
            if self._take_profit_level is not None:
                if self._position_direction > 0:
                    hit_target = float(candle.HighPrice) >= self._take_profit_level
                else:
                    hit_target = float(candle.LowPrice) <= self._take_profit_level

                if hit_target:
                    self._close_position(self._take_profit_level)

            if self._position_direction != 0 and candle.OpenTime.Hour != self.TradingHour:
                self._close_position(close)

        if self._position_direction != 0:
            self._update_history(close)
            return

        if candle.OpenTime.Hour != self.TradingHour:
            self._update_history(close)
            return

        if self.MaxPositions <= 0 or self._trades_today >= self.MaxPositions:
            self._update_history(close)
            return

        required_history = max(self.HoursToCheckTrend, 2)
        if len(self._close_history) < required_history:
            self._update_history(close)
            return

        reference_close = self._close_history[len(self._close_history) - self.HoursToCheckTrend]
        previous_close = self._close_history[len(self._close_history) - 1]

        if previous_close == reference_close:
            self._update_history(close)
            return

        go_long = previous_close < reference_close
        order_volume = self._calculate_order_volume()
        if order_volume <= 0:
            self._update_history(close)
            return

        if go_long:
            self.BuyMarket()
            self._position_direction = 1
        else:
            self.SellMarket()
            self._position_direction = -1

        self._entry_price = close
        self._entry_volume = order_volume

        distance = self._get_take_profit_distance()
        if distance > 0:
            if self._position_direction > 0:
                self._take_profit_level = self._entry_price + distance
            else:
                self._take_profit_level = self._entry_price - distance
        else:
            self._take_profit_level = None

        self._trades_today += 1
        self._update_history(close)

    def _close_position(self, exit_price):
        direction = self._position_direction
        entry_price = self._entry_price
        volume = abs(self.Position)

        if volume <= 0 and self._entry_volume > 0:
            volume = self._entry_volume

        if volume <= 0:
            self._position_direction = 0
            self._take_profit_level = None
            self._entry_price = None
            self._entry_volume = 0.0
            return

        if direction > 0:
            self.SellMarket()
        elif direction < 0:
            self.BuyMarket()

        if entry_price is not None:
            if direction > 0:
                is_loss = exit_price < entry_price
            else:
                is_loss = exit_price > entry_price

            if is_loss:
                self._consecutive_losses = min(self._consecutive_losses + 1, 5)
            else:
                self._consecutive_losses = 0

        self._position_direction = 0
        self._take_profit_level = None
        self._entry_price = None
        self._entry_volume = 0.0

    def _update_history(self, close_price):
        self._close_history.append(close_price)
        max_history = max(self.HoursToCheckTrend, 2)
        if len(self._close_history) > max_history:
            self._close_history = self._close_history[len(self._close_history) - max_history:]

    def _calculate_order_volume(self):
        base_vol = self.Volume
        if base_vol <= 0:
            return 0.0

        losses = self._consecutive_losses
        if losses >= 5:
            multiplier = float(self.FifthMultiplier)
        elif losses == 4:
            multiplier = float(self.FourthMultiplier)
        elif losses == 3:
            multiplier = float(self.ThirdMultiplier)
        elif losses == 2:
            multiplier = float(self.SecondMultiplier)
        elif losses == 1:
            multiplier = float(self.FirstMultiplier)
        else:
            multiplier = 1.0

        desired_volume = float(base_vol) * multiplier
        if self.MaxVolume > 0 and desired_volume > self.MaxVolume:
            desired_volume = self.MaxVolume
        return desired_volume

    def _get_take_profit_distance(self):
        pip_size = self._get_pip_size()
        if pip_size > 0:
            return self.TakeProfitPips * pip_size
        return 0.0

    def OnReseted(self):
        super(twenty_pips_opposite_last_n_hour_trend_strategy, self).OnReseted()
        self._close_history = []
        self._entry_price = None
        self._take_profit_level = None
        self._entry_volume = 0.0
        self._position_direction = 0
        self._consecutive_losses = 0
        self._current_day = None
        self._trades_today = 0

    def CreateClone(self):
        return twenty_pips_opposite_last_n_hour_trend_strategy()
