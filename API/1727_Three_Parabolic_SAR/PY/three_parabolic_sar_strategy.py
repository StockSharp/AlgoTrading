import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class three_parabolic_sar_strategy(Strategy):
    def __init__(self):
        super(three_parabolic_sar_strategy, self).__init__()
        self._fast_acceleration = self.Param("FastAcceleration", 0.04) \
            .SetDisplay("Fast Acceleration", "Fast SAR acceleration", "SAR")
        self._slow_acceleration = self.Param("SlowAcceleration", TimeSpan.FromHours(4)) \
            .SetDisplay("Slow Acceleration", "Slow SAR acceleration", "SAR")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast_above = False
        self._prev_slow_above = False
        self._has_prev = False

    @property
    def fast_acceleration(self):
        return self._fast_acceleration.Value

    @property
    def slow_acceleration(self):
        return self._slow_acceleration.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_parabolic_sar_strategy, self).OnReseted()
        self._prev_fast_above = False
        self._prev_slow_above = False
        self._has_prev = False

    def OnStarted(self, time):
        super(three_parabolic_sar_strategy, self).OnStarted(time)
        fast_sar = ParabolicSar()
        slow_sar = ParabolicSar()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_sar, slow_sar, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast_sar, slow_sar):
        if candle.State != CandleStates.Finished:
            return
        fast_above = candle.ClosePrice > fast_sar
        slow_above = candle.ClosePrice > slow_sar
        if not self._has_prev:
            self._prev_fast_above = fast_above
            self._prev_slow_above = slow_above
            self._has_prev = True
            return
        # Buy when both SAR levels flip bullish
        if fast_above and slow_above and (not self._prev_fast_above or not self._prev_slow_above) and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
        # Sell when both SAR levels flip bearish
        elif not fast_above and not slow_above and (self._prev_fast_above or self._prev_slow_above) and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
        # Exit long if slow SAR turns bearish
        elif self.Position > 0 and not slow_above:
            self.SellMarket()
        # Exit short if slow SAR turns bullish
        elif self.Position < 0 and slow_above:
            self.BuyMarket()
        self._prev_fast_above = fast_above
        self._prev_slow_above = slow_above

    def CreateClone(self):
        return three_parabolic_sar_strategy()
