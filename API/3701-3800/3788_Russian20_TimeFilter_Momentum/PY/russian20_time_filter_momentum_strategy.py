import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class russian20_time_filter_momentum_strategy(Strategy):
    """SMA + Momentum filter strategy with optional trading hours restriction.
    Buy when close > SMA, momentum > threshold, and close > previous close.
    Sell when close < SMA, momentum < threshold, and close < previous close."""

    def __init__(self):
        super(russian20_time_filter_momentum_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")
        self._moving_average_length = self.Param("MovingAverageLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length", "Simple moving average lookback", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Period", "Momentum indicator lookback", "Indicators")
        self._momentum_threshold = self.Param("MomentumThreshold", 100.0) \
            .SetDisplay("Momentum Threshold", "Neutral momentum level for signals", "Indicators")
        self._take_profit_pips = self.Param("TakeProfitPips", 20.0) \
            .SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
        self._use_time_filter = self.Param("UseTimeFilter", False) \
            .SetDisplay("Use Time Filter", "Restrict trading to a session", "Session")
        self._start_hour = self.Param("StartHour", 14) \
            .SetDisplay("Start Hour", "Inclusive start hour of the trading session", "Session")
        self._end_hour = self.Param("EndHour", 16) \
            .SetDisplay("End Hour", "Inclusive end hour of the trading session", "Session")

        self._previous_close = None
        self._entry_price = None
        self._pip_size = 0.0
        self._take_profit_offset = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MovingAverageLength(self):
        return self._moving_average_length.Value

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    @property
    def MomentumThreshold(self):
        return self._momentum_threshold.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def UseTimeFilter(self):
        return self._use_time_filter.Value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

    def OnReseted(self):
        super(russian20_time_filter_momentum_strategy, self).OnReseted()
        self._previous_close = None
        self._entry_price = None
        self._pip_size = 0.0
        self._take_profit_offset = 0.0

    def _update_pip_settings(self):
        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is None or float(step) <= 0:
            self._pip_size = 1.0
        else:
            step_val = float(step)
            # Detect 3/5-digit broker
            digits = self._get_decimal_places(step_val)
            multiplier = 10.0 if (digits == 3 or digits == 5) else 1.0
            self._pip_size = step_val * multiplier

        tp = float(self.TakeProfitPips)
        self._take_profit_offset = tp * self._pip_size if tp > 0 else 0.0

    def _get_decimal_places(self, value):
        digits = 0
        v = abs(value)
        while v != int(v) and digits < 10:
            v *= 10.0
            digits += 1
        return digits

    def OnStarted2(self, time):
        super(russian20_time_filter_momentum_strategy, self).OnStarted2(time)

        self._update_pip_settings()

        ma = SimpleMovingAverage()
        ma.Length = self.MovingAverageLength

        mom = Momentum()
        mom.Length = self.MomentumPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, mom, self._process_candle).Start()

    def _process_candle(self, candle, ma_value, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        ma_val = float(ma_value)
        mom_val = float(momentum_value)
        close = float(candle.ClosePrice)

        # Time filter
        if self.UseTimeFilter:
            hour = candle.OpenTime.Hour
            if hour < self.StartHour or hour > self.EndHour:
                self._previous_close = close
                return

        if self._pip_size == 0.0:
            self._update_pip_settings()

        if self._previous_close is None:
            self._previous_close = close
            return

        prev_close = self._previous_close
        threshold = float(self.MomentumThreshold)

        if self.Position == 0 and self._entry_price is not None:
            self._entry_price = None

        if self.Position == 0:
            # Entry conditions
            bullish = close > ma_val and mom_val > threshold and close > prev_close
            bearish = close < ma_val and mom_val < threshold and close < prev_close

            if bullish:
                self.BuyMarket()
                self._entry_price = close
            elif bearish:
                self.SellMarket()
                self._entry_price = close
        elif self.Position > 0:
            # Exit long: momentum weakens or TP hit
            exit_momentum = mom_val <= threshold
            exit_tp = (self._entry_price is not None and self._take_profit_offset > 0
                       and close >= self._entry_price + self._take_profit_offset)
            if exit_momentum or exit_tp:
                self.SellMarket()
                self._entry_price = None
        else:
            # Exit short: momentum strengthens or TP hit
            exit_momentum = mom_val >= threshold
            exit_tp = (self._entry_price is not None and self._take_profit_offset > 0
                       and close <= self._entry_price - self._take_profit_offset)
            if exit_momentum or exit_tp:
                self.BuyMarket()
                self._entry_price = None

        self._previous_close = close

    def CreateClone(self):
        return russian20_time_filter_momentum_strategy()
