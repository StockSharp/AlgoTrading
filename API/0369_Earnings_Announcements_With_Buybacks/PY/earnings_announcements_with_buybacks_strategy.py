import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

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
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        result = []
        if self.Security is not None:
            result.append((self.Security, self.candle_type))
        return result

    def OnReseted(self):
        super(earnings_announcements_with_buybacks_strategy, self).OnReseted()
        self._buyback_proxy = None
        self._bar_index = 0
        self._holding_remaining = 0
        self._cooldown_remaining = 0
        self._latest_buyback_value = 0.0

    def OnStarted2(self, time):
        super(earnings_announcements_with_buybacks_strategy, self).OnStarted2(time)

        self._buyback_proxy = ExponentialMovingAverage()
        self._buyback_proxy.Length = int(self._buyback_length.Value)

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

        buyback_signal = self.CalculateBuybackSignal(candle)
        result = process_float(self._buyback_proxy, buyback_signal, candle.OpenTime, True)
        self._latest_buyback_value = float(result)

        if not self._buyback_proxy.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            self._bar_index += 1
            return

        cooldown = int(self._cooldown_bars.Value)
        days_before = int(self._days_before.Value)
        days_after = int(self._days_after.Value)
        event_cycle = int(self._event_cycle_bars.Value)
        buyback_thresh = float(self._buyback_threshold.Value)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if self._holding_remaining > 0:
            self._holding_remaining -= 1
            if self._holding_remaining == 0 and self.Position > 0:
                self.SellMarket(self.Position)
                self._cooldown_remaining = cooldown

        bars_to_event = event_cycle - (self._bar_index % event_cycle)
        in_entry_window = bars_to_event <= days_before and bars_to_event > 0
        buyback_active = self._latest_buyback_value >= buyback_thresh

        if self._cooldown_remaining == 0 and self.Position == 0 and in_entry_window and buyback_active:
            self.BuyMarket()
            self._holding_remaining = days_after + 1
            self._cooldown_remaining = cooldown

        self._bar_index += 1

    def CalculateBuybackSignal(self, candle):
        price_base = max(float(candle.OpenPrice), 1.0)
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        range_val = max(float(candle.HighPrice) - float(candle.LowPrice), price_step)
        close_location = ((float(candle.ClosePrice) - float(candle.LowPrice)) - (float(candle.HighPrice) - float(candle.ClosePrice))) / range_val
        compression = 1.0 - min(0.2, range_val / price_base)

        return (close_location * 2.0) + compression

    def CreateClone(self):
        return earnings_announcements_with_buybacks_strategy()
