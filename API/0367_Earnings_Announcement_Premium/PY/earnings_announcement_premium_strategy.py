import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class earnings_announcement_premium_strategy(Strategy):
    """Earnings announcement premium strategy that enters the primary instrument
    shortly before a synthetic earnings event and exits after the event passes."""

    def __init__(self):
        super(earnings_announcement_premium_strategy, self).__init__()

        self._days_before = self.Param("DaysBefore", 3) \
            .SetRange(1, 10) \
            .SetDisplay("Days Before", "Bars before the synthetic earnings event to enter", "General")

        self._days_after = self.Param("DaysAfter", 1) \
            .SetRange(1, 10) \
            .SetDisplay("Days After", "Bars after the synthetic earnings event to exit", "General")

        self._event_cycle_bars = self.Param("EventCycleBars", 18) \
            .SetRange(8, 80) \
            .SetDisplay("Event Cycle Bars", "Distance between synthetic earnings events in finished bars", "General")

        self._trend_length = self.Param("TrendLength", 12) \
            .SetRange(3, 50) \
            .SetDisplay("Trend Length", "Trend filter length", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetRange(0, 20) \
            .SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk")

        self._stop_loss = self.Param("StopLoss", 2.5) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")

        self._bars_since_event = 0
        self._cooldown_remaining = 0

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
    def TrendLength(self):
        return self._trend_length.Value

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
        super(earnings_announcement_premium_strategy, self).OnReseted()
        self._bars_since_event = 0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(earnings_announcement_premium_strategy, self).OnStarted(time)

        trend = SimpleMovingAverage()
        trend.Length = self.TrendLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(trend, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(float(self.StopLoss), UnitTypes.Percent))

    def ProcessCandle(self, candle, trend_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        latest_trend = float(trend_val)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        bars_to_event = self.EventCycleBars - (self._bars_since_event % self.EventCycleBars)
        bullish_window = (bars_to_event <= self.DaysBefore and bars_to_event > 0
                          and float(candle.ClosePrice) >= latest_trend * 0.995)
        exit_window = (self._bars_since_event % self.EventCycleBars) == self.DaysAfter

        if self._cooldown_remaining == 0 and self.Position == 0 and bullish_window:
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
        elif self.Position > 0 and exit_window:
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars

        self._bars_since_event += 1

    def CreateClone(self):
        return earnings_announcement_premium_strategy()
