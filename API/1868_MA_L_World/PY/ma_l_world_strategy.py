import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WeightedMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ma_l_world_strategy(Strategy):
    def __init__(self):
        super(ma_l_world_strategy, self).__init__()
        self._fast_ma_length = self.Param("FastMaLength", 12) \
            .SetDisplay("Fast MA", "Period of the fast weighted MA", "Parameters")
        self._slow_ma_length = self.Param("SlowMaLength", 25) \
            .SetDisplay("Slow MA", "Period of the slow weighted MA", "Parameters")
        self._trailing_ma_period = self.Param("TrailingMaPeriod", 92) \
            .SetDisplay("Trailing EMA", "Period of trailing EMA", "Risk")
        self._stop_loss = self.Param("StopLoss", 95.0) \
            .SetDisplay("Stop Loss", "Fixed stop loss distance", "Risk")
        self._take_profit = self.Param("TakeProfit", 670.0) \
            .SetDisplay("Take Profit", "Fixed take profit distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._min_spread_percent = self.Param("MinSpreadPercent", 0.0008) \
            .SetDisplay("Minimum Spread %", "Minimum normalized spread between fast and slow MA", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 3) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._initialized = False
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    @property
    def fast_ma_length(self):
        return self._fast_ma_length.Value
    @property
    def slow_ma_length(self):
        return self._slow_ma_length.Value
    @property
    def trailing_ma_period(self):
        return self._trailing_ma_period.Value
    @property
    def stop_loss(self):
        return self._stop_loss.Value
    @property
    def take_profit(self):
        return self._take_profit.Value
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
        super(ma_l_world_strategy, self).OnReseted()
        self._initialized = False
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ma_l_world_strategy, self).OnStarted(time)
        fast_ma = WeightedMovingAverage()
        fast_ma.Length = self.fast_ma_length
        slow_ma = WeightedMovingAverage()
        slow_ma.Length = self.slow_ma_length
        trailing_ma = ExponentialMovingAverage()
        trailing_ma.Length = self.trailing_ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, trailing_ma, self.process_candle).Start()
        self.StartProtection(
            Unit(float(self.take_profit), UnitTypes.Absolute),
            Unit(float(self.stop_loss), UnitTypes.Absolute))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawIndicator(area, trailing_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow, trail):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        trail = float(trail)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return
        close = float(candle.ClosePrice)
        spread_percent = abs(fast - slow) / close if close != 0 else 0.0
        min_sp = float(self.min_spread_percent)
        cross_up = self._prev_fast <= self._prev_slow and fast > slow and spread_percent >= min_sp
        cross_down = self._prev_fast >= self._prev_slow and fast < slow and spread_percent >= min_sp
        if self._cooldown_remaining == 0:
            if cross_up and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif cross_down and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
        self._prev_fast = fast
        self._prev_slow = slow
        if self.Position > 0 and float(candle.LowPrice) <= trail:
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and float(candle.HighPrice) >= trail:
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return ma_l_world_strategy()
