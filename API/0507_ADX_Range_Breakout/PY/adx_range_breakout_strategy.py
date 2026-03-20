import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, Highest
from StockSharp.Algo.Strategies import Strategy


class adx_range_breakout_strategy(Strategy):
    def __init__(self):
        super(adx_range_breakout_strategy, self).__init__()
        self._highest_period = self.Param("HighestPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Highest Lookback", "Bars for highest close", "Indicators")
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetDisplay("ADX Threshold", "Upper ADX limit for range", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_highest = 0.0
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
        super(adx_range_breakout_strategy, self).OnReseted()
        self._prev_highest = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adx_range_breakout_strategy, self).OnStarted(time)
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        highest = Highest()
        highest.Length = self._highest_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, highest, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, adx_value, highest_value):
        if candle.State != CandleStates.Finished:
            return
        cur_highest = float(highest_value.GetValue[float]())
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_highest = cur_highest
            return
        if self._prev_highest == 0:
            self._prev_highest = cur_highest
            return
        adx_typed = adx_value
        adx_ma = adx_typed.MovingAverage
        if adx_ma is None:
            self._prev_highest = cur_highest
            return
        adx_v = float(adx_ma)
        close = float(candle.ClosePrice)
        threshold = float(self._adx_threshold.Value)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_highest = cur_highest
            return
        if self.Position == 0 and adx_v < threshold and close > self._prev_highest:
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and (adx_v >= threshold * 1.5 or close < self._prev_highest * 0.98):
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_highest = cur_highest

    def CreateClone(self):
        return adx_range_breakout_strategy()
