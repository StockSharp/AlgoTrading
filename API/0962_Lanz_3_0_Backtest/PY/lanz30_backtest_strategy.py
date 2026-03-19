import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class lanz30_backtest_strategy(Strategy):
    """
    LANZ 3.0: session range breakout with Fibonacci targets.
    """

    def __init__(self):
        super(lanz30_backtest_strategy, self).__init__()
        self._use_optimized_fibo = self.Param("UseOptimizedFibo", True).SetDisplay("Optimized Fibo", "Use optimized Fibonacci", "General")
        self._max_entries = self.Param("MaxEntries", 20).SetDisplay("Max Entries", "Max entries per run", "Risk")
        self._cooldown_days = self.Param("CooldownDays", 2).SetDisplay("Cooldown Days", "Min days between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._ref_high = 0.0
        self._ref_low = 0.0
        self._range_session = False
        self._tp = 0.0
        self._sl = 0.0
        self._is_buy = False
        self._direction_defined = False
        self._trade_executed = False
        self._entries = 0
        self._last_trade_day = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(lanz30_backtest_strategy, self).OnReseted()
        self._ref_high = 0.0
        self._ref_low = 0.0
        self._range_session = False
        self._tp = 0.0
        self._sl = 0.0
        self._is_buy = False
        self._direction_defined = False
        self._trade_executed = False
        self._entries = 0
        self._last_trade_day = None

    def OnStarted(self, time):
        super(lanz30_backtest_strategy, self).OnStarted(time)
        ema1 = ExponentialMovingAverage()
        ema1.Length = 10
        ema2 = ExponentialMovingAverage()
        ema2.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema1, ema2, self._process_candle).Start()

    def _process_candle(self, candle, d1, d2):
        if candle.State != CandleStates.Finished:
            return

        t = candle.OpenTime
        hour = t.Hour
        minute = t.Minute
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        range_session = hour >= 9 and hour < 11
        new_session = range_session and not self._range_session
        self._range_session = range_session

        if new_session:
            self._ref_high = high
            self._ref_low = low
            self._tp = 0
            self._sl = 0
            self._direction_defined = False
            self._is_buy = False
            self._trade_executed = False
        elif range_session:
            if high > self._ref_high:
                self._ref_high = high
            if low < self._ref_low:
                self._ref_low = low

        decision_window = hour == 11
        entry_window = 11 <= hour < 15
        close_time = hour == 15 and minute >= 45

        if decision_window and not self._direction_defined and self._ref_high > self._ref_low:
            fibo_range = self._ref_high - self._ref_low
            asia_mid = (self._ref_high + self._ref_low) / 2.0
            self._is_buy = close < asia_mid
            if self._use_optimized_fibo.Value:
                self._tp = (self._ref_low + 1.95 * fibo_range) if self._is_buy else (self._ref_high - 1.95 * fibo_range)
                self._sl = (self._ref_low - 0.65 * fibo_range) if self._is_buy else (self._ref_high + 0.65 * fibo_range)
            else:
                self._tp = (self._ref_low + 2.25 * fibo_range) if self._is_buy else (self._ref_high - 2.25 * fibo_range)
                self._sl = (self._ref_low - 0.75 * fibo_range) if self._is_buy else (self._ref_high + 0.75 * fibo_range)
            self._direction_defined = True

        today = t.Date
        can_trade = self._entries < self._max_entries.Value
        if self._last_trade_day is not None:
            diff = (today - self._last_trade_day).Days
            if diff < self._cooldown_days.Value:
                can_trade = False

        if self._direction_defined and entry_window and not self._trade_executed and can_trade:
            if self._is_buy:
                self.BuyMarket()
            else:
                self.SellMarket()
            self._trade_executed = True
            self._entries += 1
            self._last_trade_day = today

        if self._trade_executed and self._tp != 0 and self._sl != 0:
            if self.Position > 0:
                if low <= self._sl:
                    self.SellMarket()
                elif high >= self._tp:
                    self.SellMarket()
            elif self.Position < 0:
                if high >= self._sl:
                    self.BuyMarket()
                elif low <= self._tp:
                    self.BuyMarket()

        if close_time and self._trade_executed:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()

    def CreateClone(self):
        return lanz30_backtest_strategy()
