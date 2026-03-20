import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest
from StockSharp.Algo.Strategies import Strategy


class weeks52_high_strategy(Strategy):
    """Strategy based on the 52-week high proximity effect."""

    def __init__(self):
        super(weeks52_high_strategy, self).__init__()

        self._high_period = self.Param("HighPeriod", 50) \
            .SetDisplay("High Period", "Rolling high lookback period", "Parameters")
        self._entry_ratio = self.Param("EntryRatio", 0.97) \
            .SetDisplay("Entry Ratio", "Min price/high ratio to enter", "Parameters")
        self._exit_ratio = self.Param("ExitRatio", 0.92) \
            .SetDisplay("Exit Ratio", "Exit when price/high drops below this", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown_remaining = 0

    @property
    def HighPeriod(self):
        return self._high_period.Value
    @property
    def EntryRatio(self):
        return self._entry_ratio.Value
    @property
    def ExitRatio(self):
        return self._exit_ratio.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(weeks52_high_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(weeks52_high_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self.HighPeriod
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, highest_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        hv = float(highest_val)
        if hv <= 0:
            return

        ratio = float(candle.ClosePrice) / hv

        if ratio >= self.EntryRatio and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
        elif ratio <= self.ExitRatio and self.Position > 0:
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        return weeks52_high_strategy()
