import clr
import sys

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class one_two_three_reversal_strategy(Strategy):
    """One-Two-Three Reversal Strategy."""

    def __init__(self):
        super(one_two_three_reversal_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._hold_bars = self.Param("HoldBars", 15) \
            .SetDisplay("Hold Bars", "Bars to hold position", "Trading")
        self._ma_length = self.Param("MaLength", 50) \
            .SetDisplay("MA Length", "Moving average period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._sma = None
        self._low1 = 0.0
        self._low2 = 0.0
        self._low3 = 0.0
        self._low4 = 0.0
        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._history_count = 0
        self._bars_since_entry = 999999999
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(one_two_three_reversal_strategy, self).OnReseted()
        self._sma = None
        self._low1 = 0.0
        self._low2 = 0.0
        self._low3 = 0.0
        self._low4 = 0.0
        self._high1 = 0.0
        self._high2 = 0.0
        self._high3 = 0.0
        self._history_count = 0
        self._bars_since_entry = 999999999
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(one_two_three_reversal_strategy, self).OnStarted2(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = int(self._ma_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            self._bars_since_entry += 1

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._update_history(candle)
            return

        hold = int(self._hold_bars.Value)
        cooldown = int(self._cooldown_bars.Value)
        price = float(candle.ClosePrice)
        ma_val = float(ma_value)

        if self._history_count >= 4:
            if self.Position > 0 and (self._bars_since_entry >= hold or price >= ma_val):
                self.SellMarket(Math.Abs(self.Position))
                self._bars_since_entry = 999999999
                self._cooldown_remaining = cooldown
            elif self.Position <= 0:
                condition1 = float(candle.LowPrice) < self._low1
                condition2 = self._low1 < self._low3
                condition3 = self._low2 < self._low4
                condition4 = self._high2 < self._high3

                if condition1 and condition2 and condition3 and condition4:
                    if self.Position < 0:
                        self.BuyMarket(Math.Abs(self.Position))
                    self.BuyMarket(self.Volume)
                    self._bars_since_entry = 0
                    self._cooldown_remaining = cooldown

        self._update_history(candle)

    def _update_history(self, candle):
        self._low4 = self._low3
        self._low3 = self._low2
        self._low2 = self._low1
        self._low1 = float(candle.LowPrice)
        self._high3 = self._high2
        self._high2 = self._high1
        self._high1 = float(candle.HighPrice)
        if self._history_count < 4:
            self._history_count += 1

    def CreateClone(self):
        return one_two_three_reversal_strategy()
