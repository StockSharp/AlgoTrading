import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hma_crossover_atr_curvature_strategy(Strategy):
    """
    HMA Crossover ATR Curvature strategy.
    Goes long when the fast HMA crosses above the slow HMA while the curvature (second difference) of the fast HMA is above
    CurvatureThreshold, and short on the opposite cross with curvature below -CurvatureThreshold, reversing an opposite position.
    The order size risks RiskPercent of the portfolio over a distance of AtrMultiplier ATRs, and an ATR trailing stop of
    TrailMultiplier ATRs closes the position.
    """

    def __init__(self):
        super(hma_crossover_atr_curvature_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 15).SetGreaterThanZero().SetDisplay("Fast Length", "Fast HMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 34).SetGreaterThanZero().SetDisplay("Slow Length", "Slow HMA length", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Indicators")
        self._risk_percent = self.Param("RiskPercent", 1.0).SetGreaterThanZero().SetDisplay("Risk %", "Percent of the portfolio risked per trade", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiple used as risk distance for sizing", "Risk")
        self._trail_multiplier = self.Param("TrailMultiplier", 1.0).SetGreaterThanZero().SetDisplay("Trail Multiplier", "ATR multiple of the trailing stop distance", "Risk")
        self._curvature_threshold = self.Param("CurvatureThreshold", 0.0).SetDisplay("Curvature Threshold", "Minimum curvature of the fast HMA", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_fast2 = None
        self._prev_slow = None
        self._trail_stop = None

    def OnReseted(self):
        super(hma_crossover_atr_curvature_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(hma_crossover_atr_curvature_strategy, self).OnStarted2(time)

        self._reset_state()

        fast_hma = HullMovingAverage()
        fast_hma.Length = self._fast_length.Value
        slow_hma = HullMovingAverage()
        slow_hma.Length = self._slow_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_hma, slow_hma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_hma)
            self.DrawIndicator(area, slow_hma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)
        atr = float(atr_value)

        prev_fast = self._prev_fast
        prev_fast2 = self._prev_fast2
        prev_slow = self._prev_slow

        self._prev_fast2 = self._prev_fast
        self._prev_fast = fast
        self._prev_slow = slow

        if prev_fast is None or prev_fast2 is None or prev_slow is None or atr <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        trail_distance = atr * float(self._trail_multiplier.Value)

        if self.Position > 0:
            stop = close - trail_distance
            self._trail_stop = stop if self._trail_stop is None else max(self._trail_stop, stop)
            if float(candle.LowPrice) <= self._trail_stop:
                self.SellMarket(self.Position)
                self._trail_stop = None
                return
        elif self.Position < 0:
            stop = close + trail_distance
            self._trail_stop = stop if self._trail_stop is None else min(self._trail_stop, stop)
            if float(candle.HighPrice) >= self._trail_stop:
                self.BuyMarket(-self.Position)
                self._trail_stop = None
                return

        threshold = float(self._curvature_threshold.Value)
        curvature = fast - 2.0 * prev_fast + prev_fast2
        cross_up = prev_fast <= prev_slow and fast > slow
        cross_down = prev_fast >= prev_slow and fast < slow

        if cross_up and curvature > threshold and self.Position <= 0:
            self.BuyMarket(self._get_order_volume(atr) + abs(self.Position))
            self._trail_stop = close - trail_distance
        elif cross_down and curvature < -threshold and self.Position >= 0:
            self.SellMarket(self._get_order_volume(atr) + abs(self.Position))
            self._trail_stop = close + trail_distance

    def _get_order_volume(self, atr):
        equity = 0.0
        if self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
            equity = float(self.Portfolio.CurrentValue)
        risk_distance = atr * float(self._atr_multiplier.Value)

        if equity <= 0 or risk_distance <= 0:
            return self.Volume

        step = 1.0
        if self.Security is not None and self.Security.VolumeStep is not None:
            step = float(self.Security.VolumeStep)
        if step <= 0:
            step = 1.0

        volume = math.floor(equity * float(self._risk_percent.Value) / 100.0 / risk_distance / step) * step
        return Decimal(volume) if volume > 0 else self.Volume

    def CreateClone(self):
        return hma_crossover_atr_curvature_strategy()
