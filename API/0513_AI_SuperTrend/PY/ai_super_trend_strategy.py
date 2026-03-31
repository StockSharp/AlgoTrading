import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, WeightedMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class ai_super_trend_strategy(Strategy):
    def __init__(self):
        super(ai_super_trend_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period = self.Param("AtrPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR period for SuperTrend", "SuperTrend")
        self._atr_factor = self.Param("AtrFactor", 3.0) \
            .SetDisplay("ATR Factor", "ATR factor for SuperTrend", "SuperTrend")
        self._wma_length = self.Param("WmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("WMA Length", "WMA length for trend filter", "AI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_is_up_trend = False
        self._is_initialized = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ai_super_trend_strategy, self).OnReseted()
        self._prev_is_up_trend = False
        self._is_initialized = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(ai_super_trend_strategy, self).OnStarted2(time)
        st = SuperTrend()
        st.Length = int(self._atr_period.Value)
        st.Multiplier = self._atr_factor.Value
        wma = WeightedMovingAverage()
        wma.Length = int(self._wma_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(st, wma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, st)
            self.DrawIndicator(area, wma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, st_value, wma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        is_up_trend = st_value.IsUpTrend
        wma_v = float(IndicatorHelper.ToDecimal(wma_value))
        close = float(candle.ClosePrice)

        if not self._is_initialized:
            self._prev_is_up_trend = is_up_trend
            self._is_initialized = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_is_up_trend = is_up_trend
            return

        cooldown = int(self._cooldown_bars.Value)

        if not self._prev_is_up_trend and is_up_trend and close > wma_v and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self._prev_is_up_trend and not is_up_trend and close < wma_v and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._prev_is_up_trend = is_up_trend

    def CreateClone(self):
        return ai_super_trend_strategy()
