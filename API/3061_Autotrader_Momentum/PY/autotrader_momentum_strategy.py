import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class autotrader_momentum_strategy(Strategy):
    def __init__(self):
        super(autotrader_momentum_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe for price comparisons", "Data")
        self._current_bar_index = self.Param("CurrentBarIndex", 0) \
            .SetDisplay("Current Bar Index", "Index of signal source candle", "Logic")
        self._comparable_bar_index = self.Param("ComparableBarIndex", 8) \
            .SetDisplay("Comparable Bar Index", "Historical candle index for comparison", "Logic")
        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetDisplay("Cooldown Bars", "Bars to wait after entries and exits", "Risk")

        self._close_history = []
        self._cooldown_left = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def CurrentBarIndex(self):
        return self._current_bar_index.Value
    @property
    def ComparableBarIndex(self):
        return self._comparable_bar_index.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(autotrader_momentum_strategy, self).OnReseted()
        self._close_history = []
        self._cooldown_left = 0

    def OnStarted(self, time):
        super(autotrader_momentum_strategy, self).OnStarted(time)
        self._close_history = []
        self._cooldown_left = 0
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_left > 0:
            self._cooldown_left -= 1

        close = float(candle.ClosePrice)
        max_count = max(self.CurrentBarIndex, self.ComparableBarIndex) + 1
        if max_count <= 0:
            max_count = 1
        self._close_history.append(close)
        if len(self._close_history) > max_count:
            self._close_history.pop(0)

        if self._cooldown_left > 0:
            return

        required_history = max(self.CurrentBarIndex, self.ComparableBarIndex) + 1
        if len(self._close_history) < required_history:
            return

        current_close = self._get_close_at_index(self.CurrentBarIndex)
        comparable_close = self._get_close_at_index(self.ComparableBarIndex)
        if current_close is None or comparable_close is None:
            return

        if current_close > comparable_close and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_left = self.CooldownBars
        elif current_close < comparable_close and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_left = self.CooldownBars

    def _get_close_at_index(self, index_from_current):
        if index_from_current < 0:
            return None
        target = len(self._close_history) - 1 - index_from_current
        if target < 0 or target >= len(self._close_history):
            return None
        return self._close_history[target]

    def CreateClone(self):
        return autotrader_momentum_strategy()
