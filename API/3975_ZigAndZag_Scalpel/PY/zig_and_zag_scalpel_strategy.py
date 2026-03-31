import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, DateTime
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
        self._keel_over_length = self.Param("KeelOverLength", 55) \
            .SetDisplay("KeelOver Length", "Lookback for the trend-defining ZigZag", "ZigZag")
        self._slalom_length = self.Param("SlalomLength", 17) \
            .SetDisplay("Slalom Length", "Lookback for the entry ZigZag", "ZigZag")
        self._deviation_points = self.Param("DeviationPoints", 5.0) \
            .SetDisplay("Deviation (pts)", "Minimum price movement to confirm a new pivot", "ZigZag")
        self._backstep = self.Param("Backstep", 3) \
            .SetDisplay("Backstep", "Bars that must separate consecutive pivots", "ZigZag")
        self._breakout_distance_points = self.Param("BreakoutDistancePoints", 2.0) \
            .SetDisplay("Breakout Distance (pts)", "Required distance from pivot to trigger order", "Trading")
        self._max_trades_per_day = self.Param("MaxTradesPerDay", 1) \
            .SetDisplay("Max Trades Per Day", "Daily limit", "Trading")
        self._close_on_opposite_pivot = self.Param("CloseOnOppositePivot", True) \
            .SetDisplay("Close On Opposite Pivot", "Exit when entry ZigZag prints opposite swing", "Risk")

        self._price_step = 1.0
        self._deviation = 0.0
        self._breakout_distance = 0.0
        self._previous_major_pivot = 0.0
        self._last_major_pivot = 0.0
        self._previous_minor_pivot = 0.0
        self._last_minor_pivot = 0.0
        self._current_day = DateTime.MinValue
        self._trades_today = 0
        self._trend_up = False
        self._last_minor_pivot_type = PIVOT_NONE
        self._minor_pivot_used = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def KeelOverLength(self):
        return self._keel_over_length.Value

    @property
    def SlalomLength(self):
        return self._slalom_length.Value

    @property
    def DeviationPoints(self):
        return self._deviation_points.Value

    @property
    def Backstep(self):
        return self._backstep.Value

    @property
    def BreakoutDistancePoints(self):
        return self._breakout_distance_points.Value

    @property
    def MaxTradesPerDay(self):
        return self._max_trades_per_day.Value

    @property
    def CloseOnOppositePivot(self):
        return self._close_on_opposite_pivot.Value

    def OnStarted2(self, time):
        super(zig_and_zag_scalpel_strategy, self).OnStarted2(time)

        self._price_step = float(self.Security.PriceStep) if self.Security is not None else 1.0
        self._deviation = max(self._price_step, abs(float(self.DeviationPoints)) * self._price_step)
        self._breakout_distance = max(0.0, abs(float(self.BreakoutDistancePoints)) * self._price_step)

        major_zigzag = ZigZag()
        major_zigzag.Deviation = 0.02
        minor_zigzag = ZigZag()
        minor_zigzag.Deviation = 0.005

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(major_zigzag, minor_zigzag, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, major_value, minor_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_daily_counter(candle.OpenTime)
        self._update_major_trend(float(major_value))
        self._update_minor_pivot(float(minor_value))

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
            if navel - self._last_minor_pivot >= self._breakout_distance:
                self.BuyMarket()
                self._minor_pivot_used = True
                self._trades_today += 1
        elif self._last_minor_pivot_type == PIVOT_HIGH and not self._trend_up:
            if self._last_minor_pivot - navel >= self._breakout_distance:
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
        if major_value == 0:
            return
        if self._last_major_pivot == 0:
            self._last_major_pivot = major_value
            self._previous_major_pivot = major_value
            return
        if major_value == self._last_major_pivot:
            return
        self._previous_major_pivot = self._last_major_pivot
        self._last_major_pivot = major_value
        self._trend_up = self._last_major_pivot < self._previous_major_pivot

    def _update_minor_pivot(self, minor_value):
        if minor_value == 0:
            return
        if self._last_minor_pivot == 0:
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
        return (5.0 * float(candle.ClosePrice) + 2.0 * float(candle.OpenPrice) +
                float(candle.HighPrice) + float(candle.LowPrice)) / 9.0

    def OnReseted(self):
        super(zig_and_zag_scalpel_strategy, self).OnReseted()
        self._previous_major_pivot = 0.0
        self._last_major_pivot = 0.0
        self._previous_minor_pivot = 0.0
        self._last_minor_pivot = 0.0
        self._current_day = DateTime.MinValue
        self._trades_today = 0
        self._trend_up = False
        self._last_minor_pivot_type = PIVOT_NONE
        self._minor_pivot_used = False

    def CreateClone(self):
        return zig_and_zag_scalpel_strategy()
