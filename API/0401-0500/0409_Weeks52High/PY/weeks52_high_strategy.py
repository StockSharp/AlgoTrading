import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
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

        self._highest = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(weeks52_high_strategy, self).OnReseted()
        self._highest = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(weeks52_high_strategy, self).OnStarted2(time)
        self._highest = Highest()
        self._highest.Length = int(self._high_period.Value)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._highest, self._process_candle).Start()

    def _process_candle(self, candle, highest_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._highest.IsFormed:
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
        entry_ratio = float(self._entry_ratio.Value)
        exit_ratio = float(self._exit_ratio.Value)
        cooldown = int(self._cooldown_bars.Value)

        if ratio >= entry_ratio and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif ratio <= exit_ratio and self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return weeks52_high_strategy()
