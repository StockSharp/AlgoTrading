import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend, AverageDirectionalIndex, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class ai_supertrend_pivot_percentile_strategy(Strategy):
    """
    AI Supertrend x Pivot Percentile strategy.
    Goes long when the close is above both Supertrends, ADX is above AdxThreshold and Williams %R is above -50, and short when
    the close is below both Supertrends, ADX is above AdxThreshold and Williams %R is below -50. The opposite signal reverses
    the position, and percent take-profit and stop-loss protect it.
    """

    def __init__(self):
        super(ai_supertrend_pivot_percentile_strategy, self).__init__()
        self._length1 = self.Param("Length1", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ST1 Length", "ATR period of the first Supertrend", "Supertrend")
        self._factor1 = self.Param("Factor1", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ST1 Factor", "Multiplier of the first Supertrend", "Supertrend")
        self._length2 = self.Param("Length2", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("ST2 Length", "ATR period of the second Supertrend", "Supertrend")
        self._factor2 = self.Param("Factor2", 4.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ST2 Factor", "Multiplier of the second Supertrend", "Supertrend")
        self._adx_length = self.Param("AdxLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Length", "ADX period", "Filter")
        self._adx_threshold = self.Param("AdxThreshold", 20.0) \
            .SetDisplay("ADX Threshold", "Minimum ADX for entries", "Filter")
        self._pivot_length = self.Param("PivotLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Pivot Length", "Williams %R period", "Filter")
        self._tp_percent = self.Param("TpPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Take Profit %", "Take-profit percentage", "Risk")
        self._sl_percent = self.Param("SlPercent", 1.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted2(self, time):
        super(ai_supertrend_pivot_percentile_strategy, self).OnStarted2(time)

        st1 = SuperTrend()
        st1.Length = self._length1.Value
        st1.Multiplier = self._factor1.Value
        st2 = SuperTrend()
        st2.Length = self._length2.Value
        st2.Multiplier = self._factor2.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_length.Value
        wpr = WilliamsR()
        wpr.Length = self._pivot_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(st1, st2, adx, wpr, self._process_candle).Start()

        self.StartProtection(
            Unit(Decimal(self._tp_percent.Value), UnitTypes.Percent),
            Unit(Decimal(self._sl_percent.Value), UnitTypes.Percent),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, st1)
            self.DrawIndicator(area, st2)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)
                self.DrawIndicator(oscillators, wpr)

    def _process_candle(self, candle, st1_value, st2_value, adx_value, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        if not st1_value.IsFormed or not st2_value.IsFormed or not adx_value.IsFormed or not wpr_value.IsFormed:
            return

        if adx_value.MovingAverage is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        adx = float(adx_value.MovingAverage)
        close = float(candle.ClosePrice)
        st1 = float(to_decimal(st1_value))
        st2 = float(to_decimal(st2_value))
        wpr = float(to_decimal(wpr_value))
        strong_trend = adx > float(self._adx_threshold.Value)

        long_signal = close > st1 and close > st2 and strong_trend and wpr > -50.0
        short_signal = close < st1 and close < st2 and strong_trend and wpr < -50.0

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ai_supertrend_pivot_percentile_strategy()
