import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ZigZag
from StockSharp.Algo.Strategies import Strategy


class zig_and_zag_scalpel_strategy(Strategy):
    def __init__(self):
        super(zig_and_zag_scalpel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Primary timeframe for all calculations", "General")
        self._keel_over_length = self.Param("KeelOverLength", 55) \
            .SetDisplay("Candle Type", "Primary timeframe for all calculations", "General")
        self._slalom_length = self.Param("SlalomLength", 17) \
            .SetDisplay("Candle Type", "Primary timeframe for all calculations", "General")
        self._deviation_points = self.Param("DeviationPoints", 5) \
            .SetDisplay("Candle Type", "Primary timeframe for all calculations", "General")
        self._backstep = self.Param("Backstep", 3) \
            .SetDisplay("Candle Type", "Primary timeframe for all calculations", "General")
        self._breakout_distance_points = self.Param("BreakoutDistancePoints", 2) \
            .SetDisplay("Candle Type", "Primary timeframe for all calculations", "General")
        self._max_trades_per_day = self.Param("MaxTradesPerDay", 1) \
            .SetDisplay("Candle Type", "Primary timeframe for all calculations", "General")
        self._close_on_opposite_pivot = self.Param("CloseOnOppositePivot", True) \
            .SetDisplay("Candle Type", "Primary timeframe for all calculations", "General")

        self._price_step = 1
        self._deviation = 0.0
        self._breakout_distance = 0.0
        self._previous_major_pivot = 0.0
        self._last_major_pivot = 0.0
        self._previous_minor_pivot = 0.0
        self._last_minor_pivot = 0.0
        self._current_day = DateTime.MinValue
        self._trades_today = 0.0
        self._trend_up = False
        self._last_minor_pivot_type = PivotTypes.None
        self._minor_pivot_used = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zig_and_zag_scalpel_strategy, self).OnReseted()
        self._price_step = 1
        self._deviation = 0.0
        self._breakout_distance = 0.0
        self._previous_major_pivot = 0.0
        self._last_major_pivot = 0.0
        self._previous_minor_pivot = 0.0
        self._last_minor_pivot = 0.0
        self._current_day = DateTime.MinValue
        self._trades_today = 0.0
        self._trend_up = False
        self._last_minor_pivot_type = PivotTypes.None
        self._minor_pivot_used = False

    def OnStarted(self, time):
        super(zig_and_zag_scalpel_strategy, self).OnStarted(time)

        self._major_zig_zag = ZigZag()
        self._major_zig_zag.Deviation = 0.02
        self._minor_zig_zag = ZigZag()
        self._minor_zig_zag.Deviation = 0.005

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._major_zig_zag, self._minor_zig_zag, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return zig_and_zag_scalpel_strategy()
