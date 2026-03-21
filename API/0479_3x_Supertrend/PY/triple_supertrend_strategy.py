import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class triple_supertrend_strategy(Strategy):
    def __init__(self):
        super(triple_supertrend_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._p1 = self.Param("AtrPeriod1", 11) \
            .SetDisplay("ATR1", "Fast ATR", "Supertrend")
        self._f1 = self.Param("Factor1", 1.0) \
            .SetDisplay("Factor1", "Fast factor", "Supertrend")
        self._p2 = self.Param("AtrPeriod2", 12) \
            .SetDisplay("ATR2", "Medium ATR", "Supertrend")
        self._f2 = self.Param("Factor2", 2.0) \
            .SetDisplay("Factor2", "Medium factor", "Supertrend")
        self._p3 = self.Param("AtrPeriod3", 13) \
            .SetDisplay("ATR3", "Slow ATR", "Supertrend")
        self._f3 = self.Param("Factor3", 3.0) \
            .SetDisplay("Factor3", "Slow factor", "Supertrend")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
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
        super(triple_supertrend_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(triple_supertrend_strategy, self).OnStarted(time)
        ema1 = ExponentialMovingAverage()
        ema1.Length = self._p1.Value
        ema2 = ExponentialMovingAverage()
        ema2.Length = self._p2.Value
        ema3 = ExponentialMovingAverage()
        ema3.Length = self._p3.Value
        atr1 = AverageTrueRange()
        atr1.Length = self._p1.Value
        atr2 = AverageTrueRange()
        atr2.Length = self._p2.Value
        atr3 = AverageTrueRange()
        atr3.Length = self._p3.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema1, ema2, ema3, atr1, atr2, atr3, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema1)
            self.DrawIndicator(area, ema2)
            self.DrawIndicator(area, ema3)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, m1, m2, m3, a1, a2, a3):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return
        close = float(candle.ClosePrice)
        m1_v = float(m1)
        m2_v = float(m2)
        m3_v = float(m3)
        a1_v = float(a1)
        a2_v = float(a2)
        a3_v = float(a3)
        f1 = float(self._f1.Value)
        f2 = float(self._f2.Value)
        f3 = float(self._f3.Value)
        lower1 = m1_v - a1_v * f1
        lower2 = m2_v - a2_v * f2
        lower3 = m3_v - a3_v * f3
        up1 = close > lower1
        up2 = close > lower2
        up3 = close > lower3
        if up1 and up2 and up3 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif not up1 and not up2 and not up3 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and not up1 and not up2:
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and up1 and up2:
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return triple_supertrend_strategy()
