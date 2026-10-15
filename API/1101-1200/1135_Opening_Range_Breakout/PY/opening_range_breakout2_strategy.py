import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class opening_range_breakout2_strategy(Strategy):
    """
    Opening range breakout strategy.
    The high and low of the candles between RangeStart and RangeEnd form the opening range. After it closes, and only if its width
    exceeds MinRangePercent of the close, a break above the high goes long and a break below the low goes short. The stop sits
    Retrace times the range from the breakout level and the target RewardRisk times that distance. Optionally only one trade per
    day is taken and a stopped-out trade reverses. Everything is flat at DayEnd.
    """

    def __init__(self):
        super(opening_range_breakout2_strategy, self).__init__()
        self._range_start = self.Param("RangeStart", TimeSpan(9, 30, 0)).SetDisplay("Range Start", "Opening range start time (UTC)", "Session")
        self._range_end = self.Param("RangeEnd", TimeSpan(10, 15, 0)).SetDisplay("Range End", "Opening range end time (UTC)", "Session")
        self._day_end = self.Param("DayEnd", TimeSpan(15, 45, 0)).SetDisplay("Day End", "Time when all positions are closed (UTC)", "Session")
        self._min_range_percent = self.Param("MinRangePercent", 0.35).SetNotNegative().SetDisplay("Min Range %", "Minimum range width as percent of close", "Trading")
        self._reward_risk = self.Param("RewardRisk", 1.1).SetGreaterThanZero().SetDisplay("Reward/Risk", "Target distance in multiples of the stop distance", "Risk")
        self._retrace = self.Param("Retrace", 0.5).SetGreaterThanZero().SetDisplay("Retrace", "Stop distance as a fraction of the range width", "Risk")
        self._one_trade_per_day = self.Param("OneTradePerDay", True).SetDisplay("One Trade Per Day", "Take at most one breakout per day", "Trading")
        self._reverse_on_loss = self.Param("ReverseOnLoss", False).SetDisplay("Reverse On Loss", "Reverse the position when the stop is hit", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._current_day = None
        self._reset_day()

    def _reset_day(self):
        self._range_high = None
        self._range_low = None
        self._range_valid = False
        self._traded_today = False
        self._reversed_today = False
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

    def OnReseted(self):
        super(opening_range_breakout2_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(opening_range_breakout2_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        open_time = candle.OpenTime.ToUniversalTime()
        day = open_time.Date
        tod = open_time.TimeOfDay.Ticks
        range_start = self._range_start.Value.Ticks
        range_end = self._range_end.Value.Ticks
        day_end = self._day_end.Value.Ticks

        if self._current_day is None or day != self._current_day:
            self._current_day = day
            self._reset_day()

        if tod >= range_start and tod < range_end:
            self._range_high = candle.HighPrice if self._range_high is None else max(self._range_high, candle.HighPrice)
            self._range_low = candle.LowPrice if self._range_low is None else min(self._range_low, candle.LowPrice)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if tod >= day_end:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
            return

        if tod < range_end or self._range_high is None or self._range_low is None:
            return

        high = self._range_high
        low = self._range_low
        rng = high - low

        if not self._range_valid:
            # The width filter is checked once, on the first candle after the window closes.
            if rng <= 0 or rng < candle.ClosePrice * Decimal(self._min_range_percent.Value) / Decimal(100):
                self._range_high = None
                self._range_low = None
                return
            self._range_valid = True

        stop_distance = rng * Decimal(self._retrace.Value)
        reverse_on_loss = self._reverse_on_loss.Value

        if self.Position > 0:
            if candle.LowPrice <= self._stop_price:
                if reverse_on_loss and not self._reversed_today:
                    self.SellMarket(self.Position + self.Volume)
                    self._set_levels(self._stop_price, stop_distance, False)
                    self._reversed_today = True
                else:
                    self.SellMarket(self.Position)
            elif candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if candle.HighPrice >= self._stop_price:
                if reverse_on_loss and not self._reversed_today:
                    self.BuyMarket(-self.Position + self.Volume)
                    self._set_levels(self._stop_price, stop_distance, True)
                    self._reversed_today = True
                else:
                    self.BuyMarket(-self.Position)
            elif candle.LowPrice <= self._take_price:
                self.BuyMarket(-self.Position)
            return

        if self._one_trade_per_day.Value and self._traded_today:
            return

        break_up = candle.HighPrice > high
        break_down = candle.LowPrice < low

        # A candle through both boundaries gives no clear direction.
        if break_up and not break_down:
            self.BuyMarket(self.Volume)
            self._set_levels(high, stop_distance, True)
            self._traded_today = True
        elif break_down and not break_up:
            self.SellMarket(self.Volume)
            self._set_levels(low, stop_distance, False)
            self._traded_today = True

    def _set_levels(self, entry, stop_distance, is_long):
        take_distance = stop_distance * Decimal(self._reward_risk.Value)
        self._stop_price = entry - stop_distance if is_long else entry + stop_distance
        self._take_price = entry + take_distance if is_long else entry - take_distance

    def CreateClone(self):
        return opening_range_breakout2_strategy()
