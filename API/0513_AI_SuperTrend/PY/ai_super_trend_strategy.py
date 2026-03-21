import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, WeightedMovingAverage
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

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(ai_super_trend_strategy, self).OnReseted()
        self._prev_is_up_trend = False
        self._is_initialized = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(ai_super_trend_strategy, self).OnStarted(time)
        st = SuperTrend()
        st.Length = self._atr_period.Value
        st.Multiplier = self._atr_factor.Value
        wma = WeightedMovingAverage()
        wma.Length = self._wma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(st, wma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, st)
            self.DrawIndicator(area, wma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, st_value, wma_value):
        if candle.State != CandleStates.Finished:
            return
        is_up_trend = st_value.IsUpTrend
        wma_v = float(wma_value.GetValue[float]())
        close = float(candle.ClosePrice)
        if not self._is_initialized:
            self._prev_is_up_trend = is_up_trend
            self._is_initialized = True
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_is_up_trend = is_up_trend
            return
        if not self._prev_is_up_trend and is_up_trend and close > wma_v and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self._prev_is_up_trend and not is_up_trend and close < wma_v and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_is_up_trend = is_up_trend

    def CreateClone(self):
        return ai_super_trend_strategy()
