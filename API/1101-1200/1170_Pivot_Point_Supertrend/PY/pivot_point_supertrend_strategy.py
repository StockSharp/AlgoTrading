import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Decimal, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class pivot_point_supertrend_strategy(Strategy):
    def __init__(self):
        super(pivot_point_supertrend_strategy, self).__init__()
        self._pivot_period = self.Param("PivotPeriod", 2).SetGreaterThanZero() \
            .SetDisplay("Pivot Period", "Finished candles on each side of a confirmed pivot", "Indicators")
        self._atr_factor = self.Param("AtrFactor", Decimal.Parse("3")).SetGreaterThanZero() \
            .SetDisplay("ATR Factor", "ATR width of the pivot bands", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 10).SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Wilder ATR period of the pivot bands", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of finished candles", "General")
        self._reset_signals()

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def _reset_signals(self):
        self._pivots = []
        self._center = None
        self._support = None
        self._resistance = None
        self._previous_close = None
        self._pivot_up = None

    def OnReseted(self):
        super(pivot_point_supertrend_strategy, self).OnReseted()
        self._reset_signals()

    def OnStarted2(self, time):
        super(pivot_point_supertrend_strategy, self).OnStarted2(time)
        self._reset_signals()
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, self.OnProcess, True).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return
        previous_close = candle.ClosePrice if self._previous_close is None else self._previous_close
        self._previous_close = candle.ClosePrice
        self._pivots.append((candle.HighPrice, candle.LowPrice))
        period = self._pivot_period.Value
        if len(self._pivots) > 2 * period + 1:
            self._pivots.pop(0)
        if len(self._pivots) == 2 * period + 1:
            candidate = self._pivots[period]
            neighbors = self._pivots[:period] + self._pivots[period + 1:]
            is_high = all(candidate[0] > bar[0] for bar in neighbors)
            is_low = all(candidate[1] < bar[1] for bar in neighbors)
            if is_high or is_low:
                pivot = candidate[0] if is_high else candidate[1]
                self._center = pivot if self._center is None else (Decimal(2) * self._center + pivot) / Decimal(3)
        previous_pivot_up = self._pivot_up
        if self._center is not None and atr_value.IsFormed and not atr_value.IsEmpty:
            width = self._atr_factor.Value * atr_value.GetValue[Decimal](None)
            old_support = self._support
            old_resistance = self._resistance
            basic_support = self._center - width
            basic_resistance = self._center + width
            self._support = max(basic_support, old_support) if old_support is not None and previous_close > old_support else basic_support
            self._resistance = min(basic_resistance, old_resistance) if old_resistance is not None and previous_close < old_resistance else basic_resistance
            if old_resistance is not None and candle.ClosePrice > old_resistance:
                self._pivot_up = True
            elif old_support is not None and candle.ClosePrice < old_support:
                self._pivot_up = False
            elif self._pivot_up is None:
                self._pivot_up = True
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if previous_pivot_up is False and self._pivot_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif previous_pivot_up is True and not self._pivot_up and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return pivot_point_supertrend_strategy()
