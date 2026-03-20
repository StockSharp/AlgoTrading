import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RateOfChange
from StockSharp.Algo.Strategies import Strategy


class earnings_announcement_reversal_strategy(Strategy):
    """Trades short-term reversals around synthetic earnings announcement dates
    for the primary instrument."""

    def __init__(self):
        super(earnings_announcement_reversal_strategy, self).__init__()

        self._lookback_days = self.Param("LookbackDays", 6) \
            .SetRange(2, 30) \
            .SetDisplay("Lookback Days", "Number of bars used to calculate recent return", "Parameters")

        self._holding_days = self.Param("HoldingDays", 3) \
            .SetRange(1, 20) \
            .SetDisplay("Holding Days", "Bars to hold the position after the event", "Parameters")

        self._event_cycle_bars = self.Param("EventCycleBars", 20) \
            .SetRange(8, 80) \
            .SetDisplay("Event Cycle Bars", "Distance between synthetic earnings events", "Parameters")

        self._reversal_threshold = self.Param("ReversalThreshold", 1.2) \
            .SetRange(0.1, 20.0) \
            .SetDisplay("Reversal Threshold", "Absolute momentum threshold used to classify winners and losers", "Parameters")

        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetRange(0, 20) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 2.5) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._bar_index = 0
        self._holding_remaining = 0
        self._cooldown_remaining = 0
        self._latest_momentum = 0.0

    @property
    def LookbackDays(self):
        return self._lookback_days.Value

    @property
    def HoldingDays(self):
        return self._holding_days.Value

    @property
    def EventCycleBars(self):
        return self._event_cycle_bars.Value

    @property
    def ReversalThreshold(self):
        return self._reversal_threshold.Value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(earnings_announcement_reversal_strategy, self).OnReseted()
        self._bar_index = 0
        self._holding_remaining = 0
        self._cooldown_remaining = 0
        self._latest_momentum = 0.0

    def OnStarted(self, time):
        super(earnings_announcement_reversal_strategy, self).OnStarted(time)

        momentum = RateOfChange()
        momentum.Length = self.LookbackDays

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(momentum, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(float(self.StopLoss), UnitTypes.Percent))

    def ProcessCandle(self, candle, momentum_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._bar_index += 1
            return

        self._latest_momentum = float(momentum_val)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if self._holding_remaining > 0:
            self._holding_remaining -= 1
            if self._holding_remaining == 0 and self.Position != 0:
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._cooldown_remaining = self.CooldownBars

        is_event_bar = self._bar_index > 0 and self._bar_index % self.EventCycleBars == 0
        if self._cooldown_remaining == 0 and self.Position == 0 and is_event_bar:
            if self._latest_momentum >= self.ReversalThreshold:
                self.SellMarket()
                self._holding_remaining = self.HoldingDays
                self._cooldown_remaining = self.CooldownBars
            elif self._latest_momentum <= -self.ReversalThreshold:
                self.BuyMarket()
                self._holding_remaining = self.HoldingDays
                self._cooldown_remaining = self.CooldownBars

        self._bar_index += 1

    def CreateClone(self):
        return earnings_announcement_reversal_strategy()
