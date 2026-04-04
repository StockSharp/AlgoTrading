import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTime, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ZigZag

# Pivot type constants
PIVOT_NONE = 0
PIVOT_LOW = 1
PIVOT_HIGH = 2

class zig_and_zag_scalpel_strategy(Strategy):
    def __init__(self):
        super(zig_and_zag_scalpel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Primary timeframe for all calculations", "General")
        self._max_trades_per_day = self.Param("MaxTradesPerDay", 1) \
            .SetDisplay("Max Trades Per Day", "Daily limit matching the original expert advisor", "Trading")
        self._close_on_opposite_pivot = self.Param("CloseOnOppositePivot", True) \
            .SetDisplay("Close On Opposite Pivot", "Exit when the entry ZigZag prints the opposite swing", "Risk")

        self._previous_major_pivot = Decimal(0)
        self._last_major_pivot = Decimal(0)
        self._previous_minor_pivot = Decimal(0)
        self._last_minor_pivot = Decimal(0)
        self._current_day = DateTime.MinValue
        self._trades_today = 0
        self._trend_up = False
        self._last_minor_pivot_type = PIVOT_NONE
        self._minor_pivot_used = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MaxTradesPerDay(self):
        return self._max_trades_per_day.Value

    @property
    def CloseOnOppositePivot(self):
        return self._close_on_opposite_pivot.Value

    def OnStarted2(self, time):
        super(zig_and_zag_scalpel_strategy, self).OnStarted2(time)

        major_zigzag = ZigZag()
        major_zigzag.Deviation = Decimal(0.02)
        minor_zigzag = ZigZag()
        minor_zigzag.Deviation = Decimal(0.005)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindWithEmpty(major_zigzag, minor_zigzag, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, major_zigzag)
            self.DrawIndicator(area, minor_zigzag)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, major_value, minor_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_daily_counter(candle.OpenTime)

        if major_value is not None:
            self._update_major_trend(major_value)

        if minor_value is not None:
            self._update_minor_pivot(minor_value)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._manage_existing_position()

        if self.Position != 0:
            return

        if self._minor_pivot_used:
            return

        if self._last_minor_pivot_type == PIVOT_NONE:
            return

        if self._trades_today >= self.MaxTradesPerDay:
            return

        navel = self._calculate_navel(candle)

        if self._last_minor_pivot_type == PIVOT_LOW and self._trend_up:
            if navel > self._last_minor_pivot:
                self.BuyMarket()
                self._minor_pivot_used = True
                self._trades_today += 1
        elif self._last_minor_pivot_type == PIVOT_HIGH and not self._trend_up:
            if navel < self._last_minor_pivot:
                self.SellMarket()
                self._minor_pivot_used = True
                self._trades_today += 1

    def _update_daily_counter(self, time):
        date = time.Date
        if date == self._current_day:
            return
        self._current_day = date
        self._trades_today = 0

    def _update_major_trend(self, major_value):
        if self._last_major_pivot == Decimal(0):
            self._last_major_pivot = major_value
            self._previous_major_pivot = major_value
            return
        if major_value == self._last_major_pivot:
            return
        self._previous_major_pivot = self._last_major_pivot
        self._last_major_pivot = major_value
        self._trend_up = self._last_major_pivot < self._previous_major_pivot

    def _update_minor_pivot(self, minor_value):
        if self._last_minor_pivot == Decimal(0):
            self._last_minor_pivot = minor_value
            self._previous_minor_pivot = minor_value
            self._last_minor_pivot_type = PIVOT_LOW
            self._minor_pivot_used = False
            return
        if minor_value == self._last_minor_pivot:
            return
        self._previous_minor_pivot = self._last_minor_pivot
        self._last_minor_pivot = minor_value
        self._last_minor_pivot_type = PIVOT_LOW if self._last_minor_pivot < self._previous_minor_pivot else PIVOT_HIGH
        self._minor_pivot_used = False

    def _manage_existing_position(self):
        if self.Position > 0:
            if not self._trend_up or (self.CloseOnOppositePivot and self._last_minor_pivot_type == PIVOT_HIGH):
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if self._trend_up or (self.CloseOnOppositePivot and self._last_minor_pivot_type == PIVOT_LOW):
                self.BuyMarket(abs(self.Position))

    def _calculate_navel(self, candle):
        return (Decimal(5) * candle.ClosePrice + Decimal(2) * candle.OpenPrice +
                candle.HighPrice + candle.LowPrice) / Decimal(9)

    def OnReseted(self):
        super(zig_and_zag_scalpel_strategy, self).OnReseted()
        self._previous_major_pivot = Decimal(0)
        self._last_major_pivot = Decimal(0)
        self._previous_minor_pivot = Decimal(0)
        self._last_minor_pivot = Decimal(0)
        self._current_day = DateTime.MinValue
        self._trades_today = 0
        self._trend_up = False
        self._last_minor_pivot_type = PIVOT_NONE
        self._minor_pivot_used = False

    def CreateClone(self):
        return zig_and_zag_scalpel_strategy()
