import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class donchain_counter_strategy(Strategy):
    def __init__(self):
        super(donchain_counter_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 20)
        self._buffer_steps = self.Param("BufferSteps", 50)
        self._trade_cooldown = self.Param("TradeCooldown", TimeSpan.FromMinutes(30))
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._donchian = None
        self._price_step = 0.0
        self._tolerance = 0.0
        self._current_upper = 0.0
        self._current_lower = 0.0
        self._previous_upper = 0.0
        self._previous_lower = 0.0
        self._earlier_upper = 0.0
        self._earlier_lower = 0.0
        self._long_stop = None
        self._short_stop = None
        self._last_trade_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(donchain_counter_strategy, self).OnStarted(time)

        self._price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001
        if self._price_step <= 0:
            self._price_step = 0.0001
        self._tolerance = self._price_step / 2.0

        self._donchian = DonchianChannels()
        self._donchian.Length = self._channel_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        result = self._donchian.Process(candle)
        if result.IsEmpty or not self._donchian.IsFormed:
            return

        ub = result.UpperBand
        lb = result.LowerBand
        if ub is None or lb is None:
            return

        self._current_upper = float(ub)
        self._current_lower = float(lb)

        if self.Position != 0:
            self._manage_position(candle)
            self._update_history()
            return

        self._try_open(candle)
        self._update_history()

    def _manage_position(self, candle):
        buffer = self._buffer_steps.Value * self._price_step

        if self.Position > 0:
            if float(candle.HighPrice) > self._current_lower + buffer:
                if self._long_stop is None or self._long_stop < self._current_lower - self._tolerance:
                    self._long_stop = self._current_lower
            if self._long_stop is not None and float(candle.LowPrice) <= self._long_stop + self._tolerance:
                self.SellMarket(self.Position)
                self._long_stop = None
        elif self.Position < 0:
            if float(candle.LowPrice) < self._current_upper - buffer:
                if self._short_stop is None or self._short_stop > self._current_upper + self._tolerance:
                    self._short_stop = self._current_upper
            if self._short_stop is not None and float(candle.HighPrice) >= self._short_stop - self._tolerance:
                self.BuyMarket(abs(self.Position))
                self._short_stop = None

    def _try_open(self, candle):
        if self._previous_upper == 0 or self._earlier_upper == 0 or self._previous_lower == 0 or self._earlier_lower == 0:
            return

        now = candle.CloseTime
        if self._last_trade_time is not None and (now - self._last_trade_time) < self._trade_cooldown.Value:
            return

        if self._previous_upper > self._earlier_upper and not self._are_close(self._previous_upper, self._earlier_upper):
            self.BuyMarket(self.Volume)
            self._long_stop = self._current_lower
            self._last_trade_time = now
            return

        if self._previous_lower < self._earlier_lower and not self._are_close(self._previous_lower, self._earlier_lower):
            self.SellMarket(self.Volume)
            self._short_stop = self._current_upper
            self._last_trade_time = now

    def _update_history(self):
        self._earlier_upper = self._previous_upper
        self._earlier_lower = self._previous_lower
        self._previous_upper = self._current_upper
        self._previous_lower = self._current_lower

    def _are_close(self, first, second):
        return abs(first - second) <= self._tolerance

    def OnReseted(self):
        super(donchain_counter_strategy, self).OnReseted()
        self._current_upper = 0.0
        self._current_lower = 0.0
        self._previous_upper = 0.0
        self._previous_lower = 0.0
        self._earlier_upper = 0.0
        self._earlier_lower = 0.0
        self._long_stop = None
        self._short_stop = None
        self._last_trade_time = None

    def CreateClone(self):
        return donchain_counter_strategy()
