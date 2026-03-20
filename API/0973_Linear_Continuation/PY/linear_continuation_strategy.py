import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class linear_continuation_strategy(Strategy):
    def __init__(self):
        super(linear_continuation_strategy, self).__init__()
        self._ma1_period = self.Param("Ma1Period", 120) \
            .SetGreaterThanZero() \
            .SetDisplay("MA1 Period", "Period for MA1", "General")
        self._ma2_period = self.Param("Ma2Period", 55) \
            .SetGreaterThanZero() \
            .SetDisplay("MA2 Period", "Period for MA2", "General")
        self._ma3_period = self.Param("Ma3Period", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("MA3 Period", "Period for MA3", "General")
        self._min_spread_pct = self.Param("MinSpreadPercent", 0.03) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Spread Pct", "Minimal fast/slow MA spread in percent", "General")
        self._signal_cooldown = self.Param("SignalCooldownBars", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Minimum bars between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._last_trend = 0
        self._bars_from_signal = 999999

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(linear_continuation_strategy, self).OnReseted()
        self._last_trend = 0
        self._bars_from_signal = 999999

    def OnStarted(self, time):
        super(linear_continuation_strategy, self).OnStarted(time)
        ma1 = SimpleMovingAverage()
        ma1.Length = self._ma1_period.Value
        ma2 = SimpleMovingAverage()
        ma2.Length = self._ma2_period.Value
        ma3 = SimpleMovingAverage()
        ma3.Length = self._ma3_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma1, ma2, ma3, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma1)
            self.DrawIndicator(area, ma2)
            self.DrawIndicator(area, ma3)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ma1_val, ma2_val, ma3_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_from_signal += 1
        m1 = float(ma1_val)
        m2 = float(ma2_val)
        m3 = float(ma3_val)
        close = float(candle.ClosePrice)
        bullish = m3 > m2 and m2 > m1
        bearish = m3 < m2 and m2 < m1
        if not bullish and not bearish:
            return
        if close <= 0.0:
            return
        spread_pct = abs(m3 - m1) / close * 100.0
        min_spread = float(self._min_spread_pct.Value)
        if spread_pct < min_spread:
            return
        cd = self._signal_cooldown.Value
        if self._bars_from_signal < cd:
            return
        trend = 1 if bullish else -1
        if trend == self._last_trend:
            return
        if trend > 0 and self.Position <= 0:
            self.BuyMarket()
            self._last_trend = trend
            self._bars_from_signal = 0
        elif trend < 0 and self.Position >= 0:
            self.SellMarket()
            self._last_trend = trend
            self._bars_from_signal = 0

    def CreateClone(self):
        return linear_continuation_strategy()
