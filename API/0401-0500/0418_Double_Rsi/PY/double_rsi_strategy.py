import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
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

        self._rsi_short = None
        self._rsi_long = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(double_rsi_strategy, self).OnReseted()
        self._rsi_short = None
        self._rsi_long = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(double_rsi_strategy, self).OnStarted2(time)

        self._rsi_short = RelativeStrengthIndex()
        self._rsi_short.Length = int(self._rsi_short_length.Value)

        self._rsi_long = RelativeStrengthIndex()
        self._rsi_long.Length = int(self._rsi_long_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi_short, self._rsi_long, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_short_val, rsi_long_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi_short.IsFormed or not self._rsi_long.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        rs = float(rsi_short_val)
        rl = float(rsi_long_val)
        oversold = float(self._oversold.Value)
        overbought = float(self._overbought.Value)
        cooldown = int(self._cooldown_bars.Value)

        if rs < oversold and rl < oversold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif rs > overbought and rl > overbought and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and rs > overbought:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and rs < oversold:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return double_rsi_strategy()
