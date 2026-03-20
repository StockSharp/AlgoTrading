import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class double_rsi_strategy(Strategy):
    """Double RSI Strategy. Uses short and long RSI for entry/exit signals."""

    def __init__(self):
        super(double_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._rsi_short_length = self.Param("RSIShortLength", 7) \
            .SetDisplay("Short RSI", "Short RSI period", "RSI")
        self._rsi_long_length = self.Param("RSILongLength", 21) \
            .SetDisplay("Long RSI", "Long RSI period", "RSI")
        self._oversold = self.Param("Oversold", 35.0) \
            .SetDisplay("Oversold", "RSI oversold level", "RSI")
        self._overbought = self.Param("Overbought", 65.0) \
            .SetDisplay("Overbought", "RSI overbought level", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def Oversold(self):
        return self._oversold.Value
    @property
    def Overbought(self):
        return self._overbought.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(double_rsi_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(double_rsi_strategy, self).OnStarted(time)
        rsi_short = RelativeStrengthIndex()
        rsi_short.Length = self._rsi_short_length.Value
        rsi_long = RelativeStrengthIndex()
        rsi_long.Length = self._rsi_long_length.Value
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi_short, rsi_long, self.OnProcess).Start()

    def OnProcess(self, candle, rsi_short_val, rsi_long_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        rs = float(rsi_short_val)
        rl = float(rsi_long_val)

        if rs < self.Oversold and rl < self.Oversold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
        elif rs > self.Overbought and rl > self.Overbought and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars
        elif self.Position > 0 and rs > self.Overbought:
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars
        elif self.Position < 0 and rs < self.Oversold:
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        return double_rsi_strategy()
