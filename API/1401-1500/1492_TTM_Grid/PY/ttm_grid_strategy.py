import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ttm_grid_strategy(Strategy):
    def __init__(self):
        super(ttm_grid_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 8) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow Period", "Slow EMA period", "Indicators")
        self._grid_levels = self.Param("GridLevels", 14) \
            .SetDisplay("RSI Period", "RSI period for momentum", "Strategy")
        self._grid_spacing = self.Param("GridSpacing", 0.005) \
            .SetDisplay("Grid Spacing", "Distance between grid levels (fraction)", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_rsi = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown = 0

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def grid_levels(self):
        return self._grid_levels.Value

    @property
    def grid_spacing(self):
        return self._grid_spacing.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ttm_grid_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(ttm_grid_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.grid_levels
        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self.fast_period
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = int(self.slow_period)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema_fast, ema_slow, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi_val, ema_fast, ema_slow):
        if candle.State != CandleStates.Finished:
            return
        rsi_val = float(rsi_val)
        ema_fast = float(ema_fast)
        ema_slow = float(ema_slow)
        if self._prev_rsi == 0 or self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_rsi = rsi_val
            self._prev_fast = ema_fast
            self._prev_slow = ema_slow
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_rsi = rsi_val
            self._prev_fast = ema_fast
            self._prev_slow = ema_slow
            return
        hist = ema_fast - ema_slow
        hist_up = hist > 0
        hist_down = hist < 0
        rsi_cross_up = self._prev_rsi <= 50 and rsi_val > 50
        rsi_cross_down = self._prev_rsi >= 50 and rsi_val < 50
        if self.Position > 0 and rsi_cross_down:
            self.SellMarket()
            self._cooldown = 80
        elif self.Position < 0 and rsi_cross_up:
            self.BuyMarket()
            self._cooldown = 80
        if self.Position == 0:
            if rsi_cross_up and hist_up:
                self.BuyMarket()
                self._cooldown = 80
            elif rsi_cross_down and hist_down:
                self.SellMarket()
                self._cooldown = 80
        self._prev_rsi = rsi_val
        self._prev_fast = ema_fast
        self._prev_slow = ema_slow

    def CreateClone(self):
        return ttm_grid_strategy()
