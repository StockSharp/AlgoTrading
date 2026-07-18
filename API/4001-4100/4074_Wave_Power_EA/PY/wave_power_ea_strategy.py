import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex

class wave_power_ea_strategy(Strategy):
    def __init__(self):
        super(wave_power_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 12) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._grid_step_percent = self.Param("GridStepPercent", 0.5) \
            .SetDisplay("Grid Step %", "Price move % to add to position", "Grid")
        self._max_grid_orders = self.Param("MaxGridOrders", 5) \
            .SetDisplay("Max Grid Orders", "Maximum averaging orders", "Grid")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._grid_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def GridStepPercent(self):
        return self._grid_step_percent.Value

    @property
    def MaxGridOrders(self):
        return self._max_grid_orders.Value

    def OnStarted2(self, time):
        super(wave_power_ea_strategy, self).OnStarted2(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._grid_count = 0

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastPeriod
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowPeriod
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_ema, self._slow_ema, self._rsi, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_val, slow_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)
        rv = float(rsi_val)

        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        close = float(candle.ClosePrice)
        bullish_cross = self._prev_fast <= self._prev_slow and fv > sv
        bearish_cross = self._prev_fast >= self._prev_slow and fv < sv

        # Exit on opposite cross
        if self.Position > 0 and bearish_cross:
            self.SellMarket()
            self._grid_count = 0
            self._entry_price = 0.0
        elif self.Position < 0 and bullish_cross:
            self.BuyMarket()
            self._grid_count = 0
            self._entry_price = 0.0

        # Grid averaging: add to position if price moved against us
        grid_step = float(self.GridStepPercent)
        max_grid = self.MaxGridOrders

        if self.Position > 0 and self._entry_price > 0 and self._grid_count < max_grid:
            drop_percent = (self._entry_price - close) / self._entry_price * 100.0
            if drop_percent >= grid_step * (self._grid_count + 1):
                self.BuyMarket()
                self._grid_count += 1
        elif self.Position < 0 and self._entry_price > 0 and self._grid_count < max_grid:
            rise_percent = (close - self._entry_price) / self._entry_price * 100.0
            if rise_percent >= grid_step * (self._grid_count + 1):
                self.SellMarket()
                self._grid_count += 1

        # New entry
        if self.Position == 0:
            if bullish_cross and rv > 50:
                self._entry_price = close
                self._grid_count = 0
                self.BuyMarket()
            elif bearish_cross and rv < 50:
                self._entry_price = close
                self._grid_count = 0
                self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def OnReseted(self):
        super(wave_power_ea_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0
        self._grid_count = 0

    def CreateClone(self):
        return wave_power_ea_strategy()
