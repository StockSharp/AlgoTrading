import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Array
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import (
    LinearRegression,
    Highest,
    Lowest,
    SimpleMovingAverage,
    AverageDirectionalIndex,
    AverageTrueRange,
    IIndicator
)
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class strategy_tester_strategy(Strategy):
    """Momentum and ADX based tester strategy with ATR risk reference.

    The script replicates the original C# sample that demonstrates how to
    combine momentum swings with trend strength confirmation from ADX. It opens
    long positions when either momentum peaks while ADX declines or when ADX
    peaks and momentum begins to rise from negative territory. Exits are
    controlled by optional flags allowing momentum based or custom strategy
    logic."""

    def __init__(self):
        super(strategy_tester_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._momentum_length = self.Param("MomentumLength", 20) \
            .SetDisplay("Momentum Length", "Linear regression period", "Momentum")

        self._adx_smoothing_length = self.Param("AdxSmoothingLength", 14) \
            .SetDisplay("ADX Smoothing", "ADX smoothing length", "ADX")

        self._di_length = self.Param("DiLength", 14) \
            .SetDisplay("DI Length", "Directional index length", "ADX")

        self._adx_key_level = self.Param("AdxKeyLevel", 25) \
            .SetDisplay("ADX Key Level", "Trend strength threshold", "ADX")

        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "ATR")

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Mult", "ATR multiplier", "ATR")

        self._structure_lookback = self.Param("StructureLookback", 5) \
            .SetDisplay("Structure Lookback", "Lookback for pivots", "Strategy")

        self._exit_by_momentum = self.Param("ExitByMomentum", True) \
            .SetDisplay("Exit by Momentum", "Enable momentum exit", "Strategy")

        self._exit_by_strategy = self.Param("ExitByStrategy", False) \
            .SetDisplay("Exit by Strategy", "Enable custom exit", "Strategy")

        self._momentum = None
        self._highest = None
        self._lowest = None
        self._close_sma = None
        self._adx = None
        self._atr = None

        self._prev_momentum = 0
        self._prev_adx = 0
        self._prev_close = 0

    # region properties
    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def momentum_length(self):
        return self._momentum_length.Value

    @property
    def adx_smoothing_length(self):
        return self._adx_smoothing_length.Value

    @property
    def di_length(self):
        return self._di_length.Value

    @property
    def adx_key_level(self):
        return self._adx_key_level.Value

    @property
    def atr_length(self):
        return self._atr_length.Value

    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value

    @property
    def structure_lookback(self):
        return self._structure_lookback.Value

    @property
    def exit_by_momentum(self):
        return self._exit_by_momentum.Value

    @property
    def exit_by_strategy(self):
        return self._exit_by_strategy.Value

    # endregion

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super().OnReseted()
        self._prev_momentum = self._prev_adx = self._prev_close = 0

    def OnStarted(self, time):
        super().OnStarted(time)

        self._highest = Highest(Length=self.momentum_length)
        self._lowest = Lowest(Length=self.momentum_length)
        self._close_sma = SimpleMovingAverage(Length=self.momentum_length)
        self._momentum = LinearRegression(Length=self.momentum_length)
        self._adx = AverageDirectionalIndex(Length=self.di_length)
        self._atr = AverageTrueRange(Length=self.atr_length)

        sub = self.SubscribeCandles(self.candle_type)
        
        indicators_array = Array.CreateInstance(IIndicator, 6)
        indicators_array[0] = self._highest
        indicators_array[1] = self._lowest
        indicators_array[2] = self._close_sma
        indicators_array[3] = self._momentum
        indicators_array[4] = self._adx
        indicators_array[5] = self._atr
        
        sub.BindEx(indicators_array, self.ProcessCandle)
        sub.Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, values):
        if candle.State != CandleStates.Finished:
            return

        if not self._momentum.IsFormed or not self._adx.IsFormed or not self._atr.IsFormed:
            return

        high_val = float(values[0])
        low_val = float(values[1])
        sma_val = float(values[2])

        lr_val = values[3]
        momentum_val = float(lr_val.LinearRegSlope)

        adx_val_obj = values[4]
        adx_val = float(adx_val_obj.MovingAverage)

        atr_val = float(values[5])

        momentum_pivot_high = self._prev_momentum != 0 and self._prev_momentum > momentum_val
        adx_pivot_high = self._prev_adx != 0 and self._prev_adx > adx_val

        self.CheckEntry(candle, momentum_val, adx_val, atr_val, momentum_pivot_high, adx_pivot_high)
        self.CheckExit(candle, momentum_val, adx_val, momentum_pivot_high)

        self._prev_momentum = momentum_val
        self._prev_adx = adx_val
        self._prev_close = candle.ClosePrice

    def CheckEntry(self, candle, momentum, adx_val, atr_val, momentum_pivot_high, adx_pivot_high):
        price = candle.ClosePrice

        buy_cond1 = momentum_pivot_high and adx_val < self._prev_adx
        buy_cond2 = adx_pivot_high and momentum >= self._prev_momentum and momentum < 0

        if (buy_cond1 or buy_cond2) and self.Position == 0:
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, self.Volume))

    def CheckExit(self, candle, momentum, adx_val, momentum_pivot_high):
        if self.Position > 0 and self.exit_by_momentum and momentum_pivot_high:
            self.RegisterOrder(self.CreateOrder(Sides.Sell, self._prev_close, abs(self.Position)))

        if self.Position > 0 and self.exit_by_strategy:
            pass

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return strategy_tester_strategy()
