import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class renko_chart_from_ticks_strategy(Strategy):
    def __init__(self):
        super(renko_chart_from_ticks_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for significance filter", "General")
        self._body_atr_factor = self.Param("BodyAtrFactor", 0.7) \
            .SetDisplay("Body ATR Factor", "Minimum body size as ATR fraction", "General")
        self._cooldown_candles = self.Param("CooldownCandles", 2) \
            .SetDisplay("Cooldown Candles", "Minimum candles between signals", "General")
        self._prev_up = None
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def body_atr_factor(self):
        return self._body_atr_factor.Value

    @property
    def cooldown_candles(self):
        return self._cooldown_candles.Value

    def OnReseted(self):
        super(renko_chart_from_ticks_strategy, self).OnReseted()
        self._prev_up = None
        self._bars_since_signal = int(self.cooldown_candles)

    def OnStarted(self, time):
        super(renko_chart_from_ticks_strategy, self).OnStarted(time)
        self._prev_up = None
        self._bars_since_signal = int(self.cooldown_candles)
        atr = AverageTrueRange()
        atr.Length = int(self.atr_period)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return
        atr_value = float(atr_value)
        self._bars_since_signal += 1
        if atr_value <= 0:
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        body = abs(close - open_p)
        baf = float(self.body_atr_factor)
        if body < atr_value * baf:
            return
        is_up = close > open_p
        cd = int(self.cooldown_candles)
        if self._prev_up is not None and self._prev_up != is_up and self._bars_since_signal >= cd:
            if is_up and self.Position <= 0:
                self.BuyMarket()
                self._bars_since_signal = 0
            elif not is_up and self.Position >= 0:
                self.SellMarket()
                self._bars_since_signal = 0
        self._prev_up = is_up

    def CreateClone(self):
        return renko_chart_from_ticks_strategy()
