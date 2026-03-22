import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, CandleIndicatorValue
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

        self._trend = None
        self._bars_since_event = 0
        self._cooldown_remaining = 0
        self._latest_trend_value = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        result = []
        if self.Security is not None:
            result.append((self.Security, self.candle_type))
        return result

    def OnReseted(self):
        super(earnings_announcement_premium_strategy, self).OnReseted()
        self._trend = None
        self._bars_since_event = 0
        self._cooldown_remaining = 0
        self._latest_trend_value = 0.0

    def OnStarted(self, time):
        super(earnings_announcement_premium_strategy, self).OnStarted(time)

        self._trend = SimpleMovingAverage()
        self._trend.Length = int(self._trend_length.Value)

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

        civ = CandleIndicatorValue(self._trend, candle)
        civ.IsFinal = True
        trend_result = self._trend.Process(civ)
        self._latest_trend_value = float(trend_result)

        if not self._trend.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        event_cycle = int(self._event_cycle_bars.Value)
        days_before = int(self._days_before.Value)
        days_after = int(self._days_after.Value)
        cooldown = int(self._cooldown_bars.Value)

        bars_to_event = event_cycle - (self._bars_since_event % event_cycle)
        bullish_window = bars_to_event <= days_before and bars_to_event > 0 and float(candle.ClosePrice) >= self._latest_trend_value * 0.995
        exit_window = (self._bars_since_event % event_cycle) == days_after

        if self._cooldown_remaining == 0 and self.Position == 0 and bullish_window:
            self.BuyMarket()
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and exit_window:
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown

        self._bars_since_event += 1

    def CreateClone(self):
        return earnings_announcement_premium_strategy()
