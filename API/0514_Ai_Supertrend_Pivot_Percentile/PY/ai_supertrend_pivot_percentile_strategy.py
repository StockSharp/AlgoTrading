import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, AverageDirectionalIndex, WilliamsR, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class ai_supertrend_pivot_percentile_strategy(Strategy):
    def __init__(self):
        super(ai_supertrend_pivot_percentile_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._length1 = self.Param("Length1", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ST1 Length", "First Supertrend ATR length", "Supertrend")
        self._factor1 = self.Param("Factor1", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ST1 Factor", "First Supertrend multiplier", "Supertrend")
        self._length2 = self.Param("Length2", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("ST2 Length", "Second Supertrend ATR length", "Supertrend")
        self._factor2 = self.Param("Factor2", 4.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ST2 Factor", "Second Supertrend multiplier", "Supertrend")
        self._adx_length = self.Param("AdxLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Length", "ADX calculation period", "Filter")
        self._adx_threshold = self.Param("AdxThreshold", 15.0) \
            .SetDisplay("ADX Threshold", "Minimum ADX for trading", "Filter")
        self._pivot_length = self.Param("PivotLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Pivot Length", "Length for Williams %R", "Filter")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ai_supertrend_pivot_percentile_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ai_supertrend_pivot_percentile_strategy, self).OnStarted(time)
        st1 = SuperTrend()
        st1.Length = int(self._length1.Value)
        st1.Multiplier = self._factor1.Value
        st2 = SuperTrend()
        st2.Length = int(self._length2.Value)
        st2.Multiplier = self._factor2.Value
        adx = AverageDirectionalIndex()
        adx.Length = int(self._adx_length.Value)
        wpr = WilliamsR()
        wpr.Length = int(self._pivot_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(st1, st2, adx, wpr, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, st1)
            self.DrawIndicator(area, st2)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, st1_value, st2_value, adx_value, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            return

        adx_v = float(adx_ma)
        wpr_v = float(IndicatorHelper.ToDecimal(wpr_value))
        st1_v = float(IndicatorHelper.ToDecimal(st1_value))
        st2_v = float(IndicatorHelper.ToDecimal(st2_value))
        close = float(candle.ClosePrice)
        threshold = float(self._adx_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        is_bull = close > st1_v and close > st2_v
        is_bear = close < st1_v and close < st2_v
        strong_trend = adx_v > threshold
        pivot_bull = wpr_v > -50.0
        pivot_bear = wpr_v < -50.0

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        if is_bull and strong_trend and pivot_bull and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown
        elif is_bear and strong_trend and pivot_bear and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and (not is_bull or not pivot_bull):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and (not is_bear or not pivot_bear):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return ai_supertrend_pivot_percentile_strategy()
