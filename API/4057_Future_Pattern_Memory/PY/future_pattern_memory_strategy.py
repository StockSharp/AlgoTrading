import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from collections import deque
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class future_pattern_memory_strategy(Strategy):
    """
    Future Pattern Memory: records normalized MA spread sequences,
    tracks outcomes, and trades when a recognized pattern has favorable statistics.
    """

    def __init__(self):
        super(future_pattern_memory_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._fast_ma_length = self.Param("FastMaLength", 6) \
            .SetDisplay("Fast MA", "Fast EMA period", "Indicators")
        self._slow_ma_length = self.Param("SlowMaLength", 24) \
            .SetDisplay("Slow MA", "Slow EMA period", "Indicators")
        self._pattern_length = self.Param("PatternLength", 5) \
            .SetDisplay("Pattern Length", "Number of bars in pattern signature", "Pattern")
        self._min_matches = self.Param("MinMatches", 3) \
            .SetDisplay("Min Matches", "Minimum pattern occurrences before trading", "Pattern")

        self._pattern_window = deque()
        self._patterns = {}
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(future_pattern_memory_strategy, self).OnReseted()
        self._pattern_window = deque()
        self._patterns = {}
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(future_pattern_memory_strategy, self).OnStarted(time)

        self._pattern_window = deque()
        self._patterns = {}
        self._entry_price = 0.0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ma_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        fast = float(fast_value)
        slow = float(slow_value)

        spread = fast - slow
        normalized = 1 if spread > 0 else (-1 if spread < 0 else 0)

        self._pattern_window.append(normalized)
        pat_len = self._pattern_length.Value
        while len(self._pattern_window) > pat_len:
            self._pattern_window.popleft()

        if len(self._pattern_window) < pat_len:
            return

        key = "_".join(str(v) for v in self._pattern_window)

        stats = self._patterns.get(key, (0, 0))
        buy_count, sell_count = stats

        if close > fast:
            buy_count += 1
        elif close < fast:
            sell_count += 1

        self._patterns[key] = (buy_count, sell_count)

        # Position management
        if self.Position > 0:
            if spread < 0 or (self._entry_price > 0 and close < self._entry_price * 0.985):
                self.SellMarket()
        elif self.Position < 0:
            if spread > 0 or (self._entry_price > 0 and close > self._entry_price * 1.015):
                self.BuyMarket()

        # Entry based on pattern statistics
        if self.Position == 0:
            total = buy_count + sell_count
            if total >= self._min_matches.Value:
                if buy_count > sell_count and spread > 0:
                    self._entry_price = close
                    self.BuyMarket()
                elif sell_count > buy_count and spread < 0:
                    self._entry_price = close
                    self.SellMarket()

    def CreateClone(self):
        return future_pattern_memory_strategy()
