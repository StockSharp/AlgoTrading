import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_30_70_strategy(Strategy):
    """RSI 30-70 Strategy."""

    def __init__(self):
        super(rsi_30_70_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI calculation length", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "Overbought level", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("RSI Oversold", "Oversold level", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._rsi = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_30_70_strategy, self).OnReseted()
        self._rsi = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(rsi_30_70_strategy, self).OnStarted2(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        rsi_v = float(rsi_val)
        overbought = float(self._rsi_overbought.Value)
        oversold = float(self._rsi_oversold.Value)
        cooldown = int(self._cooldown_bars.Value)

        if rsi_v < oversold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif rsi_v > overbought and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and rsi_v > 60:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and rsi_v < 40:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return rsi_30_70_strategy()
