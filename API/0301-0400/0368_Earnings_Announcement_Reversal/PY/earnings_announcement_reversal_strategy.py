import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RateOfChange, CandleIndicatorValue
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

        self._momentum = None
        self._bar_index = 0
        self._holding_remaining = 0
        self._cooldown_remaining = 0
        self._latest_momentum = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        result = []
        if self.Security is not None:
            result.append((self.Security, self.candle_type))
        return result

    def OnReseted(self):
        super(earnings_announcement_reversal_strategy, self).OnReseted()
        self._momentum = None
        self._bar_index = 0
        self._holding_remaining = 0
        self._cooldown_remaining = 0
        self._latest_momentum = 0.0

    def OnStarted2(self, time):
        super(earnings_announcement_reversal_strategy, self).OnStarted2(time)

        self._momentum = RateOfChange()
        self._momentum.Length = int(self._lookback_days.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(float(self._stop_loss.Value), UnitTypes.Percent)
        )

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        civ = CandleIndicatorValue(self._momentum, candle)
        civ.IsFinal = True
        momentum_value = self._momentum.Process(civ)

        if momentum_value.IsEmpty or not self._momentum.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            self._bar_index += 1
            return

        self._latest_momentum = float(momentum_value)

        cooldown = int(self._cooldown_bars.Value)
        holding_days = int(self._holding_days.Value)
        event_cycle = int(self._event_cycle_bars.Value)
        reversal_thresh = float(self._reversal_threshold.Value)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if self._holding_remaining > 0:
            self._holding_remaining -= 1
            if self._holding_remaining == 0 and self.Position != 0:
                if self.Position > 0:
                    self.SellMarket(self.Position)
                else:
                    self.BuyMarket(Math.Abs(self.Position))
                self._cooldown_remaining = cooldown

        is_event_bar = self._bar_index > 0 and self._bar_index % event_cycle == 0
        if self._cooldown_remaining == 0 and self.Position == 0 and is_event_bar:
            if self._latest_momentum >= reversal_thresh:
                self.SellMarket()
                self._holding_remaining = holding_days
                self._cooldown_remaining = cooldown
            elif self._latest_momentum <= -reversal_thresh:
                self.BuyMarket()
                self._holding_remaining = holding_days
                self._cooldown_remaining = cooldown

        self._bar_index += 1

    def CreateClone(self):
        return earnings_announcement_reversal_strategy()
