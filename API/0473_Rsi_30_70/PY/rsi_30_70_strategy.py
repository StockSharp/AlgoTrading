import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_30_70_strategy(Strategy):
    def __init__(self):
        super(rsi_30_70_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI calculation length", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "Overbought level", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("RSI Oversold", "Oversold level", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def rsi_length(self):
        return self._rsi_length.Value
    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value
    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(rsi_30_70_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(rsi_30_70_strategy, self).OnStarted(time)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(self._rsi, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._rsi.IsFormed:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        rsi_v = float(rsi_val)

        if rsi_v < float(self.rsi_oversold) and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif rsi_v > float(self.rsi_overbought) and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and rsi_v > 60:
            self.SellMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and rsi_v < 40:
            self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return rsi_30_70_strategy()
