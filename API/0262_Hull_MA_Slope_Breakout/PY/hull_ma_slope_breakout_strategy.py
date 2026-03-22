import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hull_ma_slope_breakout_strategy(Strategy):
    """
    Hull MA slope breakout. Enters when slope exceeds avg + k*stddev.
    """

    def __init__(self):
        super(hull_ma_slope_breakout_strategy, self).__init__()
        self._hull_length = self.Param("HullLength", 9).SetDisplay("Hull Length", "Hull MA period", "Indicators")
        self._lookback = self.Param("LookbackPeriod", 20).SetDisplay("Lookback", "Slope stats period", "Strategy")
        self._dev_mult = self.Param("DeviationMultiplier", 2.0).SetDisplay("Dev Mult", "Stddev multiplier", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_hull = 0.0
        self._slopes = None
        self._current_index = 0
        self._is_init = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_ma_slope_breakout_strategy, self).OnReseted()
        self._prev_hull = 0.0
        self._slopes = [0.0] * int(self._lookback.Value)
        self._current_index = 0
        self._is_init = False

    def OnStarted(self, time):
        super(hull_ma_slope_breakout_strategy, self).OnStarted(time)
        lb = int(self._lookback.Value)
        self._slopes = [0.0] * lb
        self._current_index = 0

        hma = HullMovingAverage()
        hma.Length = self._hull_length.Value
        atr = AverageTrueRange()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, atr, self._process_candle).Start()
        self.StartProtection(None, Unit(2, UnitTypes.Absolute))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, hull_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        hull = float(hull_val)

        if not self._is_init:
            self._prev_hull = hull
            self._is_init = True
            return

        current_slope = hull - self._prev_hull

        lb = int(self._lookback.Value)
        if self._slopes is None or len(self._slopes) != lb:
            self._slopes = [0.0] * lb
            self._current_index = 0
            self._prev_hull = hull
            return

        # Store slope in circular array
        self._slopes[self._current_index] = current_slope
        self._current_index = (self._current_index + 1) % lb

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_hull = hull
            return

        # Calculate statistics
        avg_slope = sum(self._slopes) / lb
        sum_sq = sum((s - avg_slope) ** 2 for s in self._slopes)
        std_slope = math.sqrt(sum_sq / lb)

        dm = float(self._dev_mult.Value)

        if abs(avg_slope) > 0:
            # Long signal
            if current_slope > 0 and current_slope > avg_slope + dm * std_slope and self.Position <= 0:
                self.CancelActiveOrders()
                vol = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(vol)
            # Short signal
            elif current_slope < 0 and current_slope < avg_slope - dm * std_slope and self.Position >= 0:
                self.CancelActiveOrders()
                vol = self.Volume + Math.Abs(self.Position)
                self.SellMarket(vol)

            # Exit conditions
            if self.Position > 0 and current_slope < avg_slope:
                self.SellMarket(Math.Abs(self.Position))
            elif self.Position < 0 and current_slope > avg_slope:
                self.BuyMarket(Math.Abs(self.Position))

        self._prev_hull = hull

    def CreateClone(self):
        return hull_ma_slope_breakout_strategy()
