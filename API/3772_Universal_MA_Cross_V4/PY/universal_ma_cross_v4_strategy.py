import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    DecimalIndicatorValue, ExponentialMovingAverage,
    SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
)
from StockSharp.Algo.Strategies import Strategy


# MA method constants
MA_SIMPLE = 0
MA_EXPONENTIAL = 1
MA_SMOOTHED = 2
MA_LINEAR_WEIGHTED = 3

# Applied price constants
PRICE_CLOSE = 0
PRICE_OPEN = 1
PRICE_HIGH = 2
PRICE_LOW = 3
PRICE_MEDIAN = 4
PRICE_TYPICAL = 5
PRICE_WEIGHTED = 6

# Trade direction constants
DIR_NONE = 0
DIR_LONG = 1
DIR_SHORT = 2


class universal_ma_cross_v4_strategy(Strategy):
    """Universal MA Cross EA v4. Trades crossover between configurable fast and slow
    moving averages with optional session filters, stop-and-reverse, and trailing stop."""

    def __init__(self):
        super(universal_ma_cross_v4_strategy, self).__init__()

        self._fast_ma_period = self.Param("FastMaPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 80) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA Period", "Length of the slow moving average", "Indicators")
        self._fast_ma_type = self.Param("FastMaType", MA_EXPONENTIAL) \
            .SetDisplay("Fast MA Method", "Smoothing method for the fast MA", "Indicators")
        self._slow_ma_type = self.Param("SlowMaType", MA_EXPONENTIAL) \
            .SetDisplay("Slow MA Method", "Smoothing method for the slow MA", "Indicators")
        self._fast_price_type = self.Param("FastPriceType", PRICE_CLOSE) \
            .SetDisplay("Fast MA Price", "Price source for the fast MA", "Indicators")
        self._slow_price_type = self.Param("SlowPriceType", PRICE_CLOSE) \
            .SetDisplay("Slow MA Price", "Price source for the slow MA", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 100.0) \
            .SetDisplay("Stop Loss (points)", "Stop-loss distance in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 200.0) \
            .SetDisplay("Take Profit (points)", "Take-profit distance in price steps", "Risk")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 40.0) \
            .SetDisplay("Trailing Stop (points)", "Trailing stop distance in price steps", "Risk")
        self._min_cross_distance_points = self.Param("MinCrossDistancePoints", 0.0) \
            .SetDisplay("Min Cross Distance (points)", "Minimum separation between MAs", "Filters")
        self._reverse_condition = self.Param("ReverseCondition", False) \
            .SetDisplay("Reverse Signals", "Swap bullish and bearish conditions", "General")
        self._confirmed_on_entry = self.Param("ConfirmedOnEntry", True) \
            .SetDisplay("Confirmed On Entry", "Validate signals on the previous closed bar", "General")
        self._one_entry_per_bar = self.Param("OneEntryPerBar", True) \
            .SetDisplay("One Entry Per Bar", "Allow at most one entry per candle", "General")
        self._stop_and_reverse = self.Param("StopAndReverse", True) \
            .SetDisplay("Stop And Reverse", "Close and reverse on opposite signal", "Risk")
        self._pure_sar = self.Param("PureSar", False) \
            .SetDisplay("Pure SAR", "Disable protective stops and trailing", "Risk")
        self._use_hour_trade = self.Param("UseHourTrade", False) \
            .SetDisplay("Use Hour Filter", "Restrict trading to a specific session", "Session")
        self._start_hour = self.Param("StartHour", 10) \
            .SetDisplay("Start Hour", "Trading window start hour", "Session")
        self._end_hour = self.Param("EndHour", 11) \
            .SetDisplay("End Hour", "Trading window end hour", "Session")
        self._volume_param = self.Param("TradeVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Order volume for each entry", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary candle subscription", "General")

        self._fast_ma = None
        self._slow_ma = None
        self._fast_prev = None
        self._fast_prev_prev = None
        self._slow_prev = None
        self._slow_prev_prev = None
        self._last_entry_bar = None
        self._last_trade = DIR_NONE
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @property
    def FastMaType(self):
        return self._fast_ma_type.Value

    @property
    def SlowMaType(self):
        return self._slow_ma_type.Value

    @property
    def FastPriceType(self):
        return self._fast_price_type.Value

    @property
    def SlowPriceType(self):
        return self._slow_price_type.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @property
    def MinCrossDistancePoints(self):
        return self._min_cross_distance_points.Value

    @property
    def ReverseCondition(self):
        return self._reverse_condition.Value

    @property
    def ConfirmedOnEntry(self):
        return self._confirmed_on_entry.Value

    @property
    def OneEntryPerBar(self):
        return self._one_entry_per_bar.Value

    @property
    def StopAndReverse(self):
        return self._stop_and_reverse.Value

    @property
    def PureSar(self):
        return self._pure_sar.Value

    @property
    def UseHourTrade(self):
        return self._use_hour_trade.Value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

    @property
    def TradeVolume(self):
        return self._volume_param.Value

    def OnReseted(self):
        super(universal_ma_cross_v4_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._fast_prev = None
        self._fast_prev_prev = None
        self._slow_prev = None
        self._slow_prev_prev = None
        self._last_entry_bar = None
        self._last_trade = DIR_NONE
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    def _create_ma(self, method, period):
        if method == MA_SIMPLE:
            ma = SimpleMovingAverage()
        elif method == MA_SMOOTHED:
            ma = SmoothedMovingAverage()
        elif method == MA_LINEAR_WEIGHTED:
            ma = WeightedMovingAverage()
        else:
            ma = ExponentialMovingAverage()
        ma.Length = period
        return ma

    def _get_price(self, candle, price_type):
        if price_type == PRICE_OPEN:
            return float(candle.OpenPrice)
        elif price_type == PRICE_HIGH:
            return float(candle.HighPrice)
        elif price_type == PRICE_LOW:
            return float(candle.LowPrice)
        elif price_type == PRICE_MEDIAN:
            return (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        elif price_type == PRICE_TYPICAL:
            return (float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0
        elif price_type == PRICE_WEIGHTED:
            return (float(candle.HighPrice) + float(candle.LowPrice) + 2.0 * float(candle.ClosePrice)) / 4.0
        return float(candle.ClosePrice)

    def _get_price_offset(self, points):
        pts = float(points)
        if pts <= 0:
            return 0.0
        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is not None and float(step) > 0:
            return pts * float(step)
        return pts

    def OnStarted(self, time):
        super(universal_ma_cross_v4_strategy, self).OnStarted(time)

        self._fast_ma = self._create_ma(self.FastMaType, self.FastMaPeriod)
        self._slow_ma = self._create_ma(self.SlowMaType, self.SlowMaPeriod)

        self.Volume = float(self.TradeVolume)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

    def _reset_protection(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    def _set_protection_levels(self, entry_price, is_long):
        self._entry_price = entry_price
        if self.PureSar:
            self._stop_price = None
            self._take_profit_price = None
            return

        stop_dist = self._get_price_offset(self.StopLossPoints)
        take_dist = self._get_price_offset(self.TakeProfitPoints)

        if stop_dist > 0:
            self._stop_price = entry_price - stop_dist if is_long else entry_price + stop_dist
        else:
            self._stop_price = None

        if take_dist > 0:
            self._take_profit_price = entry_price + take_dist if is_long else entry_price - take_dist
        else:
            self._take_profit_price = None

    def _update_trailing_stop(self, candle):
        if self.PureSar or float(self.TrailingStopPoints) <= 0 or self._entry_price is None:
            return

        trailing_distance = self._get_price_offset(self.TrailingStopPoints)
        if trailing_distance <= 0:
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            move = close - self._entry_price
            if move > trailing_distance:
                candidate = close - trailing_distance
                if self._stop_price is None or candidate > self._stop_price:
                    self._stop_price = candidate
        elif self.Position < 0:
            move = self._entry_price - close
            if move > trailing_distance:
                candidate = close + trailing_distance
                if self._stop_price is None or candidate < self._stop_price:
                    self._stop_price = candidate

    def _manage_existing_position(self, candle):
        if self.Position == 0:
            self._reset_protection()
            return

        self._update_trailing_stop(candle)

        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        if self.Position > 0:
            if self._stop_price is not None and low <= self._stop_price:
                self._close_position()
                self._reset_protection()
                return
            if self._take_profit_price is not None and high >= self._take_profit_price:
                self._close_position()
                self._reset_protection()
        elif self.Position < 0:
            if self._stop_price is not None and high >= self._stop_price:
                self._close_position()
                self._reset_protection()
                return
            if self._take_profit_price is not None and low <= self._take_profit_price:
                self._close_position()
                self._reset_protection()

    def _is_within_trading_hours(self, candle):
        if not self.UseHourTrade:
            return True
        hour = candle.OpenTime.Hour
        start = self.StartHour
        end = self.EndHour
        if start <= end:
            return hour >= start and hour <= end
        return hour >= start or hour <= end

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._manage_existing_position(candle)

        if self._fast_ma is None or self._slow_ma is None:
            return

        fast_price = self._get_price(candle, self.FastPriceType)
        slow_price = self._get_price(candle, self.SlowPriceType)
        time = candle.OpenTime

        fast_input = DecimalIndicatorValue(self._fast_ma, fast_price, time)
        fast_input.IsFinal = True
        fast_result = self._fast_ma.Process(fast_input)
        if fast_result.IsEmpty:
            return
        fast_value = float(fast_result)

        slow_input = DecimalIndicatorValue(self._slow_ma, slow_price, time)
        slow_input.IsFinal = True
        slow_result = self._slow_ma.Process(slow_input)
        if slow_result.IsEmpty:
            return
        slow_value = float(slow_result)

        prev_fast = self._fast_prev
        prev_slow = self._slow_prev
        prev_fast_prev = self._fast_prev_prev
        prev_slow_prev = self._slow_prev_prev

        self._fast_prev_prev = prev_fast
        self._slow_prev_prev = prev_slow
        self._fast_prev = fast_value
        self._slow_prev = slow_value

        min_distance = self._get_price_offset(self.MinCrossDistancePoints)

        cross_up = False
        cross_down = False

        if self.ConfirmedOnEntry:
            if prev_fast is not None and prev_slow is not None and \
               prev_fast_prev is not None and prev_slow_prev is not None:
                diff = prev_fast - prev_slow
                cross_up = prev_fast_prev < prev_slow_prev and prev_fast > prev_slow and diff >= min_distance
                cross_down = prev_fast_prev > prev_slow_prev and prev_fast < prev_slow and -diff >= min_distance
        else:
            if prev_fast is not None and prev_slow is not None:
                diff = fast_value - slow_value
                cross_up = prev_fast < prev_slow and fast_value > slow_value and diff >= min_distance
                cross_down = prev_fast > prev_slow and fast_value < slow_value and -diff >= min_distance

        if not self.ReverseCondition:
            buy_signal = cross_up
            sell_signal = cross_down
        else:
            buy_signal = cross_down
            sell_signal = cross_up

        if not self._is_within_trading_hours(candle):
            return

        if self.StopAndReverse and self.Position != 0:
            reverse_to_short = self._last_trade == DIR_LONG and sell_signal
            reverse_to_long = self._last_trade == DIR_SHORT and buy_signal
            if reverse_to_long or reverse_to_short:
                self._close_position()
                self._reset_protection()
                self._last_trade = DIR_NONE

        if self.Position != 0:
            return

        if self.OneEntryPerBar and self._last_entry_bar == candle.OpenTime:
            return

        close = float(candle.ClosePrice)

        if buy_signal:
            self.BuyMarket()
            self._set_protection_levels(close, True)
            self._last_trade = DIR_LONG
            self._last_entry_bar = candle.OpenTime
        elif sell_signal:
            self.SellMarket()
            self._set_protection_levels(close, False)
            self._last_trade = DIR_SHORT
            self._last_entry_bar = candle.OpenTime

    def CreateClone(self):
        return universal_ma_cross_v4_strategy()
