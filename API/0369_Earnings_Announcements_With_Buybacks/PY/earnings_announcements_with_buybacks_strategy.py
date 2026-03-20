import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class earnings_announcements_with_buybacks_strategy(Strategy):
    """Strategy that buys the primary instrument before synthetic earnings events
    when a synthetic buyback regime is active and exits after the event."""

    def __init__(self):
        super(earnings_announcements_with_buybacks_strategy, self).__init__()

        self._days_before = self.Param("DaysBefore", 3) \
            .SetRange(1, 10) \
            .SetDisplay("Days Before", "Bars before the synthetic earnings event to enter", "Trading")

        self._days_after = self.Param("DaysAfter", 1) \
            .SetRange(1, 10) \
            .SetDisplay("Days After", "Bars after the synthetic earnings event to exit", "Trading")

        self._event_cycle_bars = self.Param("EventCycleBars", 20) \
            .SetRange(8, 80) \
            .SetDisplay("Event Cycle Bars", "Distance between synthetic earnings events", "Trading")

        self._buyback_length = self.Param("BuybackLength", 8) \
            .SetRange(2, 40) \
            .SetDisplay("Buyback Length", "Smoothing length for the synthetic buyback proxy", "Indicators")

        self._buyback_threshold = self.Param("BuybackThreshold", 0.7) \
            .SetRange(-5.0, 5.0) \
            .SetDisplay("Buyback Threshold", "Minimum synthetic buyback score required to enter", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetRange(0, 20) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 2.5) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._buyback_proxy = None
        self._bar_index = 0
        self._holding_remaining = 0
        self._cooldown_remaining = 0
        self._latest_buyback_value = 0.0

    @property
    def DaysBefore(self):
        return self._days_before.Value

    @property
    def DaysAfter(self):
        return self._days_after.Value

    @property
    def EventCycleBars(self):
        return self._event_cycle_bars.Value

    @property
    def BuybackLength(self):
        return self._buyback_length.Value

    @property
    def BuybackThreshold(self):
        return self._buyback_threshold.Value

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
        super(earnings_announcements_with_buybacks_strategy, self).OnReseted()
        self._buyback_proxy = None
        self._bar_index = 0
        self._holding_remaining = 0
        self._cooldown_remaining = 0
        self._latest_buyback_value = 0.0

    def OnStarted(self, time):
        super(earnings_announcements_with_buybacks_strategy, self).OnStarted(time)

        self._buyback_proxy = ExponentialMovingAverage()
        self._buyback_proxy.Length = self.BuybackLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(float(self.StopLoss), UnitTypes.Percent))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        buyback_signal = Decimal(self._calculate_buyback_signal(candle))
        result = IndicatorHelper.Process(self._buyback_proxy, buyback_signal, candle.OpenTime, True)
        self._latest_buyback_value = float(result)

        if not self._buyback_proxy.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            self._bar_index += 1
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if self._holding_remaining > 0:
            self._holding_remaining -= 1
            if self._holding_remaining == 0 and self.Position > 0:
                self.SellMarket()
                self._cooldown_remaining = self.CooldownBars

        bars_to_event = self.EventCycleBars - (self._bar_index % self.EventCycleBars)
        in_entry_window = bars_to_event <= self.DaysBefore and bars_to_event > 0
        buyback_active = self._latest_buyback_value >= self.BuybackThreshold

        if self._cooldown_remaining == 0 and self.Position == 0 and in_entry_window and buyback_active:
            self.BuyMarket()
            self._holding_remaining = self.DaysAfter + 1
            self._cooldown_remaining = self.CooldownBars

        self._bar_index += 1

    def _calculate_buyback_signal(self, candle):
        price_base = max(float(candle.OpenPrice), 1.0)
        rng = max(float(candle.HighPrice) - float(candle.LowPrice), 0.01)
        close_location = ((float(candle.ClosePrice) - float(candle.LowPrice)) - (float(candle.HighPrice) - float(candle.ClosePrice))) / rng
        compression = 1.0 - min(0.2, rng / price_base)
        return (close_location * 2.0) + compression

    def CreateClone(self):
        return earnings_announcements_with_buybacks_strategy()
