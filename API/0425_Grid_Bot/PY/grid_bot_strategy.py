import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class grid_bot_strategy(Strategy):
    """Grid Bot Strategy. Dynamic grid around EMA with ATR spacing."""

    def __init__(self):
        super(grid_bot_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._ma_length = self.Param("MALength", 50) \
            .SetDisplay("MA Length", "Moving average for grid center", "Grid Settings")
        self._atr_length = self.Param("ATRLength", 14) \
            .SetDisplay("ATR Length", "ATR period for grid spacing", "Grid Settings")
        self._grid_count = self.Param("GridCount", 3) \
            .SetDisplay("Grid Count", "Number of grid levels each side", "Grid Settings")
        self._grid_multiplier = self.Param("GridMultiplier", 0.5) \
            .SetDisplay("Grid Multiplier", "ATR multiplier for grid spacing", "Grid Settings")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._ma = None
        self._atr = None
        self._prev_close = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(grid_bot_strategy, self).OnReseted()
        self._ma = None
        self._atr = None
        self._prev_close = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(grid_bot_strategy, self).OnStarted(time)

        self._ma = ExponentialMovingAverage()
        self._ma.Length = int(self._ma_length.Value)

        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ma, self._atr, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._ma.IsFormed or not self._atr.IsFormed:
            self._prev_close = float(candle.ClosePrice)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = float(candle.ClosePrice)
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = float(candle.ClosePrice)
            return

        if self._prev_close == 0.0 or float(atr_val) <= 0:
            self._prev_close = float(candle.ClosePrice)
            return

        close = float(candle.ClosePrice)
        ma = float(ma_val)
        atr = float(atr_val)
        grid_mult = float(self._grid_multiplier.Value)
        grid_count = int(self._grid_count.Value)
        cooldown = int(self._cooldown_bars.Value)
        grid_spacing = atr * grid_mult

        for i in range(1, grid_count + 1):
            lower_grid = ma - grid_spacing * i
            upper_grid = ma + grid_spacing * i

            if self._prev_close > lower_grid and close <= lower_grid and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                self.BuyMarket(self.Volume)
                self._cooldown_remaining = cooldown
                self._prev_close = close
                return

            if self._prev_close < upper_grid and close >= upper_grid and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                self.SellMarket(self.Volume)
                self._cooldown_remaining = cooldown
                self._prev_close = close
                return

        # Mean reversion exits at MA
        if self.Position > 0 and self._prev_close < ma and close >= ma:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and self._prev_close > ma and close <= ma:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_close = close

    def CreateClone(self):
        return grid_bot_strategy()
