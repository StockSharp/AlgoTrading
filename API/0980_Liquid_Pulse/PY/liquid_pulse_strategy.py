import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class liquid_pulse_strategy(Strategy):
    """
    Liquid Pulse: EMA crossover with SL/TP based on candle range.
    Simplified version focusing on dual EMA trend signals.
    """

    def __init__(self):
        super(liquid_pulse_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 13) \
            .SetDisplay("Slow Period", "Slow EMA period", "Indicators")
        self._daily_trade_limit = self.Param("DailyTradeLimit", 20) \
            .SetDisplay("Daily Trade Limit", "Max trades per day", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._stop = 0.0
        self._tp = 0.0
        self._day = None
        self._daily_trades = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(liquid_pulse_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._stop = 0.0
        self._tp = 0.0
        self._day = None
        self._daily_trades = 0

    def OnStarted(self, time):
        super(liquid_pulse_strategy, self).OnStarted(time)

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        slow = float(slow_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        day = candle.OpenTime.Date

        if self._day is None or self._day != day:
            self._day = day
            self._daily_trades = 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast = fast
            self._prev_slow = slow
            return

        # Check SL/TP
        if self.Position > 0 and self._stop > 0:
            if low <= self._stop or high >= self._tp:
                self.SellMarket()
                self._entry_price = 0.0
                self._stop = 0.0
                self._tp = 0.0
        elif self.Position < 0 and self._stop > 0:
            if high >= self._stop or low <= self._tp:
                self.BuyMarket()
                self._entry_price = 0.0
                self._stop = 0.0
                self._tp = 0.0

        candle_range = high - low
        if candle_range <= 0:
            candle_range = 0.01

        if self._daily_trades < self._daily_trade_limit.Value:
            bull = self._prev_fast <= self._prev_slow and fast > slow
            bear = self._prev_fast >= self._prev_slow and fast < slow

            if bull and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
                self._stop = close - candle_range * 1.5
                self._tp = close + candle_range * 2.0
                self._daily_trades += 1
            elif bear and self.Position >= 0:
                self.SellMarket()
                self._entry_price = close
                self._stop = close + candle_range * 1.5
                self._tp = close - candle_range * 2.0
                self._daily_trades += 1

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return liquid_pulse_strategy()
