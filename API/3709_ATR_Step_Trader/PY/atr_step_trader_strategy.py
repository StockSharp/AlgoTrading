import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class atr_step_trader_strategy(Strategy):

    def __init__(self):
        super(atr_step_trader_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 70) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA Period", "Length of the fast moving average", "Trend Filter")
        self._slow_period = self.Param("SlowPeriod", 180) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA Period", "Length of the slow moving average", "Trend Filter")
        self._atr_period = self.Param("AtrPeriod", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR window used for distance calculations", "Volatility")
        self._momentum_period = self.Param("MomentumPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Bars", "Number of consecutive bars required for trend confirmation", "Trend Filter")
        self._pyramid_limit = self.Param("PyramidLimit", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Pyramid Limit", "Maximum number of entries per direction", "Position Sizing")
        self._step_multiplier = self.Param("StepMultiplier", 4.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Step Multiplier", "ATR multiple for breakout validation", "Entry Logic")
        self._steps_multiplier = self.Param("StepsMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Steps Multiplier", "ATR multiple for add-on spacing", "Entry Logic")
        self._stop_multiplier = self.Param("StopMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Multiplier", "Extra multiplier applied on top of the step distance", "Risk Management")
        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Base order size for market entries", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame used for processing", "General")

        self._bullish_streak = 0
        self._bearish_streak = 0
        self._previous_slow = None
        self._long_entry_high = None
        self._long_entry_low = None
        self._short_entry_high = None
        self._short_entry_low = None
        self._long_stop_price = None
        self._short_stop_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    @property
    def PyramidLimit(self):
        return self._pyramid_limit.Value

    @property
    def StepMultiplier(self):
        return self._step_multiplier.Value

    @property
    def StepsMultiplier(self):
        return self._steps_multiplier.Value

    @property
    def StopMultiplier(self):
        return self._stop_multiplier.Value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    def OnReseted(self):
        super(atr_step_trader_strategy, self).OnReseted()
        self._bullish_streak = 0
        self._bearish_streak = 0
        self._previous_slow = None
        self._long_entry_high = None
        self._long_entry_low = None
        self._short_entry_high = None
        self._short_entry_low = None
        self._long_stop_price = None
        self._short_stop_price = None

    def OnStarted2(self, time):
        super(atr_step_trader_strategy, self).OnStarted2(time)

        self.Volume = self.TradeVolume

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.FastPeriod
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self.SlowPeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        highest = Highest()
        highest.Length = self.MomentumPeriod
        lowest = Lowest()
        lowest.Length = self.MomentumPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, atr, highest, lowest, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value, atr_value, highest_val, lowest_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_value)
        sv = float(slow_value)
        av = float(atr_value)

        if av <= 0:
            return

        self._update_momentum(fv, sv)

        price = float(candle.ClosePrice)
        previous_slow = self._previous_slow
        self._previous_slow = sv

        volume = float(self.Volume) if self.Volume > 0 else 1.0

        net_pos = float(self.Position)
        long_count = int(round(net_pos / volume)) if net_pos > 0 else 0
        short_count = int(round(-net_pos / volume)) if net_pos < 0 else 0

        if long_count == 0 and short_count == 0:
            if previous_slow is not None and sv > 0:
                bullish_ready = self._bullish_streak >= self.MomentumPeriod and price > previous_slow
                if bullish_ready:
                    self.BuyMarket(self.Volume)
                    long_count = 1
                    self._long_entry_high = price
                    self._long_entry_low = price
                    self._long_stop_price = price - float(self.StepMultiplier) * float(self.StopMultiplier) * av

            if long_count == 0 and previous_slow is not None and sv > 0:
                bearish_ready = self._bearish_streak >= self.MomentumPeriod and price < previous_slow
                if bearish_ready:
                    self.SellMarket(self.Volume)
                    short_count = 1
                    self._short_entry_high = price
                    self._short_entry_low = price
                    self._short_stop_price = price + float(self.StepMultiplier) * float(self.StopMultiplier) * av

        elif long_count > 0 and short_count == 0:
            self._manage_long(long_count, price, av, volume)
        elif short_count > 0 and long_count == 0:
            self._manage_short(short_count, price, av, volume)

    def _update_momentum(self, fast_value, slow_value):
        if fast_value > slow_value:
            self._bullish_streak += 1
            self._bearish_streak = 0
        elif fast_value < slow_value:
            self._bearish_streak += 1
            self._bullish_streak = 0
        else:
            self._bullish_streak += 1
            self._bearish_streak += 1

    def _manage_long(self, long_count, price, atr_value, volume):
        if self._long_entry_high is None or self._long_entry_low is None:
            return

        high = self._long_entry_high
        low = self._long_entry_low
        steps_dist = float(self.StepsMultiplier) * atr_value
        step_dist = float(self.StepMultiplier) * atr_value

        if self._long_stop_price is not None and price <= self._long_stop_price:
            self.SellMarket(self.Position)
            self._reset_long_state()
            return

        if long_count < self.PyramidLimit:
            if price >= high + steps_dist or price <= low - steps_dist:
                self.BuyMarket(self.Volume)
                long_count += 1
                self._long_entry_high = max(high, price)
                self._long_entry_low = min(low, price)
                stop = price - float(self.StepMultiplier) * float(self.StopMultiplier) * atr_value
                if self._long_stop_price is None or stop > self._long_stop_price:
                    self._long_stop_price = stop
                return

        if price <= low - steps_dist:
            self.SellMarket(self.Position)
            self._reset_long_state()
            return

        if long_count >= self.PyramidLimit:
            tightened = price - step_dist
            if self._long_stop_price is None or tightened > self._long_stop_price:
                self._long_stop_price = tightened

    def _manage_short(self, short_count, price, atr_value, volume):
        if self._short_entry_high is None or self._short_entry_low is None:
            return

        high = self._short_entry_high
        low = self._short_entry_low
        steps_dist = float(self.StepsMultiplier) * atr_value
        step_dist = float(self.StepMultiplier) * atr_value

        if self._short_stop_price is not None and price >= self._short_stop_price:
            self.BuyMarket(abs(self.Position))
            self._reset_short_state()
            return

        if short_count < self.PyramidLimit:
            if price <= low - steps_dist or price >= high + steps_dist:
                self.SellMarket(self.Volume)
                short_count += 1
                self._short_entry_high = max(high, price)
                self._short_entry_low = min(low, price)
                stop = price + float(self.StepMultiplier) * float(self.StopMultiplier) * atr_value
                if self._short_stop_price is None or stop < self._short_stop_price:
                    self._short_stop_price = stop
                return

        if price >= high + steps_dist:
            self.BuyMarket(abs(self.Position))
            self._reset_short_state()
            return

        if short_count >= self.PyramidLimit:
            tightened = price + step_dist
            if self._short_stop_price is None or tightened < self._short_stop_price:
                self._short_stop_price = tightened

    def _reset_long_state(self):
        self._long_entry_high = None
        self._long_entry_low = None
        self._long_stop_price = None
        self._bullish_streak = 0

    def _reset_short_state(self):
        self._short_entry_high = None
        self._short_entry_low = None
        self._short_stop_price = None
        self._bearish_streak = 0

    def CreateClone(self):
        return atr_step_trader_strategy()
