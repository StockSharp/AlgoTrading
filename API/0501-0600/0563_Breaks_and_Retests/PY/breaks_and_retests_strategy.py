import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from collections import deque
from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class breaks_and_retests_strategy(Strategy):
    """
    Breaks and retests strategy.
    Resistance and support are the highest and lowest closes of the previous LookbackPeriod candles. A close above resistance goes long
    and a close below support goes short, reversing an opposite position. When not already in the breakout direction, a retest of the
    broken level between RetestBarsSinceBreakout and RetestBarsSinceBreakout + RetestDetectionLimit bars after the breakout (the
    candle touches the level and closes back on the breakout side) also enters. A StopLossPercent stop protects the trade until the
    close is ProfitThresholdPercent in profit, after which a trailing stop TrailingStopGapPercent from the best close takes over.
    """

    def __init__(self):
        super(breaks_and_retests_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20).SetGreaterThanZero().SetDisplay("Lookback Period", "Previous candles whose closes define support and resistance", "Levels")
        self._retest_bars_since_breakout = self.Param("RetestBarsSinceBreakout", 2).SetNotNegative().SetDisplay("Retest Bars Since Breakout", "Bars after a breakout before a retest can be detected", "Retest")
        self._retest_detection_limit = self.Param("RetestDetectionLimit", 2).SetNotNegative().SetDisplay("Retest Detection Limit", "Additional bars during which a retest is still detected", "Retest")
        self._profit_threshold_percent = self.Param("ProfitThresholdPercent", 5.0).SetGreaterThanZero().SetDisplay("Profit Threshold %", "Profit that switches to the trailing stop", "Risk")
        self._trailing_stop_gap_percent = self.Param("TrailingStopGapPercent", 1.0).SetGreaterThanZero().SetDisplay("Trailing Gap %", "Trailing stop distance from the best close", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetGreaterThanZero().SetDisplay("Stop Loss %", "Initial stop loss from the entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._closes = deque()
        self._broken_resistance = None
        self._broken_support = None
        self._bars_since_bull_break = 0
        self._bars_since_bear_break = 0
        self._entry_price = Decimal(0)
        self._best_price = Decimal(0)
        self._trailing_active = False

    def OnReseted(self):
        super(breaks_and_retests_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(breaks_and_retests_strategy, self).OnStarted2(time)

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

        close = candle.ClosePrice
        lookback = self._lookback_period.Value

        # Levels come from the closes before this candle.
        resistance = None
        support = None
        if len(self._closes) == lookback:
            resistance = max(self._closes)
            support = min(self._closes)

        self._closes.append(close)
        while len(self._closes) > lookback:
            self._closes.popleft()

        if self._broken_resistance is not None:
            self._bars_since_bull_break += 1
        if self._broken_support is not None:
            self._bars_since_bear_break += 1

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0 and self._check_stops(candle):
            return

        if resistance is None or support is None:
            return

        bull_break = close > resistance
        bear_break = close < support

        min_bars = self._retest_bars_since_breakout.Value
        max_bars = min_bars + self._retest_detection_limit.Value

        bull_retest = (not bull_break and self._broken_resistance is not None
            and min_bars <= self._bars_since_bull_break <= max_bars
            and candle.LowPrice <= self._broken_resistance and close > self._broken_resistance)

        bear_retest = (not bear_break and self._broken_support is not None
            and min_bars <= self._bars_since_bear_break <= max_bars
            and candle.HighPrice >= self._broken_support and close < self._broken_support)

        if bull_break:
            self._broken_resistance = resistance
            self._bars_since_bull_break = 0
        elif self._broken_resistance is not None and self._bars_since_bull_break > max_bars:
            self._broken_resistance = None

        if bear_break:
            self._broken_support = support
            self._bars_since_bear_break = 0
        elif self._broken_support is not None and self._bars_since_bear_break > max_bars:
            self._broken_support = None

        if (bull_break or bull_retest) and self.Position <= 0:
            if bull_retest:
                self._broken_resistance = None
            self._enter(True, close)
        elif (bear_break or bear_retest) and self.Position >= 0:
            if bear_retest:
                self._broken_support = None
            self._enter(False, close)

    def _enter(self, is_long, price):
        volume = self.Volume + abs(self.Position)
        if is_long:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)
        self._entry_price = price
        self._best_price = price
        self._trailing_active = False

    def _check_stops(self, candle):
        if self._entry_price <= 0:
            return False

        close = candle.ClosePrice
        hundred = Decimal(100)
        one = Decimal(1)
        threshold = Decimal(self._profit_threshold_percent.Value)
        gap = Decimal(self._trailing_stop_gap_percent.Value)
        stop_loss = Decimal(self._stop_loss_percent.Value)

        if self.Position > 0:
            self._best_price = max(self._best_price, close)
            if not self._trailing_active and (close - self._entry_price) / self._entry_price * hundred >= threshold:
                self._trailing_active = True
            if self._trailing_active:
                stop = self._best_price * (one - gap / hundred)
            else:
                stop = self._entry_price * (one - stop_loss / hundred)
            if candle.LowPrice <= stop:
                self.SellMarket(self.Position)
                self._entry_price = Decimal(0)
                return True
        elif self.Position < 0:
            self._best_price = min(self._best_price, close)
            if not self._trailing_active and (self._entry_price - close) / self._entry_price * hundred >= threshold:
                self._trailing_active = True
            if self._trailing_active:
                stop = self._best_price * (one + gap / hundred)
            else:
                stop = self._entry_price * (one + stop_loss / hundred)
            if candle.HighPrice >= stop:
                self.BuyMarket(-self.Position)
                self._entry_price = Decimal(0)
                return True

        return False

    def CreateClone(self):
        return breaks_and_retests_strategy()
