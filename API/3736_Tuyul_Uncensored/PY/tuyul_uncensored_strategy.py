import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ZigZag, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class tuyul_uncensored_strategy(Strategy):
    """ZigZag swings + EMA trend filter with Fibonacci retracement entries and virtual SL/TP."""

    def __init__(self):
        super(tuyul_uncensored_strategy, self).__init__()

        self._volume_per_trade = self.Param("VolumePerTrade", 0.03) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume", "Order volume per trade", "General")
        self._take_profit_multiplier = self.Param("TakeProfitMultiplier", 1.2) \
            .SetGreaterThanZero() \
            .SetDisplay("TP Multiplier", "Take profit distance relative to stop loss", "Risk")
        self._zigzag_depth = self.Param("ZigZagDepth", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("ZigZag Depth", "Number of bars to evaluate for swings", "ZigZag")
        self._zigzag_deviation = self.Param("ZigZagDeviation", 0.05) \
            .SetDisplay("ZigZag Deviation", "Minimum deviation in points to confirm a swing", "ZigZag")
        self._zigzag_backstep = self.Param("ZigZagBackstep", 3) \
            .SetDisplay("ZigZag Backstep", "Bars required between opposite pivots", "ZigZag")
        self._fast_ema_period = self.Param("FastEmaPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Period of the fast EMA filter", "Trend")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Period of the slow EMA filter", "Trend")
        self._fib_level = self.Param("FibLevel", 0.57) \
            .SetDisplay("Fibonacci Level", "Retracement level used to position pending orders", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles used for analysis", "General")

        self._pivots = None
        self._last_zigzag_high = 0.0
        self._last_zigzag_low = 0.0
        self._previous_fast = None
        self._previous_slow = None
        self._active_stop = None
        self._active_take = None
        self._active_direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def VolumePerTrade(self):
        return self._volume_per_trade.Value

    @property
    def TakeProfitMultiplier(self):
        return self._take_profit_multiplier.Value

    @property
    def ZigZagDeviation(self):
        return self._zigzag_deviation.Value

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @property
    def FibLevel(self):
        return self._fib_level.Value

    def OnReseted(self):
        super(tuyul_uncensored_strategy, self).OnReseted()
        self._pivots = None
        self._last_zigzag_high = 0.0
        self._last_zigzag_low = 0.0
        self._previous_fast = None
        self._previous_slow = None
        self._active_stop = None
        self._active_take = None
        self._active_direction = 0

    def OnStarted(self, time):
        super(tuyul_uncensored_strategy, self).OnStarted(time)

        self._pivots = []

        zigzag = ZigZag()
        zigzag.Deviation = float(self.ZigZagDeviation)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastEmaPeriod

        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowEmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(zigzag, fast_ema, slow_ema, self._process_candle).Start()

    def _process_candle(self, candle, zigzag_value, fast_ema_value, slow_ema_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_active_protection(candle)

        zz_v = float(zigzag_value)
        fast_v = float(fast_ema_value)
        slow_v = float(slow_ema_value)

        new_high, new_low = self._update_zigzag_state(candle, zz_v)

        if self.Position == 0 and (new_high or new_low) and \
                self._previous_fast is not None and self._previous_slow is not None:
            self._try_enter_market(self._previous_fast, self._previous_slow)

        self._previous_fast = fast_v
        self._previous_slow = slow_v

        if self.Position == 0 and self._active_direction != 0:
            self._clear_active_protection()

    def _update_active_protection(self, candle):
        if self._active_direction == 1 and self.Position > 0 and \
                self._active_stop is not None and self._active_take is not None:
            if float(candle.LowPrice) <= self._active_stop or float(candle.HighPrice) >= self._active_take:
                self.SellMarket(self.Position)
                self._clear_active_protection()
        elif self._active_direction == -1 and self.Position < 0 and \
                self._active_stop is not None and self._active_take is not None:
            if float(candle.HighPrice) >= self._active_stop or float(candle.LowPrice) <= self._active_take:
                self.BuyMarket(abs(self.Position))
                self._clear_active_protection()

    def _try_enter_market(self, previous_fast, previous_slow):
        high = self._last_zigzag_high
        low = self._last_zigzag_low

        if high <= 0 or low <= 0 or high <= low:
            return

        volume = float(self.VolumePerTrade)
        if volume <= 0:
            volume = float(self.Volume)
        if volume <= 0:
            return

        fib = float(self.FibLevel)
        tp_mult = float(self.TakeProfitMultiplier)

        if previous_fast > previous_slow:
            stop_price = low
            fib_price = low + (high - low) * fib
            sl_distance = fib_price - stop_price
            if sl_distance <= 0:
                return
            take_price = fib_price + sl_distance * tp_mult
            self.BuyMarket(volume)
            self._active_stop = stop_price
            self._active_take = take_price
            self._active_direction = 1
        elif previous_fast < previous_slow:
            stop_price = high
            fib_price = high - (high - low) * fib
            sl_distance = stop_price - fib_price
            if sl_distance <= 0:
                return
            take_price = fib_price - sl_distance * tp_mult
            self.SellMarket(volume)
            self._active_stop = stop_price
            self._active_take = take_price
            self._active_direction = -1

    def _update_zigzag_state(self, candle, zigzag_value):
        new_high = False
        new_low = False

        if zigzag_value == 0:
            return False, False

        found_index = -1
        for i in range(len(self._pivots)):
            if self._pivots[i][0] == candle.OpenTime:
                found_index = i
                break

        if found_index >= 0:
            if self._pivots[found_index][1] == zigzag_value:
                return False, False
            self._pivots[found_index] = (candle.OpenTime, zigzag_value)
        else:
            self._pivots.append((candle.OpenTime, zigzag_value))
            if len(self._pivots) > 300:
                self._pivots.pop(0)

        if len(self._pivots) < 2:
            return False, False

        previous = self._pivots[-2]
        last = self._pivots[-1]
        is_high = last[1] > previous[1]

        if is_high:
            if self._last_zigzag_high != last[1]:
                self._last_zigzag_high = last[1]
                new_high = True
            if self._last_zigzag_low != previous[1]:
                self._last_zigzag_low = previous[1]
                new_low = True
        else:
            if self._last_zigzag_low != last[1]:
                self._last_zigzag_low = last[1]
                new_low = True
            if self._last_zigzag_high != previous[1]:
                self._last_zigzag_high = previous[1]
                new_high = True

        return new_high, new_low

    def _clear_active_protection(self):
        self._active_stop = None
        self._active_take = None
        self._active_direction = 0

    def CreateClone(self):
        return tuyul_uncensored_strategy()
