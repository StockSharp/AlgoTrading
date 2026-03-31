import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ma_by_ma_strategy(Strategy):
    def __init__(self):
        super(ma_by_ma_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 7) \
            .SetDisplay("Fast EMA Length", "Period for fast EMA", "Indicator")
        self._slow_length = self.Param("SlowLength", 21) \
            .SetDisplay("Slow EMA Length", "Period for slow EMA", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._min_spread_percent = self.Param("MinSpreadPercent", 0.003) \
            .SetDisplay("Minimum Spread %", "Minimum normalized spread between fast and slow EMA values", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._is_initialized = False
        self._was_fast_below_slow = False
        self._cooldown_remaining = 0

    @property
    def fast_length(self):
        return self._fast_length.Value
    @property
    def slow_length(self):
        return self._slow_length.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def min_spread_percent(self):
        return self._min_spread_percent.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(ma_by_ma_strategy, self).OnReseted()
        self._is_initialized = False
        self._was_fast_below_slow = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(ma_by_ma_strategy, self).OnStarted2(time)
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_length
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fast_value = float(fast_value)
        slow_value = float(slow_value)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        spread_percent = abs(fast_value - slow_value) / slow_value if slow_value != 0 else 0.0
        if not self._is_initialized:
            self._was_fast_below_slow = fast_value < slow_value
            self._is_initialized = True
            return
        is_fast_below_slow = fast_value < slow_value
        min_sp = float(self.min_spread_percent)
        if self._cooldown_remaining == 0 and spread_percent >= min_sp:
            if self._was_fast_below_slow and not is_fast_below_slow and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif not self._was_fast_below_slow and is_fast_below_slow and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
        self._was_fast_below_slow = is_fast_below_slow

    def CreateClone(self):
        return ma_by_ma_strategy()
