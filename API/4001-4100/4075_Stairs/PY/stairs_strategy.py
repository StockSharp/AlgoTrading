import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage

class stairs_strategy(Strategy):
    def __init__(self):
        super(stairs_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for grid step", "Indicators")
        self._grid_multiplier = self.Param("GridMultiplier", 1.5) \
            .SetDisplay("Grid Multiplier", "ATR multiplier for grid step", "Grid")
        self._max_layers = self.Param("MaxLayers", 5) \
            .SetDisplay("Max Layers", "Maximum grid layers", "Grid")
        self._profit_multiplier = self.Param("ProfitMultiplier", 2.0) \
            .SetDisplay("Profit Multiplier", "ATR multiplier for profit target", "Grid")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA for trend direction", "Indicators")

        self._entry_price = 0.0
        self._last_grid_price = 0.0
        self._grid_count = 0
        self._prev_ema = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def GridMultiplier(self):
        return self._grid_multiplier.Value

    @property
    def MaxLayers(self):
        return self._max_layers.Value

    @property
    def ProfitMultiplier(self):
        return self._profit_multiplier.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    def OnStarted2(self, time):
        super(stairs_strategy, self).OnStarted2(time)

        self._entry_price = 0.0
        self._last_grid_price = 0.0
        self._grid_count = 0
        self._prev_ema = 0.0

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self._ema, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, atr_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        atr_v = float(atr_val)
        ema_v = float(ema_val)

        if atr_v <= 0 or self._prev_ema == 0:
            self._prev_ema = ema_v
            return

        close = float(candle.ClosePrice)
        grid_step = atr_v * float(self.GridMultiplier)
        profit_target = atr_v * float(self.ProfitMultiplier)

        # Check profit target
        if self.Position > 0 and self._entry_price > 0:
            if close - self._entry_price >= profit_target or close < ema_v:
                self.SellMarket()
                self._grid_count = 0
                self._entry_price = 0.0
                self._last_grid_price = 0.0
        elif self.Position < 0 and self._entry_price > 0:
            if self._entry_price - close >= profit_target or close > ema_v:
                self.BuyMarket()
                self._grid_count = 0
                self._entry_price = 0.0
                self._last_grid_price = 0.0

        # Grid: add to winning direction
        max_layers = self.MaxLayers
        if self.Position > 0 and self._last_grid_price > 0 and self._grid_count < max_layers:
            if close - self._last_grid_price >= grid_step:
                self.BuyMarket()
                self._last_grid_price = close
                self._grid_count += 1
        elif self.Position < 0 and self._last_grid_price > 0 and self._grid_count < max_layers:
            if self._last_grid_price - close >= grid_step:
                self.SellMarket()
                self._last_grid_price = close
                self._grid_count += 1

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ema = ema_v
            return

        # Initial entry based on trend
        if self.Position == 0:
            ema_rising = ema_v > self._prev_ema
            ema_falling = ema_v < self._prev_ema

            if ema_rising and close > ema_v:
                self._entry_price = close
                self._last_grid_price = close
                self._grid_count = 0
                self.BuyMarket()
            elif ema_falling and close < ema_v:
                self._entry_price = close
                self._last_grid_price = close
                self._grid_count = 0
                self.SellMarket()

        self._prev_ema = ema_v

    def OnReseted(self):
        super(stairs_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._last_grid_price = 0.0
        self._grid_count = 0
        self._prev_ema = 0.0

    def CreateClone(self):
        return stairs_strategy()
